"""Small shared helpers : dB, lengths, placing, resampling with a clean anti-alias FIR, stereo plumbing."""
import functools

import numpy as np
from scipy import signal

from ..config import SR


def db(x):
    return 20.0 * np.log10(np.maximum(np.abs(x), 1e-12))


def undb(d):
    return 10.0 ** (np.asarray(d, dtype=np.float64) / 20.0)


def ns(seconds, sr=SR):
    return int(round(seconds * sr))


def t_axis(n, sr=SR):
    return np.arange(n, dtype=np.float64) / sr


def fit(x, n):
    """Pad with zeros or cut to n samples (along axis 0)."""
    x = np.asarray(x, dtype=np.float64)
    if len(x) >= n:
        return x[:n]
    pad = [(0, n - len(x))] + [(0, 0)] * (x.ndim - 1)
    return np.pad(x, pad)


def place(buf, x, offset, gain=1.0, circular=False):
    """Add x into buf at an integer sample offset. circular=True wraps what overflows to the start (loops)."""
    x = np.asarray(x, dtype=np.float64)
    n = len(buf)
    offset = int(offset)
    if circular:
        offset %= n
        while len(x) > 0:
            k = min(len(x), n - offset)
            buf[offset:offset + k] += x[:k] * gain
            x = x[k:]
            offset = 0
        return buf
    if offset >= n or offset + len(x) <= 0:
        return buf
    s0 = max(0, -offset)
    k = min(len(x) - s0, n - offset - s0)
    buf[offset + s0:offset + s0 + k] += x[s0:s0 + k] * gain
    return buf


def mix(*parts):
    """Sum signals of different lengths (mono or stereo, broadcast mono into stereo)."""
    parts = [np.asarray(p, dtype=np.float64) for p in parts if p is not None]
    n = max(len(p) for p in parts)
    stereo = any(p.ndim == 2 for p in parts)
    out = np.zeros((n, 2)) if stereo else np.zeros(n)
    for p in parts:
        if stereo and p.ndim == 1:
            out[:len(p)] += p[:, None]
        else:
            out[:len(p)] += p
    return out


def delay(x, seconds, sr=SR):
    d = ns(seconds, sr)
    if d <= 0:
        return np.asarray(x, dtype=np.float64)
    pad = [(d, 0)] + [(0, 0)] * (np.ndim(x) - 1)
    return np.pad(np.asarray(x, dtype=np.float64), pad)


def to_stereo(x):
    x = np.asarray(x, dtype=np.float64)
    if x.ndim == 1:
        return np.stack([x, x], axis=1)
    return x


def to_mono(x):
    x = np.asarray(x, dtype=np.float64)
    if x.ndim == 2:
        return x.mean(axis=1)
    return x


def ms_encode(x):
    x = to_stereo(x)
    return (x[:, 0] + x[:, 1]) * 0.5, (x[:, 0] - x[:, 1]) * 0.5


def ms_decode(m, s):
    return np.stack([m + s, m - s], axis=1)


def pan(x, p):
    """Constant-power pan of a mono signal, p in -1..1."""
    a = (np.clip(p, -1, 1) + 1.0) * np.pi / 4.0
    x = np.asarray(x, dtype=np.float64)
    return np.stack([x * np.cos(a), x * np.sin(a)], axis=1) * np.sqrt(2.0)


def fade(x, fade_in=0.0005, fade_out=0.005, sr=SR):
    """Raised-cosine fades at both ends (in place on a copy)."""
    x = np.array(x, dtype=np.float64)
    a, b = ns(fade_in, sr), ns(fade_out, sr)
    if a > 0:
        w = 0.5 - 0.5 * np.cos(np.linspace(0, np.pi, a))
        x[:a] *= w if x.ndim == 1 else w[:, None]
    if b > 0:
        w = 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, b))
        x[-b:] *= w if x.ndim == 1 else w[:, None]
    return x


def trim_tail(x, floor_db=-80.0, keep=0.01, sr=SR):
    """Cut a one-shot after its last sample above floor (relative to peak), keeping a short fade."""
    x = np.asarray(x, dtype=np.float64)
    a = np.abs(x) if x.ndim == 1 else np.abs(x).max(axis=1)
    pk = a.max() + 1e-12
    idx = np.nonzero(a > pk * 10 ** (floor_db / 20.0))[0]
    if len(idx) == 0:
        return x
    end = min(len(x), idx[-1] + ns(keep, sr))
    return fade(x[:end], 0.0, min(0.02, end / sr * 0.25), sr)


@functools.lru_cache(maxsize=16)
def aa_fir(factor):
    """Anti-alias / anti-image FIR for an integer rate change : cutoff 0.9 of the low Nyquist, Kaiser beta 10."""
    taps = 64 * factor + 1
    return signal.firwin(taps, 0.9 / factor, window=("kaiser", 10.0))


def upsample(x, factor):
    if factor == 1:
        return np.asarray(x, dtype=np.float64)
    return signal.resample_poly(x, factor, 1, axis=0, window=aa_fir(factor))


def downsample(x, factor):
    if factor == 1:
        return np.asarray(x, dtype=np.float64)
    return signal.resample_poly(x, 1, factor, axis=0, window=aa_fir(factor))


def ctrl_up(c, n_out):
    """Stretch a control signal (scalar or array) to n_out samples by linear interpolation."""
    c = np.asarray(c, dtype=np.float64)
    if c.ndim == 0:
        return np.full(n_out, float(c))
    if len(c) == n_out:
        return c
    src = np.linspace(0.0, 1.0, len(c))
    dst = np.linspace(0.0, 1.0, n_out)
    return np.interp(dst, src, c)


def normalize_peak(x, peak=1.0):
    m = np.max(np.abs(x)) + 1e-12
    return np.asarray(x, dtype=np.float64) * (peak / m)


def rms(x):
    x = np.asarray(x, dtype=np.float64)
    return float(np.sqrt(np.mean(x * x) + 1e-30))


def rng_of(seed):
    return np.random.default_rng(seed)
