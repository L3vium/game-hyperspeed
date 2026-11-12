"""Granular / event scattering : Poisson and power-law clouds, bouncing debris, clustered bursts (electric arcs),
and a renderer that places designed grains on a buffer (optionally circular for loops)."""
import numpy as np

from ..config import SR
from .util import place


def poisson(duration, rate, rng, t0=0.0):
    """Event times of a Poisson process (rate events/s, scalar or callable rate(t) via thinning)."""
    if callable(rate):
        rmax = max(rate(t) for t in np.linspace(0, duration, 64)) + 1e-9
        times = []
        t = 0.0
        while True:
            t += rng.exponential(1.0 / rmax)
            if t >= duration:
                break
            if rng.uniform() < rate(t) / rmax:
                times.append(t0 + t)
        return np.array(times)
    k = rng.poisson(rate * duration)
    return np.sort(rng.uniform(0, duration, k)) + t0


def power_law(duration, count, rng, alpha=1.6, t0=0.0):
    """Debris : dense at first, thinning out with a power-law of the time since the event."""
    u = rng.uniform(0, 1, count)
    t = (1 - u) ** (-1.0 / (alpha - 1.0)) - 1.0
    t = t / (np.percentile(t, 95) + 1e-9) * duration * 0.6
    return np.sort(t[t < duration]) + t0


def bounce(t0, first, restitution=0.6, count=8, jitter=0.05, rng=None):
    """Bouncing object : intervals shrink by restitution, amplitudes by restitution as well."""
    times, amps = [], []
    t, dt, a = t0, first, 1.0
    for _ in range(count):
        times.append(t)
        amps.append(a)
        if rng is not None:
            dt *= 1.0 + rng.uniform(-jitter, jitter)
        t += dt
        dt *= restitution
        a *= restitution ** 0.8
        if dt < 0.004:
            break
    return np.array(times), np.array(amps)


def clustered(duration, cluster_rate, size, spread, rng, t0=0.0):
    """Clusters of events (electric arcs, crackle bursts) : Poisson cluster centres, `size` mean events each,
    spread (s) exponential within a cluster."""
    centres = poisson(duration, cluster_rate, rng, t0)
    times = []
    for c in centres:
        k = max(1, rng.poisson(size))
        times.extend(c + np.cumsum(rng.exponential(spread / k, k)))
    times = np.array(sorted(times))
    return times[times < t0 + duration]


def render(n, times, grain_fn, rng, amps=None, sr=SR, circular=False):
    """Place grain_fn(i, rng) (mono arrays) at each time."""
    out = np.zeros(n)
    for i, t in enumerate(times):
        g = grain_fn(i, rng)
        a = 1.0 if amps is None else amps[i]
        place(out, g, int(round(t * sr)), a, circular)
    return out
