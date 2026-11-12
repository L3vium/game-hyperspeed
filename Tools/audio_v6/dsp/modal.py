"""Modal synthesis : banks of damped sinusoids excited by a half-sine contact (0.1 ms hard .. 3 ms soft).
Upper modes decay faster ; per-take +-2-4 % frequency and +-20 % decay jitter."""
import numpy as np
from scipy import signal

from ..config import SR

RATIOS = {
    "bar": [1.0, 2.756, 5.404, 8.933, 13.34],
    "membrane": [1.0, 1.594, 2.136, 2.296, 2.653, 2.918, 3.156, 3.501, 3.600, 3.652],
    "bell": [0.5, 1.0, 1.183, 1.506, 2.0, 2.514, 2.662, 3.011, 4.166, 5.433],
    "glass": [1.0, 2.32, 4.25, 6.63, 9.38, 12.6],
    "tube": [1.0, 2.76, 5.40, 8.93, 13.3, 19.9],
    "string": [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0],
}


def plate_ratios(rng, count=24, spread=6.0):
    """Random plate / shard modes : dense, inharmonic, roughly sqrt-spaced like a 2-D plate."""
    k = np.sort(rng.uniform(1.0, spread ** 2, count))
    return list(np.sqrt(k))


def contact(ms, sr=SR):
    """Half-sine contact pulse (unit area) : short = hard and bright, long = soft and dull."""
    n = max(2, int(ms * sr / 1000.0))
    p = np.sin(np.pi * (np.arange(n) + 0.5) / n)
    return p / p.sum()


def modes(f0, ratios, n, rng, decay=0.5, damp=1.0, amps=None, contact_ms=0.3, fjit=0.03, djit=0.2, sr=SR,
          phase_random=False):
    """Sum of damped sinusoids. decay : T60-ish seconds of the fundamental ; damp : how much faster upper modes
    die (decay_k = decay / ratio^damp). amps default 1/sqrt(k). Excited by a half-sine contact."""
    t = np.arange(n) / sr
    out = np.zeros(n)
    ratios = np.asarray(ratios, dtype=np.float64)
    if amps is None:
        amps = 1.0 / np.sqrt(np.arange(1, len(ratios) + 1))
    for r, a in zip(ratios, amps):
        f = f0 * r * (1.0 + rng.uniform(-fjit, fjit))
        if f >= sr * 0.45:
            continue
        d = decay / (r ** damp) * (1.0 + rng.uniform(-djit, djit))
        ph = rng.uniform(0, 2 * np.pi) if phase_random else 0.0
        out += a * rng.choice([-1.0, 1.0]) * np.exp(-6.9078 * t / max(d, 1e-4)) * np.sin(2 * np.pi * f * t + ph)
    exc = contact(contact_ms, sr)
    return signal.fftconvolve(out, exc)[:n] * len(exc) ** 0.0


def hit(f0, kind, n, rng, **kw):
    ratios = plate_ratios(rng) if kind == "plate" else RATIOS[kind]
    return modes(f0, ratios, n, rng, **kw)
