"""Filters : RBJ biquads (SOS), Butterworth, Linkwitz-Riley crossovers, modulatable TPT state-variable filter and
TPT ladder (exact block state-space propagation, vectorised), modulated fractional-delay combs, formant bank,
DC blocker, ISO 9613-1 air absorption, velvet decorrelator."""
import functools

import numpy as np
from scipy import signal

from ..config import SR
from .util import ctrl_up, fit


# ------------------------------------------------------------------ static filters

def rbj(kind, f0, q=0.7071, gain_db=0.0, sr=SR):
    """RBJ cookbook biquad as one SOS row. kind : lp hp bp notch peak lowshelf highshelf allpass."""
    f0 = float(np.clip(f0, 1.0, sr * 0.49))
    A = 10.0 ** (gain_db / 40.0)
    w = 2.0 * np.pi * f0 / sr
    cw, sw = np.cos(w), np.sin(w)
    alpha = sw / (2.0 * q)
    if kind == "lp":
        b = [(1 - cw) / 2, 1 - cw, (1 - cw) / 2]; a = [1 + alpha, -2 * cw, 1 - alpha]
    elif kind == "hp":
        b = [(1 + cw) / 2, -(1 + cw), (1 + cw) / 2]; a = [1 + alpha, -2 * cw, 1 - alpha]
    elif kind == "bp":
        b = [alpha, 0.0, -alpha]; a = [1 + alpha, -2 * cw, 1 - alpha]
    elif kind == "notch":
        b = [1.0, -2 * cw, 1.0]; a = [1 + alpha, -2 * cw, 1 - alpha]
    elif kind == "allpass":
        b = [1 - alpha, -2 * cw, 1 + alpha]; a = [1 + alpha, -2 * cw, 1 - alpha]
    elif kind == "peak":
        b = [1 + alpha * A, -2 * cw, 1 - alpha * A]; a = [1 + alpha / A, -2 * cw, 1 - alpha / A]
    elif kind in ("lowshelf", "highshelf"):
        sa = 2.0 * np.sqrt(A) * alpha
        if kind == "lowshelf":
            b = [A * ((A + 1) - (A - 1) * cw + sa), 2 * A * ((A - 1) - (A + 1) * cw), A * ((A + 1) - (A - 1) * cw - sa)]
            a = [(A + 1) + (A - 1) * cw + sa, -2 * ((A - 1) + (A + 1) * cw), (A + 1) + (A - 1) * cw - sa]
        else:
            b = [A * ((A + 1) + (A - 1) * cw + sa), -2 * A * ((A - 1) + (A + 1) * cw), A * ((A + 1) + (A - 1) * cw - sa)]
            a = [(A + 1) - (A - 1) * cw + sa, 2 * ((A - 1) - (A + 1) * cw), (A + 1) - (A - 1) * cw - sa]
    else:
        raise ValueError(kind)
    b = np.asarray(b) / a[0]
    a = np.asarray(a) / a[0]
    return np.concatenate([b, a])[None, :]


def apply(x, sos, zero_phase=False):
    x = np.asarray(x, dtype=np.float64)
    if zero_phase:
        return signal.sosfiltfilt(sos, x, axis=0)
    return signal.sosfilt(sos, x, axis=0)


def biquad(x, kind, f0, q=0.7071, gain_db=0.0, sr=SR):
    return apply(x, rbj(kind, f0, q, gain_db, sr))


@functools.lru_cache(maxsize=512)
def _butter(order, f, kind, sr):
    return signal.butter(order, f, kind, fs=sr, output="sos")


def lp(x, f, order=2, sr=SR):
    return apply(x, _butter(order, float(min(f, sr * 0.49)), "low", sr))


def hp(x, f, order=2, sr=SR):
    return apply(x, _butter(order, float(f), "high", sr))


def bp(x, lo, hi, order=2, sr=SR):
    return apply(x, _butter(order, (float(lo), float(min(hi, sr * 0.49))), "band", sr))


def peak(x, f, gain_db, q=1.0, sr=SR):
    return apply(x, rbj("peak", f, q, gain_db, sr))


def shelf(x, f, gain_db, kind="highshelf", q=0.7071, sr=SR):
    return apply(x, rbj(kind, f, q, gain_db, sr))


def lr_split(x, fc, sr=SR):
    """Linkwitz-Riley 4th order crossover : (low, high). low + high is an allpass of x (flat magnitude)."""
    b = _butter(2, float(fc), "low", sr)
    h = _butter(2, float(fc), "high", sr)
    low = apply(apply(x, b), b)
    high = apply(apply(x, h), h)
    return low, high


def lr_bands(x, freqs, sr=SR):
    """Split into len(freqs)+1 phase-coherent bands (each lower split allpass-compensated)."""
    bands = []
    rest = np.asarray(x, dtype=np.float64)
    for i, fc in enumerate(freqs):
        low, high = lr_split(rest, fc, sr)
        # compensate the already-made bands with the allpass of this crossover
        for j in range(len(bands)):
            l2, h2 = lr_split(bands[j], fc, sr)
            bands[j] = l2 + h2
        bands.append(low)
        rest = high
    bands.append(rest)
    return bands


def dc_block(x, fc=8.0, sr=SR):
    return apply(x, _butter(1, float(fc), "high", sr))


# ------------------------------------------------------------------ time-varying linear filters (exact block propagation)

def _linear_scan(M, u):
    """States at the start of every block for s[b+1] = M[b] s[b] + u[b], s[0] = 0 (Hillis-Steele scan)."""
    Mc = M.copy()
    uc = u.copy()
    nb = len(M)
    d = 1
    while d < nb:
        Mn = Mc[d:] @ Mc[:-d]
        un = np.einsum("bij,bj->bi", Mc[d:], uc[:-d]) + uc[d:]
        Mc[d:] = Mn
        uc[d:] = un
        d *= 2
    S = np.zeros_like(u)
    S[1:] = uc[:-1]
    return S


def tv_linear(x, step, coeffs, nstate, block=32):
    """Run a linear filter whose coefficients change per block of `block` samples.
    step(s (nb,k), u (nb,), coeffs) -> (s_next (nb,k), y (nb,)) is one sample of the structure (vectorised over blocks).
    Inside a block the response is computed exactly from the state-space matrices; the state is carried exactly
    across blocks, so modulation does not zipper the way swapped biquad coefficients do."""
    x = np.asarray(x, dtype=np.float64)
    n = len(x)
    nb = -(-n // block)
    X = fit(x, nb * block).reshape(nb, block)
    z = np.zeros((nb, nstate))
    A = np.empty((nb, nstate, nstate))
    C = np.empty((nb, nstate))
    zero_u = np.zeros(nb)
    for i in range(nstate):
        e = z.copy()
        e[:, i] = 1.0
        s1, y1 = step(e, zero_u, coeffs)
        A[:, :, i] = s1
        C[:, i] = y1
    B, D = step(z, np.ones(nb), coeffs)
    # C A^j (row vectors) and A^(block-1-m) B (column vectors)
    CA = np.empty((nb, block, nstate))
    v = C.copy()
    for j in range(block):
        CA[:, j] = v
        v = np.einsum("bi,bij->bj", v, A)
    AB = np.empty((nb, block, nstate))
    w = B.copy()
    for j in range(block):
        AB[:, block - 1 - j] = w
        w = np.einsum("bij,bj->bi", A, w)
    AL = A.copy()
    k = 1
    while k < block:
        AL = AL @ AL
        k *= 2
    h = np.empty((nb, block))
    h[:, 0] = D
    h[:, 1:] = np.einsum("bji,bi->bj", CA[:, :-1], B)
    u = np.einsum("bm,bmi->bi", X, AB)
    S = _linear_scan(AL, u)
    Y = np.einsum("bji,bi->bj", CA, S)
    # forced response inside each block : causal Toeplitz convolution, chunked to bound memory
    jj, mm = np.meshgrid(np.arange(block), np.arange(block), indexing="ij")
    lag = jj - mm
    mask = lag >= 0
    lag = np.where(mask, lag, 0)
    chunk = max(1, 4_000_000 // (block * block))
    for c0 in range(0, nb, chunk):
        c1 = min(nb, c0 + chunk)
        T = h[c0:c1][:, lag] * mask
        Y[c0:c1] += np.einsum("bjm,bm->bj", T, X[c0:c1])
    return Y.reshape(-1)[:n]


def _block_ctrl(c, n, block):
    """Per-block value of a control (scalar or per-sample array) taken at block centres."""
    nb = -(-n // block)
    if np.ndim(c) == 0:
        return np.full(nb, float(c))
    c = ctrl_up(c, n)
    centres = np.minimum(np.arange(nb) * block + block // 2, n - 1)
    return c[centres]


def svf(x, fc, q=0.7071, mode="lp", sr=SR, block=32):
    """Topology-preserving (Simper / Zavalishin) state-variable filter with modulatable fc and q.
    mode : lp bp hp notch peak bpn (band-pass normalised to unity peak gain)."""
    n = len(x)
    fcb = np.clip(_block_ctrl(fc, n, block), 5.0, sr * 0.49)
    qb = np.maximum(_block_ctrl(q, n, block), 0.05)
    g = np.tan(np.pi * fcb / sr)
    k = 1.0 / qb
    a1 = 1.0 / (1.0 + g * (g + k))
    a2 = g * a1
    a3 = g * a2
    co = dict(a1=a1, a2=a2, a3=a3, k=k)

    def step(s, u, c):
        ic1, ic2 = s[:, 0], s[:, 1]
        v3 = u - ic2
        v1 = c["a1"] * ic1 + c["a2"] * v3
        v2 = ic2 + c["a2"] * ic1 + c["a3"] * v3
        s2 = np.stack([2.0 * v1 - ic1, 2.0 * v2 - ic2], axis=1)
        if mode == "lp":
            y = v2
        elif mode == "bp":
            y = v1
        elif mode == "bpn":
            y = v1 * c["k"]
        elif mode == "hp":
            y = u - c["k"] * v1 - v2
        elif mode == "notch":
            y = u - c["k"] * v1
        elif mode == "peak":
            y = u - c["k"] * v1 - 2.0 * v2
        else:
            raise ValueError(mode)
        return s2, y

    return tv_linear(x, step, co, 2, block)


def ladder(x, fc, res=0.3, sr=SR, block=32, comp=0.5):
    """Linear 4-pole TPT ladder (zero-delay feedback) with modulatable cutoff and resonance (0..1, 1 ~ self-osc).
    comp : bass compensation of the resonance loss. Drive is applied by the caller (oversampled tanh)."""
    n = len(x)
    fcb = np.clip(_block_ctrl(fc, n, block), 5.0, sr * 0.45)
    rb = np.clip(_block_ctrl(res, n, block), 0.0, 0.98) * 4.0
    g = np.tan(np.pi * fcb / sr)
    G = g / (1.0 + g)
    co = dict(G=G, k=rb, c=1.0 + comp * rb)

    def step(s, u, c):
        Gb, kb = c["G"], c["k"]
        om = 1.0 - Gb
        S = Gb ** 3 * om * s[:, 0] + Gb ** 2 * om * s[:, 1] + Gb * om * s[:, 2] + om * s[:, 3]
        u0 = (u * c["c"] - kb * S) / (1.0 + kb * Gb ** 4)
        y1 = Gb * u0 + om * s[:, 0]
        y2 = Gb * y1 + om * s[:, 1]
        y3 = Gb * y2 + om * s[:, 2]
        y4 = Gb * y3 + om * s[:, 3]
        s2 = np.stack([2 * y1 - s[:, 0], 2 * y2 - s[:, 1], 2 * y3 - s[:, 2], 2 * y4 - s[:, 3]], axis=1)
        return s2, y4

    return tv_linear(x, step, co, 4, block)


# ------------------------------------------------------------------ delays and combs

def frac_read(x, pos):
    """Cubic (Catmull-Rom) read of x at fractional positions (zeros outside)."""
    i = np.floor(pos).astype(np.int64)
    f = pos - i
    n = len(x)
    xp = np.concatenate([np.zeros(2), x, np.zeros(3)])
    i = np.clip(i, -2, n) + 2
    y0, y1, y2, y3 = xp[i - 1], xp[i], xp[i + 1], xp[i + 2]
    return y1 + 0.5 * f * (y2 - y0 + f * (2 * y0 - 5 * y1 + 4 * y2 - y3 + f * (3 * (y1 - y2) + y3 - y0)))


def comb_ff(x, delay_s, mix=0.5, sr=SR):
    """Feed-forward comb / flanger with a modulatable fractional delay (seconds, scalar or array)."""
    n = len(x)
    d = ctrl_up(delay_s, n) * sr
    return x + mix * frac_read(x, np.arange(n) - d)


def comb_fb(x, delay_s, fb=0.6, sr=SR, damp=0.0):
    """Feedback comb y[n] = x[n] + fb * y[n - d[n]] with modulatable fractional delay (cubic read), processed in
    chunks shorter than the smallest delay (each chunk only reads already computed output).
    damp : one-pole low-pass inside the loop (0 .. 0.9)."""
    x = np.asarray(x, dtype=np.float64)
    n = len(x)
    d = ctrl_up(delay_s, n) * sr
    dmin = max(4, int(np.floor(d.min())) - 3)
    y = np.zeros(n + 3)
    fbv = ctrl_up(fb, n) if np.ndim(fb) else np.full(n, float(fb))
    lpstate = 0.0
    for c0 in range(0, n, dmin):
        c1 = min(n, c0 + dmin)
        pos = np.arange(c0, c1) - d[c0:c1]
        i = np.floor(pos).astype(np.int64)
        f = pos - i
        ok = i >= 1
        i = np.where(ok, i, 1)
        y0, y1, y2, y3 = y[i - 1], y[i], y[i + 1], y[i + 2]
        r = y1 + 0.5 * f * (y2 - y0 + f * (2 * y0 - 5 * y1 + 4 * y2 - y3 + f * (3 * (y1 - y2) + y3 - y0)))
        r = np.where(ok, r, 0.0)
        if damp > 0.0:
            r, zf = signal.lfilter([1.0 - damp], [1.0, -damp], r, zi=[lpstate])
            lpstate = zf[0]
        y[c0:c1] = x[c0:c1] + fbv[c0:c1] * r
    return y[:n]


def allpass_chain(x, delays_ms, g=0.5, sr=SR):
    """Schroeder allpass chain (fixed integer delays) built from lfilter."""
    y = np.asarray(x, dtype=np.float64)
    for dm in delays_ms:
        d = max(1, int(dm * sr / 1000.0))
        b = np.zeros(d + 1); a = np.zeros(d + 1)
        b[0] = -g; b[d] = 1.0
        a[0] = 1.0; a[d] = -g
        y = signal.lfilter(b, a, y)
    return y


def formant_bank(x, formants, sr=SR):
    """Parallel band-pass resonators : formants = [(freq, bw, gain_db), ...] (static)."""
    out = np.zeros(len(x))
    for f, bw, gdb in formants:
        out += biquad(x, "bp", f, max(0.3, f / max(bw, 1.0)), sr=sr) * 10 ** (gdb / 20.0)
    return out


def formant_bank_tv(x, tracks, sr=SR):
    """Moving formants : tracks = [(freq_array_or_scalar, q, gain), ...] through modulatable SVF band-passes."""
    out = np.zeros(len(x))
    for f, q, g in tracks:
        out += svf(x, f, q, "bpn", sr) * g
    return out


# ------------------------------------------------------------------ air absorption (ISO 9613-1)

def air_alpha_db_per_m(f, temp_c=20.0, rh=50.0, pa_kpa=101.325):
    """Atmospheric absorption coefficient in dB/m (ISO 9613-1)."""
    f = np.asarray(f, dtype=np.float64)
    T = temp_c + 273.15
    T0 = 293.15
    T01 = 273.16
    pr = pa_kpa / 101.325
    C = -6.8346 * (T01 / T) ** 1.261 + 4.6151
    h = rh * 10 ** C / pr
    frO = pr * (24.0 + 4.04e4 * h * (0.02 + h) / (0.391 + h))
    frN = pr * (T / T0) ** -0.5 * (9.0 + 280.0 * h * np.exp(-4.170 * ((T / T0) ** (-1.0 / 3.0) - 1.0)))
    a = 8.686 * f * f * (1.84e-11 / pr * (T / T0) ** 0.5 + (T / T0) ** -2.5 * (
        0.01275 * np.exp(-2239.1 / T) / (frO + f * f / frO) + 0.1068 * np.exp(-3352.0 / T) / (frN + f * f / frN)))
    return a


def air_absorb(x, distance_m, sr=SR, extra_lp_per_km=0.0):
    """Static air absorption for a source at distance_m (zero-phase, frequency-domain)."""
    x = np.asarray(x, dtype=np.float64)
    n = len(x)
    nfft = 1 << int(np.ceil(np.log2(n + 1)))
    f = np.fft.rfftfreq(nfft, 1.0 / sr)
    att = air_alpha_db_per_m(f) * distance_m
    gain = 10 ** (-att / 20.0)
    X = np.fft.rfft(x, nfft, axis=0)
    y = np.fft.irfft(X * (gain if x.ndim == 1 else gain[:, None]), nfft, axis=0)[:n]
    return y


# ------------------------------------------------------------------ decorrelation (side channel only)

@functools.lru_cache(maxsize=32)
def _velvet_fir(seed, length_ms, density, sr):
    rng = np.random.default_rng(seed)
    n = int(sr * length_ms / 1000.0)
    grid = max(1, int(sr / density))
    h = np.zeros(n)
    cells = n // grid
    pos = np.arange(cells) * grid + rng.integers(0, grid, cells)
    sgn = rng.choice([-1.0, 1.0], cells)
    env = np.exp(-np.arange(n) / (n / 3.0))
    h[pos] = sgn * env[pos]
    h /= np.sqrt(np.sum(h * h))
    return h


def decorrelate(x, seed=1, length_ms=18.0, density=1800.0, hp_hz=250.0, sr=SR):
    """A decorrelated copy of x with roughly the same spectrum (velvet FIR), high-passed above hp_hz :
    used as the SIDE of a mono source, so the mono sum stays exactly the source."""
    h = _velvet_fir(int(seed), float(length_ms), float(density), sr)
    y = signal.fftconvolve(np.asarray(x, dtype=np.float64), h)[:len(x)]
    return hp(hp(y, hp_hz, 4, sr), hp_hz, 4, sr)
