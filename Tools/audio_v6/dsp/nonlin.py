"""Waveshapers, always run oversampled (never at 1x) : tanh, asymmetric, tape, wavefolder, soft clip."""
import numpy as np

from .util import upsample, downsample
from .filt import dc_block


def oversample(fn, x, factor=4):
    """Apply a memoryless nonlinearity at factor x the rate (Kaiser FIR up / down)."""
    x = np.asarray(x, dtype=np.float64)
    n = len(x)
    y = fn(upsample(x, factor))
    return downsample(y, factor)[:n]


def tanh(x, drive=1.0, factor=4, normalize=True):
    k = np.tanh(drive) if normalize else 1.0
    return oversample(lambda z: np.tanh(z * drive) / k, x, factor)


def asym(x, drive=1.0, bias=0.2, factor=4):
    """Asymmetric saturation (even harmonics), DC removed afterwards."""
    def f(z):
        return (np.tanh(z * drive + bias) - np.tanh(bias)) / np.tanh(drive)
    return dc_block(oversample(f, x, factor), 5.0)


def tape(x, drive=1.0, factor=4):
    """Soft tape-like curve : arctan core with a gentle cubic knee."""
    def f(z):
        z = z * drive
        return np.arctan(z + 0.15 * z ** 3 / (1.0 + z * z)) / np.arctan(drive * 1.15)
    return oversample(f, x, factor)


def fold(x, drive=2.0, factor=4):
    """Sine wavefolder."""
    return oversample(lambda z: np.sin(z * drive * np.pi * 0.5), x, factor)


def softclip(x, ceiling=1.0, factor=4):
    """Cubic soft clip u - 4/27 u^3 : unity slope at 0, reaches the ceiling with zero slope at 1.5 x the ceiling."""
    def f(z):
        u = np.clip(z / ceiling, -1.5, 1.5)
        return (u - (4.0 / 27.0) * u ** 3) * ceiling
    return oversample(f, x, factor)


def parallel_sat(x, drive=3.0, amount=0.3, factor=4):
    """Parallel saturation blend (keeps transients, adds density)."""
    return x + amount * tanh(x, drive, factor)
