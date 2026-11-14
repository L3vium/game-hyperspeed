"""Per-file measurements : loudness (I / M / S), LRA, true peak, crest, PLR, spectral centroid / rolloff /
flatness, energy < 40 Hz and > 16 kHz, DC, clipping, L/R correlation (overall and < 150 Hz), mono-sum loss,
leading silence, time to peak, tail end, loop seams, and compact features for take-diversity checks."""
import numpy as np

from ..config import SR
from ..dsp import loudness, filt
from ..dsp.loops import seam_metrics
from ..dsp.util import to_stereo


def _band_energy_db(spec_pow, f, lo, hi):
    tot = spec_pow.sum() + 1e-30
    m = (f >= lo) & (f < hi)
    return float(10 * np.log10(spec_pow[m].sum() / tot + 1e-30))


def _corr(a, b):
    sa = np.sqrt(np.sum(a * a))
    sb = np.sqrt(np.sum(b * b))
    if sa < 1e-9 or sb < 1e-9:
        return 1.0
    return float(np.sum(a * b) / (sa * sb))


def features(x, sr=SR):
    """Spectral envelope (48 log bands, dB, level-normalised) + RMS envelope (10 ms frames, first 3 s)."""
    m = to_stereo(x).mean(axis=1)
    n = len(m)
    nfft = 4096
    pad = max(nfft, n)
    P = np.abs(np.fft.rfft(m, pad)) ** 2
    f = np.fft.rfftfreq(pad, 1.0 / sr)
    edges = np.geomspace(40, 18000, 49)
    bands = np.array([P[(f >= edges[i]) & (f < edges[i + 1])].sum() + 1e-20 for i in range(48)])
    bands = 10 * np.log10(bands / bands.sum())
    hop = int(0.01 * sr)
    k = min(n // hop, 300)
    env = np.sqrt(np.mean(m[:k * hop].reshape(k, hop) ** 2, axis=1) + 1e-12) if k > 0 else np.zeros(1)
    return dict(spec=bands.tolist(), env=(env / (env.max() + 1e-12)).tolist())


def compute(x, sr=SR, loop=False, gr=0.0):
    x = np.asarray(x, dtype=np.float64)
    mono_file = x.ndim == 1
    st = to_stereo(x)
    n = len(st)
    dur = n / sr
    out = dict(duration=dur, channels=1 if mono_file else 2, limiter_gr=float(gr))
    out["lufs_i"] = loudness.integrated(st, sr)
    mom = loudness.momentary(st, sr)
    out["lufs_m_max"] = float(np.max(mom))
    out["lufs_s_max"] = loudness.short_term_max(st, sr)
    out["lra"] = loudness.lra(st, sr) if dur > 3.5 else 0.0
    out["true_peak"] = loudness.true_peak(st)
    a = np.abs(st).max(axis=1)
    pk = float(a.max()) + 1e-12
    out["sample_peak"] = float(20 * np.log10(pk))
    # active region : from the first sample over -40 dB re peak to where the 10 ms RMS falls 40 dB under its max
    hop = int(0.01 * sr)
    k = max(1, n // hop)
    env = np.sqrt(np.mean((st[:k * hop] ** 2).mean(axis=1).reshape(k, hop), axis=1) + 1e-20)
    act = np.nonzero(env > env.max() * 10 ** (-40 / 20))[0]
    a0 = int(act[0]) * hop if len(act) else 0
    a1 = (int(act[-1]) + 1) * hop if len(act) else n
    seg = st[a0:a1]
    rms_act = np.sqrt(np.mean(seg ** 2)) + 1e-12
    out["crest"] = float(20 * np.log10(pk / rms_act))
    out["plr"] = out["true_peak"] - out["lufs_i"]
    out["psr"] = out["true_peak"] - out["lufs_m_max"]
    # spectrum (power-averaged over the file)
    m = st.mean(axis=1)
    nfft = 8192
    if n >= nfft:
        frames = n // nfft
        P = np.zeros(nfft // 2 + 1)
        w = np.hanning(nfft)
        step = max(1, frames // 64)
        for i in range(0, frames, step):
            P += np.abs(np.fft.rfft(m[i * nfft:(i + 1) * nfft] * w)) ** 2
        f = np.fft.rfftfreq(nfft, 1.0 / sr)
    else:
        P = np.abs(np.fft.rfft(m * np.hanning(n), nfft)) ** 2
        f = np.fft.rfftfreq(nfft, 1.0 / sr)
    Pt = P.sum() + 1e-30
    out["centroid"] = float((P * f).sum() / Pt)
    cs = np.cumsum(P) / Pt
    out["rolloff85"] = float(f[min(len(f) - 1, np.searchsorted(cs, 0.85))])
    sel = (f > 50) & (f < 16000)
    out["flatness"] = float(np.exp(np.mean(np.log(P[sel] + 1e-30))) / (np.mean(P[sel]) + 1e-30))
    out["e_lt40_db"] = _band_energy_db(P, f, 0, 40)
    out["e_gt16k_db"] = _band_energy_db(P, f, 16000, sr / 2 + 1)
    out["e_lt150_db"] = _band_energy_db(P, f, 0, 150)
    out["dc"] = float(np.max(np.abs(st.mean(axis=0))))
    out["clipped"] = int(np.sum(np.abs(st) >= 32767.0 / 32768.0))
    # stereo
    L, R = st[:, 0], st[:, 1]
    out["corr"] = _corr(L, R)
    lo_l = filt.lp(filt.lp(L, 150.0), 150.0)
    lo_r = filt.lp(filt.lp(R, 150.0), 150.0)
    lo_energy = np.mean(lo_l ** 2 + lo_r ** 2) / (np.mean(L ** 2 + R ** 2) + 1e-30)
    out["low_share_db"] = float(10 * np.log10(lo_energy + 1e-30))
    # below -30 dB of the total the low band is not meaningful (reverb residue) : reported as 1.0 (n/a)
    out["corr_lt150"] = _corr(lo_l, lo_r) if lo_energy > 1e-3 else 1.0
    msum = np.mean(((L + R) * 0.5) ** 2)
    out["mono_loss_db"] = float(10 * np.log10((msum + 1e-30) / (np.mean((L ** 2 + R ** 2) * 0.5) + 1e-30)))
    # timing
    thr = pk * 10 ** (-40 / 20)
    first = np.nonzero(a > thr)[0]
    out["lead_ms"] = float(first[0] / sr * 1000) if len(first) else 0.0
    out["sample_peak_ms"] = float(np.argmax(a) / sr * 1000)
    # time to peak : maximum of the 5 ms RMS envelope (1 ms hop) - the perceived onset peak, not one noise sample
    w5, h1 = int(0.005 * sr), int(0.001 * sr)
    e2 = np.concatenate([[0.0], np.cumsum((st ** 2).sum(axis=1))])
    starts = np.arange(0, max(1, n - w5), h1)
    env5 = (e2[np.minimum(starts + w5, n)] - e2[starts])
    out["peak_ms"] = float(starts[np.argmax(env5)] / sr * 1000)
    tail = st[-int(0.005 * sr):]
    out["tail_end_db"] = float(20 * np.log10(np.sqrt(np.mean(tail ** 2)) + 1e-12))
    if loop:
        out.update(seam_metrics(st, sr))
    out["features"] = features(st, sr)
    out["momentary"] = mom[::5].tolist()  # 50 ms resolution, for the plots / report
    return out
