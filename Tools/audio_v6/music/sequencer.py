"""Sequencing : the 16th grid with swing and humanise, circular tracks (events near the end wrap their tails onto the
start, so a 64-bar stem loops exactly), the section map of a 64-bar world loop."""
import numpy as np

from ..config import SR, BAR, STEP16
from ..dsp.util import place, ns

BARS = 64
SECTIONS = [("dropA", 0, 16), ("switch", 16, 24), ("breakdown", 24, 32), ("build", 32, 40), ("dropB", 40, 56),
            ("roller", 56, 64)]


def section(bar):
    b = bar % BARS
    for name, a, z in SECTIONS:
        if a <= b < z:
            return name, b - a, z - a
    return "dropA", 0, 16


def loop_len(bars=BARS):
    return ns(bars * BAR)


class Grid:
    """16th-note grid with swing (fraction of the 8th given to the first 16th, 0.5 = straight) and humanise."""

    def __init__(self, swing=0.555, human_ms=3.0, rng=None):
        self.swing = swing
        self.human = human_ms / 1000.0
        self.rng = rng

    def t(self, bar, step, humanise=True):
        base = bar * BAR + step * STEP16
        if int(np.floor(step)) % 2 == 1 and abs(step - round(step)) < 1e-6:
            base += (self.swing - 0.5) * 2.0 * STEP16
        if humanise and self.rng is not None and self.human > 0:
            base += float(np.clip(self.rng.normal(0, self.human * 0.6), -self.human, self.human))
        return base


class Track:
    """A circular buffer (mono or stereo) of a whole loop."""

    def __init__(self, n, stereo=False):
        self.n = n
        self.buf = np.zeros((n, 2)) if stereo else np.zeros(n)

    def add(self, x, t, gain=1.0):
        x = np.asarray(x, dtype=np.float64)
        if self.buf.ndim == 2 and x.ndim == 1:
            x = np.stack([x, x], axis=1)
        place(self.buf, x, int(round(t * SR)), gain, circular=True)
