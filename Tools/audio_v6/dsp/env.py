"""Envelopes : breakpoints with curvature, ADSR with curves, exponential decays, control-rate followers."""
import numpy as np

from ..config import SR
from .util import ns


def _shape(u, curve):
    """Map 0..1 to 0..1 with curvature : 0 linear, > 0 slow start (exponential rise), < 0 fast start."""
    if abs(curve) < 1e-6:
        return u
    return (np.exp(curve * u) - 1.0) / (np.exp(curve) - 1.0)


def bp(points, n, sr=SR):
    """Breakpoint envelope. points : [(t_seconds, value) or (t, value, curve)] ; curve applies to the segment that
    ends at that point. Holds the last value."""
    out = np.empty(n)
    pts = [(p[0], p[1], p[2] if len(p) > 2 else 0.0) for p in points]
    if pts[0][0] > 0:
        pts.insert(0, (0.0, pts[0][1], 0.0))
    out[:] = pts[-1][1]
    for (t0, v0, _), (t1, v1, c) in zip(pts[:-1], pts[1:]):
        i0, i1 = ns(t0, sr), ns(t1, sr)
        i1 = min(i1, n)
        if i1 <= i0 or i0 >= n:
            continue
        u = np.linspace(0.0, 1.0, ns(t1, sr) - i0, endpoint=False)[:i1 - i0]
        out[i0:i1] = v0 + (v1 - v0) * _shape(u, c)
    return out


def adsr(n, a=0.005, d=0.1, s=0.7, r=0.1, gate=None, curve_a=0.0, curve_d=-4.0, curve_r=-4.0, sr=SR):
    """ADSR. gate : seconds the key is held (default : n minus release). Curves : negative = fast start."""
    gate = (n / sr - r) if gate is None else gate
    gate = max(gate, 1.0 / sr)
    pts = [(0.0, 0.0), (a, 1.0, curve_a), (a + d, s, curve_d)]
    if gate > a + d:
        pts.append((gate, s))
        pts.append((gate + r, 0.0, curve_r))
    else:
        # released during attack / decay : release from the level reached at the gate
        lvl = float(bp(pts, ns(gate, sr) + 1, sr)[-1])
        pts = [p for p in pts if p[0] < gate] + [(gate, lvl), (gate + r, 0.0, curve_r)]
    return bp(pts, n, sr)


def decay(n, tau, sr=SR, attack=0.0005):
    """Exponential decay with time constant tau (s) and a short raised-cosine attack."""
    t = np.arange(n) / sr
    e = np.exp(-t / max(tau, 1e-6))
    a = ns(attack, sr)
    if a > 1:
        e[:a] *= 0.5 - 0.5 * np.cos(np.linspace(0, np.pi, a))
    return e


def t60(n, seconds, sr=SR, attack=0.0005):
    """Exponential decay reaching -60 dB after `seconds`."""
    return decay(n, seconds / 6.9078, sr, attack)


def ar(n, attack, release, sr=SR, curve=-3.0):
    """Attack then release ramp (swell) over n samples : attack seconds up, release seconds down."""
    return bp([(0.0, 0.0), (attack, 1.0, 2.0), (attack + release, 0.0, curve)], n, sr)


def follower(x, attack=0.005, release=0.08, hop=32, sr=SR):
    """Control-rate peak follower (asymmetric one-pole), returned at audio rate."""
    x = np.abs(np.asarray(x, dtype=np.float64))
    if x.ndim == 2:
        x = x.max(axis=1)
    n = len(x)
    nb = -(-n // hop)
    pk = np.pad(x, (0, nb * hop - n)).reshape(nb, hop).max(axis=1)
    ca = np.exp(-hop / (attack * sr))
    cr = np.exp(-hop / (release * sr))
    out = np.empty(nb)
    e = 0.0
    for i in range(nb):
        v = pk[i]
        c = ca if v > e else cr
        e = v + c * (e - v)
        out[i] = e
    return np.interp(np.arange(n), np.arange(nb) * hop + hop // 2, out)
