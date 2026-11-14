"""STFT with sqrt-Hann WOLA (linear or circular framing) and spectral noise shaping with a gain map G(t, f) :
moving resonances, tilts, distance absorption -> whooshes, wind, fire, rumble."""
import numpy as np

from ..config import SR


def _win(nfft):
    return np.sqrt(0.5 - 0.5 * np.cos(2 * np.pi * np.arange(nfft) / nfft))


def stft(x, nfft=2048, hop=512, circular=False):
    """Frames (n_frames, nfft//2+1). Linear framing pads nfft at both ends ; circular wraps (seamless loops)."""
    x = np.asarray(x, dtype=np.float64)
    w = _win(nfft)
    n = len(x)
    if circular:
        nfr = int(np.ceil(n / hop))
        idx = (np.arange(nfr)[:, None] * hop + np.arange(nfft)[None, :] - nfft // 2) % n
        frames = x[idx]
    else:
        xp = np.pad(x, (nfft, nfft))
        nfr = (len(xp) - nfft) // hop + 1
        idx = np.arange(nfr)[:, None] * hop + np.arange(nfft)[None, :]
        frames = xp[idx]
    return np.fft.rfft(frames * w, axis=1)


def istft(X, n, nfft=2048, hop=512, circular=False):
    w = _win(nfft)
    frames = np.fft.irfft(X, nfft, axis=1) * w
    nfr = len(frames)
    norm = np.sum(w * w) / hop
    if circular:
        out = np.zeros(n)
        idx = (np.arange(nfr)[:, None] * hop + np.arange(nfft)[None, :] - nfft // 2) % n
        np.add.at(out, idx.ravel(), frames.ravel())
        return out / norm
    out = np.zeros(nfr * hop + nfft)
    for i in range(nfr):
        out[i * hop:i * hop + nfft] += frames[i]
    return out[nfft:nfft + n] / norm


def frame_axes(n, nfft=2048, hop=512, sr=SR, circular=False):
    """Centre time (s) of every frame and the bin frequencies (Hz)."""
    if circular:
        nfr = int(np.ceil(n / hop))
        t = np.arange(nfr) * hop / sr
    else:
        nfr = (n + 2 * nfft - nfft) // hop + 1
        t = (np.arange(nfr) * hop + nfft // 2 - nfft) / sr
    f = np.fft.rfftfreq(nfft, 1.0 / sr)
    return t, f


def shape(x, gain_fn, nfft=2048, hop=512, sr=SR, circular=False):
    """Filter x by a time-frequency gain map : gain_fn(t (frames,1), f (1,bins)) -> linear gains."""
    X = stft(x, nfft, hop, circular)
    t, f = frame_axes(len(x), nfft, hop, sr, circular)
    G = np.broadcast_to(gain_fn(t[:len(X), None], f[None, :]), X.shape)
    return istft(X * G, len(x), nfft, hop, circular)


def shaped_noise(n, gain_fn, rng, nfft=2048, hop=512, sr=SR, circular=False):
    """White noise shaped by a gain map (whooshes, wind, fire, rumble). circular=True for seamless loops."""
    return shape(rng.standard_normal(n), gain_fn, nfft, hop, sr, circular)


def resonance(f, fc, q, gain_db):
    """A resonant peak (log-frequency Gaussian) usable inside gain maps. fc may be (frames,1)."""
    width = 1.0 / max(q, 0.1)
    return 10 ** (gain_db / 20.0 * np.exp(-0.5 * (np.log2(np.maximum(f, 1.0) / fc) / width) ** 2))


def band(f, lo, hi, slope_lo=12.0, slope_hi=12.0):
    """Smooth band-pass magnitude (dB/oct slopes) usable inside gain maps ; lo/hi may be (frames,1)."""
    fl = np.maximum(f, 1.0)
    g_lo = np.where(fl < lo, 10 ** (-slope_lo * np.log2(lo / fl) / 20.0), 1.0)
    g_hi = np.where(fl > hi, 10 ** (-slope_hi * np.log2(fl / hi) / 20.0), 1.0)
    return g_lo * g_hi


def tilt(f, db_per_oct, pivot=1000.0):
    return 10 ** (db_per_oct * np.log2(np.maximum(f, 1.0) / pivot) / 20.0)
