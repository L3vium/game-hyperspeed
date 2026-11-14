"""Physical motion : Doppler renderer (solves the emission time of every received sample, cubic read, 1/r,
time-varying air absorption, ITD / ILD above 250 Hz only, ground reflection image source). Helpers pass_by()
and orbit(). Listener at the origin, ears on the x axis, y up, facing +z."""
import numpy as np

from ..config import SR
from . import filt, stft
from .filt import frac_read, air_alpha_db_per_m

C_SOUND = 343.0
EAR = 0.0875


def _read(src, pos_e, t_out, sr):
    """Received signal at a point : for output times t_out find emission times te with te + r(te)/c = t_out."""
    n_e = len(pos_e)
    te = np.arange(n_e) / sr
    r = np.linalg.norm(pos_e, axis=1)
    ta = te + r / C_SOUND
    # arrival times are monotonic while the source is subsonic
    ta = np.maximum.accumulate(ta)
    te_out = np.interp(t_out, ta, te, left=-1.0, right=-1.0)
    r_out = np.interp(t_out, ta, r)
    y = frac_read(src, te_out * sr)
    y[te_out < 0] = 0.0
    return y, r_out


def render(src, path, sr=SR, ground=0.5, listener_h=1.6, r_min=1.0, absorb=True, ild_db=6.0, out_len=None, focus=0.35):
    """Render mono `src` emitted along `path` (n,3 metres, one row per source sample) to a stereo pair.
    Low band (< 250 Hz) comes from the head centre read (mono) ; ears only differ above 250 Hz."""
    src = np.asarray(src, dtype=np.float64)
    n = len(src)
    pos = np.asarray(path, dtype=np.float64)
    out_len = out_len or n
    t_out = np.arange(out_len) / sr
    lis = np.array([0.0, listener_h, 0.0])
    rel = pos - lis
    centre, rc = _read(src, rel, t_out, sr)
    gain_c = 1.0 / np.maximum(rc, r_min)
    if ground:
        img = pos.copy()
        img[:, 1] = -img[:, 1]
        refl, rr = _read(src, img - lis, t_out, sr)
        refl = filt.lp(refl, 2500.0) * ground
        centre = centre * gain_c + refl / np.maximum(rr, r_min)
    else:
        centre = centre * gain_c
    ears = []
    for side in (-1.0, 1.0):
        e_rel = rel - np.array([side * EAR, 0.0, 0.0])
        y, re = _read(src, e_rel, t_out, sr)
        y = y / np.maximum(re, r_min)
        # head shadow : the far ear loses highs (azimuth from the x component)
        az = np.interp(t_out, np.arange(n) / sr, rel[:, 0] / np.maximum(np.linalg.norm(rel, axis=1), 1e-6))
        shadow = np.clip(-side * az, 0.0, 1.0)
        y = y * 10 ** (-ild_db * shadow * 0.25 / 20.0)
        dark = filt.lp(y, 2500.0)
        y = y + (dark - y) * shadow * 0.6
        ears.append(y)
    low_c, hi_c = filt.lr_split(centre, 250.0, sr)
    out = []
    for y in ears:
        _, hi = filt.lr_split(y, 250.0, sr)
        # focus : part of the high band comes from the centre read, keeping the pair mono-compatible (corr > 0)
        out.append(low_c + (1.0 - focus) * hi + focus * hi_c)
    out = np.stack(out, axis=1)
    if absorb:
        # time-varying air absorption from the distance per STFT frame
        nfft, hop = 1024, 256
        tt, ff = stft.frame_axes(out_len, nfft, hop, sr)
        rr = np.interp(np.clip(tt, 0, None), t_out, rc)
        alpha = air_alpha_db_per_m(ff)[None, :]
        G = 10 ** (-(alpha * rr[:, None]) / 20.0)
        for ch in range(2):
            X = stft.stft(out[:, ch], nfft, hop)
            out[:, ch] = stft.istft(X * G[:len(X)], out_len, nfft, hop)
    return out


def pass_by(src, speed, d_min, side=1.0, height=0.0, t_closest=None, sr=SR, **kw):
    """Straight-line fly-by at `speed` m/s passing `d_min` m to the side (side -1 left / +1 right), closest
    approach at t_closest seconds (default : middle of the source)."""
    n = len(src)
    t = np.arange(n) / sr
    tc = t_closest if t_closest is not None else n / sr * 0.5
    path = np.stack([np.full(n, side * d_min), np.full(n, 1.6 + height), (t - tc) * speed], axis=1)
    return render(src, path, sr, **kw)


def orbit(src, radius=1.5, turns=1.0, height=0.0, start_angle=0.0, sr=SR, **kw):
    """Source circling the listener (flips, spins) : `turns` revolutions over the source length."""
    n = len(src)
    a = start_angle + 2 * np.pi * turns * np.arange(n) / n
    path = np.stack([radius * np.sin(a), np.full(n, 1.6 + height), radius * np.cos(a)], axis=1)
    return render(src, path, sr, ground=0.0, r_min=0.5, **kw)


def path_points(points, n, sr=SR):
    """Piecewise-linear path through [(t, (x, y, z)), ...] sampled for n samples."""
    t = np.arange(n) / sr
    ts = np.array([p[0] for p in points])
    xyz = np.array([p[1] for p in points], dtype=np.float64)
    return np.stack([np.interp(t, ts, xyz[:, k]) for k in range(3)], axis=1)
