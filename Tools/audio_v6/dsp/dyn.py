"""Dynamics : feed-forward compressor (soft knee, external sidechain, gain curve export), 3-band + OTT, transient
shaper, analytic duck curves from known trigger times, true-peak lookahead limiter."""
import numpy as np
from scipy import ndimage

from ..config import SR
from .util import to_stereo, undb
from .filt import lr_bands
from .loudness import true_peak_series


def _smooth_gr(gr_db, attack_s, release_s, hop, sr):
    """Asymmetric one-pole smoothing of a gain-reduction curve (dB, >= 0) at control rate."""
    ca = np.exp(-hop / (max(attack_s, 1e-5) * sr))
    cr = np.exp(-hop / (max(release_s, 1e-5) * sr))
    out = np.empty(len(gr_db))
    e = 0.0
    for i, v in enumerate(gr_db.tolist()):
        c = ca if v > e else cr
        e = v + c * (e - v)
        out[i] = e
    return out


def comp_gain(detector, thresh_db=-18.0, ratio=2.0, knee_db=6.0, attack=0.01, release=0.12, hop=16, sr=SR, rms_win=0.0):
    """Gain curve (linear, per sample) of a feed-forward compressor driven by `detector` (mono or stereo)."""
    d = np.abs(to_stereo(detector)).max(axis=1)
    n = len(d)
    nb = -(-n // hop)
    dp = np.pad(d, (0, nb * hop - n))
    if rms_win > 0:
        sq = np.pad(to_stereo(detector) ** 2, ((0, nb * hop - n), (0, 0))).mean(axis=1)
        w = max(1, int(rms_win * sr / hop))
        lvl = np.sqrt(np.convolve(sq.reshape(nb, hop).mean(axis=1), np.ones(w) / w, mode="same")) * np.sqrt(2)
    else:
        lvl = dp.reshape(nb, hop).max(axis=1)
    ldb = 20 * np.log10(np.maximum(lvl, 1e-9))
    over = ldb - thresh_db
    slope = 1.0 - 1.0 / ratio
    gr = np.where(over <= -knee_db / 2, 0.0,
                  np.where(over >= knee_db / 2, over * slope, slope * (over + knee_db / 2) ** 2 / (2 * max(knee_db, 1e-6))))
    gr = _smooth_gr(gr, attack, release, hop, sr)
    g = undb(-gr)
    return np.interp(np.arange(n), np.arange(nb) * hop + hop // 2, g)


def compress(x, thresh_db=-18.0, ratio=2.0, knee_db=6.0, attack=0.01, release=0.12, makeup_db=0.0, sidechain=None, **kw):
    g = comp_gain(x if sidechain is None else sidechain, thresh_db, ratio, knee_db, attack, release, **kw)
    x = np.asarray(x, dtype=np.float64)
    return x * (g if x.ndim == 1 else g[:, None]) * undb(makeup_db)


def ott(x, depth=0.5, freqs=(120.0, 2500.0), target_db=-24.0, up_ratio=3.0, down_ratio=4.0, up_max_db=18.0, sr=SR):
    """3-band upward + downward compressor (OTT-like) mixed in parallel with the dry by depth."""
    bands = lr_bands(np.asarray(x, dtype=np.float64), list(freqs), sr)
    out = np.zeros_like(bands[0])
    hop = 16
    for b in bands:
        d = np.abs(to_stereo(b)).max(axis=1)
        n = len(d)
        nb = -(-n // hop)
        lvl = np.pad(d, (0, nb * hop - n)).reshape(nb, hop).max(axis=1)
        ldb = 20 * np.log10(np.maximum(lvl, 1e-7))
        down = np.where(ldb > target_db, (ldb - target_db) * (1 - 1 / down_ratio), 0.0)
        up = np.where(ldb < target_db, np.minimum((target_db - ldb) * (1 - 1 / up_ratio), up_max_db), 0.0)
        up = np.where(ldb < -80, 0.0, up)
        gr = _smooth_gr(down - up + up_max_db, 0.003, 0.08, hop, sr) - up_max_db
        g = np.interp(np.arange(n), np.arange(nb) * hop, undb(-gr))
        out = out + (b * (g if b.ndim == 1 else g[:, None]))
    return (1 - depth) * np.asarray(x) + depth * out


def transient_shaper(x, attack_db=4.0, sustain_db=-2.0, sr=SR):
    """Boost / cut the attack and the sustain from the difference of a fast and a slow envelope."""
    from .env import follower
    m = np.abs(to_stereo(x)).max(axis=1)
    fast = follower(m, 0.0005, 0.03, 8, sr)
    slow = follower(m, 0.02, 0.03, 8, sr)
    diff = np.clip((fast - slow) / (fast + 1e-9), 0.0, 1.0)
    sus = np.clip(slow / (fast + 1e-9), 0.0, 1.0)
    gdb = attack_db * diff + sustain_db * sus * (1 - diff)
    g = undb(gdb)
    x = np.asarray(x, dtype=np.float64)
    return x * (g if x.ndim == 1 else g[:, None])


def duck_curve(n, times_s, depth_db=-6.0, attack=0.003, hold=0.02, release=0.15, sr=SR, depths=None):
    """Analytic ducking gain (linear) from known trigger times : the deepest reduction wins where ducks overlap."""
    gr = np.zeros(n)
    a, h = int(attack * sr), int(hold * sr)
    r = int(release * sr * 5)
    shape = np.concatenate([np.linspace(0, 1, max(a, 1), endpoint=False), np.ones(h),
                            np.exp(-np.arange(r) / (release * sr))])
    for i, t in enumerate(times_s):
        o = int(round(t * sr)) - a
        d = -(depths[i] if depths is not None else depth_db)
        s0 = max(0, -o)
        o2 = max(o, 0)
        k = min(len(shape) - s0, n - o2)
        if k > 0:
            np.maximum(gr[o2:o2 + k], shape[s0:s0 + k] * d, out=gr[o2:o2 + k])
    return undb(-gr)


def duck_curve_circular(n, times_s, **kw):
    """Duck curve for a loop : triggers near the end wrap their release onto the start."""
    g1 = duck_curve(2 * n, list(times_s) + [t + n / kw.get("sr", SR) for t in times_s], **kw)
    return np.minimum(g1[:n], g1[n:])


def limiter(x, ceiling_db=-1.0, attack_ms=1.5, hold_ms=2.5, release_ms=80.0, os=4, max_iter=8, sr=SR, return_gain=False):
    """True-peak lookahead limiter (offline). Returns (y, max_gr_db).
    Required gain from the 4x oversampled peak of every sample -> forward min filter over the hold window ->
    exponential release (dB domain, exact vectorised peak-hold-decay) -> backward box average over the attack
    window (shorter than the hold, so the gain is already down at every peak). Iterates until TP <= ceiling."""
    x = np.asarray(x, dtype=np.float64)
    st = to_stereo(x)
    n = len(st)
    La = max(1, int(attack_ms * sr / 1000.0))
    Lh = max(La, int(hold_ms * sr / 1000.0))
    rel = np.exp(-1.0 / (release_ms * sr / 1000.0))
    ceil = undb(ceiling_db)
    tp = true_peak_series(st, os)
    target = ceil
    g = np.ones(n)
    for _ in range(max_iter):
        req = np.minimum(1.0, target / np.maximum(tp, 1e-12))
        if np.all(req >= 1.0):
            g = np.ones(n)
            y = st
        else:
            # forward min over [n, n + Lh - 1]
            gmin = ndimage.minimum_filter1d(np.pad(req, (0, Lh), constant_values=1.0), size=Lh, origin=-(Lh // 2), mode="nearest")[:n]
            # release in dB : gr[n] = max(gr_in[n], gr[n-1] * rel) computed as a running max in log space
            gr = -20 * np.log10(gmin)
            lg = np.log(np.maximum(gr, 1e-9))
            idx = np.arange(n)
            lr = np.log(rel)
            acc = np.maximum.accumulate(lg - idx * lr)
            gr_r = np.exp(acc + idx * lr)
            gr_r = np.where(gr_r < 1e-6, 0.0, gr_r)
            gl = undb(-gr_r)
            # backward box average over [n - La + 1, n]
            c = np.concatenate([np.full(La, gl[0]), gl])
            cs = np.cumsum(c)
            g = (cs[La:] - cs[:-La]) / La
            g = np.minimum(g, 1.0)
            y = st * g[:, None]
        peak = np.max(true_peak_series(y, os))
        if peak <= ceil * 1.0001:
            break
        target *= ceil / peak * 0.999
    gr_max = float(-20 * np.log10(max(np.min(g), 1e-12)))
    if x.ndim == 1:
        y = y[:, 0]
    if return_gain:
        return y, gr_max, g
    return y, gr_max
