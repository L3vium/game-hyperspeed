"""ITU-R BS.1770-4 / EBU R128 loudness : K-weighting for any sample rate, gated integrated loudness, momentary
(400 ms), short-term (3 s), loudness range (EBU 3342) and true peak (4x oversampled)."""
import functools

import numpy as np
from scipy import signal

from ..config import SR
from .util import to_stereo, upsample


@functools.lru_cache(maxsize=8)
def k_sos(sr=SR):
    """K-weighting (pre-filter high shelf + RLB high pass) for any rate, from the analog prototypes."""
    G = 3.999843853973347
    Q = 0.7071752369554196
    fc = 1681.974450955533
    K = np.tan(np.pi * fc / sr)
    Vh = 10 ** (G / 20.0)
    Vb = Vh ** 0.4996667741545416
    a0 = 1.0 + K / Q + K * K
    b = [(Vh + Vb * K / Q + K * K) / a0, 2.0 * (K * K - Vh) / a0, (Vh - Vb * K / Q + K * K) / a0]
    a = [1.0, 2.0 * (K * K - 1.0) / a0, (1.0 - K / Q + K * K) / a0]
    fc2 = 38.13547087602444
    Q2 = 0.5003270373238773
    K = np.tan(np.pi * fc2 / sr)
    a0 = 1.0 + K / Q2 + K * K
    b2 = [1.0, -2.0, 1.0]
    a2 = [1.0, 2.0 * (K * K - 1.0) / a0, (1.0 - K / Q2 + K * K) / a0]
    return np.array([b + a, b2 + a2])


def kweight(x, sr=SR):
    return signal.sosfilt(k_sos(sr), to_stereo(x), axis=0)


def _power_series(x, win_s, hop_s, sr=SR, pad_short=True):
    """Mean-square (summed over channels) of the K-weighted signal in sliding windows."""
    y = kweight(x, sr)
    p = np.sum(y * y, axis=1)
    w = int(round(win_s * sr))
    h = max(1, int(round(hop_s * sr)))
    if len(p) < w:
        if not pad_short:
            return np.array([np.mean(p)])
        p = np.pad(p, (0, w - len(p)))
    c = np.concatenate([[0.0], np.cumsum(p)])
    starts = np.arange(0, len(p) - w + 1, h)
    return (c[starts + w] - c[starts]) / w


def _lufs(power):
    return -0.691 + 10.0 * np.log10(np.maximum(power, 1e-20))


def integrated(x, sr=SR):
    """Gated integrated loudness (LUFS) : 400 ms blocks, 75 % overlap, -70 LUFS absolute and -10 LU relative gates."""
    pw = _power_series(x, 0.4, 0.1, sr)
    l = _lufs(pw)
    keep = l > -70.0
    if not np.any(keep):
        return -120.0
    rel = _lufs(np.mean(pw[keep])) - 10.0
    keep &= l > rel
    if not np.any(keep):
        return -120.0
    return float(_lufs(np.mean(pw[keep])))


def momentary(x, sr=SR, hop_s=0.01):
    """Momentary loudness series (400 ms windows)."""
    return _lufs(_power_series(x, 0.4, hop_s, sr))


def short_term(x, sr=SR, hop_s=0.1):
    """Short-term loudness series (3 s windows)."""
    return _lufs(_power_series(x, 3.0, hop_s, sr))


def momentary_max(x, sr=SR):
    return float(np.max(momentary(x, sr)))


def short_term_max(x, sr=SR):
    return float(np.max(short_term(x, sr)))


def lra(x, sr=SR):
    """Loudness range (EBU Tech 3342) : 10th..95th percentile of gated short-term loudness."""
    st = short_term(x, sr, 0.1)
    st = st[st > -70.0]
    if len(st) < 2:
        return 0.0
    rel = _lufs(np.mean(10 ** ((st + 0.691) / 10.0))) - 20.0
    st = st[st > rel]
    if len(st) < 2:
        return 0.0
    return float(np.percentile(st, 95) - np.percentile(st, 10))


def true_peak_series(x, os=4):
    """Per-sample true-peak estimate : max |x| over the 4x oversampled interval of each sample (all channels)."""
    x = to_stereo(x)
    n = len(x)
    a = np.empty(n)
    chunk, pad = 1 << 18, 128
    for c0 in range(0, n, chunk):
        c1 = min(n, c0 + chunk)
        s0, s1 = max(0, c0 - pad), min(n, c1 + pad)
        up = upsample(x[s0:s1], os)
        seg = np.abs(up).max(axis=1)
        seg = np.pad(seg, (0, max(0, (s1 - s0) * os - len(seg))))[:(s1 - s0) * os].reshape(s1 - s0, os).max(axis=1)
        a[c0:c1] = seg[c0 - s0:c0 - s0 + (c1 - c0)]
    a = np.maximum(a, np.abs(x).max(axis=1))
    a[1:] = np.maximum(a[1:], a[:-1])
    return a


def true_peak(x, os=4):
    """True peak in dBTP."""
    return float(20.0 * np.log10(np.max(true_peak_series(x, os)) + 1e-12))


def measure(x, metric, sr=SR):
    if metric == "I":
        return integrated(x, sr)
    if metric == "M":
        return momentary_max(x, sr)
    if metric == "S":
        return short_term_max(x, sr)
    raise ValueError(metric)
