"""Oscillators : PolyBLEP saw / pulse / triangle (vectorised, optional oversampling), supersaw, phase-modulation FM,
Karplus-Strong, coloured noise from shaped spectra (periodic -> seamless loops), velvet noise, smooth random LFOs."""
import numpy as np
from scipy import signal

from ..config import SR
from .util import ctrl_up, downsample


# ------------------------------------------------------------------ phase and BLEP

def phase(freq, n, sr=SR, phase0=0.0):
    """Phase in cycles [0,1) per sample and the per-sample increment for a (possibly time-varying) frequency."""
    inc = ctrl_up(freq, n) / sr
    ph = phase0 + np.cumsum(inc) - inc
    return ph % 1.0, inc


def _polyblep(t, dt):
    """Band-limited step residual from the integrated cubic B-spline (4-point BLEP), x2 for a ramp wrapping at t = 0.
    At 2x oversampling a 3.5 kHz saw keeps its inharmonic (aliased) energy under -75 dB."""
    out = np.zeros_like(t)
    dt = np.maximum(dt, 1e-12)
    m = t < 2.0 * dt
    d = t[m] / dt[m]
    s = np.where(d < 1.0, 0.5 + 2.0 * d / 3.0 - d ** 3 / 3.0 + d ** 4 / 8.0, 1.0 - (2.0 - d) ** 4 / 24.0)
    out[m] = 2.0 * (s - 1.0)
    m = t > 1.0 - 2.0 * dt
    d = (t[m] - 1.0) / dt[m]
    s = np.where(d > -1.0, 0.5 + 2.0 * d / 3.0 - d ** 3 / 3.0 - d ** 4 / 8.0, (2.0 + d) ** 4 / 24.0)
    out[m] = 2.0 * s
    return out


def _saw_core(ph, inc):
    return 2.0 * ph - 1.0 - _polyblep(ph, inc)


def saw(freq, n, sr=SR, phase0=0.0, os=2):
    """Band-limited sawtooth. Rendered at os x the rate then decimated through a Kaiser FIR."""
    m = n * os
    ph, inc = phase(np.asarray(freq, dtype=np.float64) if np.ndim(freq) == 0 else ctrl_up(freq, m), m, sr * os, phase0)
    y = _saw_core(ph, inc)
    return downsample(y, os)[:n] if os > 1 else y


def pulse(freq, n, width=0.5, sr=SR, phase0=0.0, os=2):
    """Band-limited pulse from two BLEP saws (width may be an array for PWM)."""
    m = n * os
    f = np.asarray(freq, dtype=np.float64) if np.ndim(freq) == 0 else ctrl_up(freq, m)
    w = ctrl_up(width, m) if np.ndim(width) else np.full(m, float(width))
    ph, inc = phase(f, m, sr * os, phase0)
    ph2 = (ph - w) % 1.0
    y = _saw_core(ph, inc) - _saw_core(ph2, inc) - (2.0 * w - 1.0)
    return downsample(y, os)[:n] if os > 1 else y


def triangle(freq, n, sr=SR, phase0=0.0, os=2):
    """Band-limited triangle : leaky integration of a BLEP square."""
    m = n * os
    f = np.asarray(freq, dtype=np.float64) if np.ndim(freq) == 0 else ctrl_up(freq, m)
    ph, inc = phase(f, m, sr * os, phase0)
    sq = _saw_core(ph, inc) - _saw_core((ph - 0.5) % 1.0, inc)
    y = signal.lfilter([1.0], [1.0, -0.9995], sq * 4.0 * inc, zi=[1.0])[0] if m else sq
    y = y - np.mean(y)
    pk = np.max(np.abs(y)) + 1e-12
    y = y / pk
    return downsample(y, os)[:n] if os > 1 else y


def sine(freq, n, sr=SR, phase0=0.0):
    ph, _ = phase(freq, n, sr, phase0)
    return np.sin(2.0 * np.pi * ph)


# JP-8000 style detune spread (normalised) for 7 voices.
_SUPERSAW_SPREAD = np.array([-0.11002313, -0.06288439, -0.01952356, 0.0, 0.01991221, 0.06216538, 0.10745242])


def supersaw(freq, n, rng, detune=0.35, voices=7, centre=0.5, sr=SR, os=2):
    """7 detuned BLEP saws with random phases per note; centre voice weighted, high-passed under the fundamental."""
    spread = _SUPERSAW_SPREAD if voices == 7 else np.linspace(-0.11, 0.11, voices)
    out = np.zeros(n)
    f = np.asarray(freq, dtype=np.float64)
    for i, s in enumerate(spread):
        g = centre if s == 0.0 else (1.0 - centre) * 2.0 / (voices - 1) * 1.6
        out += saw(f * (1.0 + s * detune * 0.25), n, sr, rng.uniform(0, 1), os) * g
    f0 = float(np.min(f)) if np.ndim(f) else float(f)
    sos = signal.butter(2, max(20.0, f0 * 0.7), "high", fs=sr, output="sos")
    return signal.sosfilt(sos, out)


def pm(freq, n, ratios, indices, sr=SR, feedback=0.0, phases=None, detune_hz=None):
    """Phase-modulation stack : op[k] modulates op[k-1]; op[0] is the carrier.
    ratios : frequency ratios per operator. indices : modulation index (scalar or array per sample) of op[k>=1].
    feedback applies a one-iteration self modulation to the top operator."""
    f = np.asarray(freq, dtype=np.float64)
    k = len(ratios)
    phases = phases if phases is not None else [0.0] * k
    detune_hz = detune_hz if detune_hz is not None else [0.0] * k
    mod = np.zeros(n)
    for i in range(k - 1, -1, -1):
        ph, _ = phase(f * ratios[i] + detune_hz[i], n, sr, phases[i])
        arg = 2.0 * np.pi * ph
        if i == k - 1 and feedback:
            arg = arg + feedback * np.sin(arg + feedback * np.sin(arg))
        y = np.sin(arg + mod)
        if i > 0:
            idx = indices[i - 1] if i - 1 < len(indices) else 0.0
            mod = ctrl_up(idx, n) * y if np.ndim(idx) else float(idx) * y
        else:
            return y
    return mod


def karplus_strong(freq, n, rng, decay=1.5, bright=0.5, pick=0.13, sr=SR, exc_len=None):
    """Plucked string : lfilter loop with a two-tap damping filter and linear fractional delay.
    decay : seconds to -60 dB. bright : 0 (dark) .. 1 (bright)."""
    period = sr / float(freq)
    s = 0.5 * (1.0 - np.clip(bright, 0.0, 1.0)) + 0.02
    total = period - s
    L = int(np.floor(total))
    fr = total - L
    g = 10.0 ** (-3.0 / (decay * float(freq)))
    c = np.convolve([1.0 - s, s], [1.0 - fr, fr])
    a = np.zeros(L + 3)
    a[0] = 1.0
    a[L:L + 3] -= g * c
    m = exc_len or max(4, int(period))
    exc = rng.uniform(-1, 1, m)
    exc = signal.lfilter([0.5 + 0.5 * bright], [1.0, -(0.5 - 0.5 * bright)], exc)
    d = max(1, int(pick * period))
    exc[d:] -= exc[:-d] * 0.9
    exc = np.concatenate([exc, np.zeros(max(0, n - m))])[:n]
    y = signal.lfilter([1.0], a, exc)
    return y / (np.max(np.abs(y)) + 1e-12)


# ------------------------------------------------------------------ noise

def white(n, rng):
    return rng.standard_normal(n)


def colored(n, rng, slope_db_oct=-3.0, lo=None, hi=None, sr=SR):
    """Noise with a power-law spectrum built in the frequency domain. Periodic over n : loops seamlessly.
    slope 0 = white, -3 = pink, -6 = brown. lo/hi : optional hard band limits (Hz). RMS 1."""
    nf = n // 2 + 1
    f = np.fft.rfftfreq(n, 1.0 / sr)
    mag = np.ones(nf)
    nz = f > 0
    mag[nz] = (f[nz] / 1000.0) ** (slope_db_oct / 6.0206)
    mag[0] = 0.0
    if lo is not None:
        mag[f < lo] = 0.0
    if hi is not None:
        mag[f > hi] = 0.0
    spec = mag * (rng.standard_normal(nf) + 1j * rng.standard_normal(nf))
    y = np.fft.irfft(spec, n)
    return y / (np.sqrt(np.mean(y * y)) + 1e-12)


def pink(n, rng, sr=SR):
    return colored(n, rng, -3.0, sr=sr)


def brown(n, rng, sr=SR):
    return colored(n, rng, -6.0, lo=15.0, sr=sr)


def velvet(n, rng, density=2000.0, sr=SR):
    """Velvet noise : one +-1 impulse at a random position in each grid cell of sr/density samples."""
    grid = max(1, int(sr / density))
    cells = n // grid
    out = np.zeros(n)
    pos = np.arange(cells) * grid + rng.integers(0, grid, cells)
    out[pos] = rng.choice([-1.0, 1.0], cells)
    return out


def smooth_random(n, rate_hz, rng, sr=SR, periodic=True):
    """Smooth random control signal in [-1, 1] with energy up to rate_hz. Periodic over n (FFT built)."""
    nf = n // 2 + 1
    f = np.fft.rfftfreq(n, 1.0 / sr)
    mag = np.exp(-0.5 * (f / max(rate_hz, 1e-3)) ** 2)
    mag[0] = 0.0
    spec = mag * (rng.standard_normal(nf) + 1j * rng.standard_normal(nf))
    y = np.fft.irfft(spec, n)
    if not np.any(y):
        return y
    return y / (np.max(np.abs(y)) + 1e-12)


def periodic_lfo(n, cycles, rng=None, shape="sine", phase0=0.0):
    """An LFO doing an integer number of cycles over n samples (seamless when the buffer loops)."""
    ph = (np.arange(n) / n * cycles + phase0) % 1.0
    if shape == "sine":
        return np.sin(2 * np.pi * ph)
    if shape == "tri":
        return 1.0 - 4.0 * np.abs(ph - 0.5)
    return 2.0 * ph - 1.0
