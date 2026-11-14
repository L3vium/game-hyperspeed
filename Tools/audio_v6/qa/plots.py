"""PNG per file (L/R waveform, log spectrogram fixed -100..0 dB, momentary loudness vs target band, L/R
correlation) and contact sheets per family."""
import os

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402

from ..config import SR, CATEGORIES  # noqa: E402
from ..dsp.util import to_stereo  # noqa: E402

ROWS = 220


def log_spectrogram(x, sr=SR, max_frames=900):
    """(rows, frames) dB image on a log-frequency axis 20 Hz .. 24 kHz, 0 dB = full-scale sine."""
    m = to_stereo(x).mean(axis=1)
    n = len(m)
    nfft = 2048 if n > SR * 1.5 else (512 if n > 2048 else 256)
    hop = max(nfft // 4, int(np.ceil((n - nfft) / max_frames)) if n > nfft else nfft // 4)
    if n < nfft:
        m = np.pad(m, (0, nfft - n))
        n = nfft
    nfr = 1 + (n - nfft) // hop
    w = np.hanning(nfft)
    idx = np.arange(nfr)[:, None] * hop + np.arange(nfft)[None, :]
    S = np.abs(np.fft.rfft(m[idx] * w, axis=1)) / (w.sum() / 2)
    f = np.fft.rfftfreq(nfft, 1.0 / sr)
    fl = np.geomspace(20, sr / 2, ROWS)
    img = np.empty((ROWS, nfr))
    for i in range(nfr):
        img[:, i] = np.interp(fl, f, S[i])
    return 20 * np.log10(img + 1e-10), fl, (np.arange(nfr) * hop + nfft / 2) / sr


def _corr_series(st, sr=SR, win=0.05):
    h = int(win * sr)
    k = len(st) // h
    if k < 1:
        return np.array([0.0]), np.array([1.0])
    a = st[:k * h].reshape(k, h, 2)
    num = (a[:, :, 0] * a[:, :, 1]).sum(axis=1)
    den = np.sqrt((a[:, :, 0] ** 2).sum(axis=1) * (a[:, :, 1] ** 2).sum(axis=1)) + 1e-12
    c = np.where(den > 1e-8, num / den, 1.0)
    return (np.arange(k) + 0.5) * win, c


def file_png(path, name, x, metrics, category, sr=SR):
    st = to_stereo(x)
    n = len(st)
    t = np.arange(n) / sr
    fig, ax = plt.subplots(4, 1, figsize=(9, 7.2), dpi=72, sharex=True,
                           gridspec_kw=dict(height_ratios=[1.0, 2.2, 1.0, 0.7]))
    dec = max(1, n // 6000)
    ax[0].plot(t[::dec], st[::dec, 0], lw=0.5, color="#2a7fd4", label="L")
    ax[0].plot(t[::dec], -st[::dec, 1] if False else st[::dec, 1], lw=0.5, color="#d46a2a", alpha=0.6, label="R")
    ax[0].set_ylim(-1, 1)
    ax[0].set_ylabel("wave")
    ax[0].legend(loc="upper right", fontsize=7)
    img, fl, ft = log_spectrogram(st, sr)
    ax[1].imshow(img, origin="lower", aspect="auto", vmin=-100, vmax=0, cmap="magma",
                 extent=[0, n / sr, 0, ROWS])
    ticks = [30, 100, 300, 1000, 3000, 10000]
    ax[1].set_yticks([np.interp(np.log(f), np.log(fl), np.arange(ROWS)) for f in ticks])
    ax[1].set_yticklabels(["30", "100", "300", "1k", "3k", "10k"])
    ax[1].set_ylabel("Hz")
    mom = np.array(metrics.get("momentary", []))
    if len(mom):
        tm = 0.2 + np.arange(len(mom)) * 0.05
        ax[2].plot(np.minimum(tm, n / sr), mom, color="#222", lw=1, marker="o" if len(mom) < 4 else None, ms=3)
    c = CATEGORIES[category]
    if c.get("target") is not None:
        lo, hi = c["target"] - c["tol"], c["target"] + c["tol"]
        ax[2].axhspan(lo, hi, color="#4caf50", alpha=0.25)
    ax[2].set_ylim(-60, -4)
    ax[2].set_ylabel("LUFS-M")
    tc, cc = _corr_series(st, sr)
    ax[3].plot(tc, cc, color="#7b3fb5", lw=0.8)
    ax[3].axhline(0, color="#999", lw=0.5)
    ax[3].set_ylim(-1.05, 1.05)
    ax[3].set_ylabel("L/R corr")
    ax[3].set_xlabel("s")
    fig.suptitle("%s   [%s]   I %.1f  M %.1f  TP %.1f  crest %.1f" % (
        name, category, metrics["lufs_i"], metrics["lufs_m_max"], metrics["true_peak"], metrics["crest"]), fontsize=9)
    fig.tight_layout(rect=[0, 0, 1, 0.97])
    os.makedirs(os.path.dirname(path), exist_ok=True)
    fig.savefig(path)
    plt.close(fig)


def contact_sheet(path, title, items, sr=SR):
    """items : [(name, audio)]. One small spectrogram per take, same scale."""
    k = len(items)
    cols = min(4, k)
    rows = int(np.ceil(k / cols))
    fig, axes = plt.subplots(rows, cols, figsize=(3.2 * cols, 2.2 * rows), dpi=72, squeeze=False)
    for i, (nm, x) in enumerate(items):
        a = axes[i // cols][i % cols]
        img, fl, _ = log_spectrogram(x, sr, 300)
        a.imshow(img, origin="lower", aspect="auto", vmin=-100, vmax=0, cmap="magma", extent=[0, len(x) / sr, 0, ROWS])
        a.set_title(nm, fontsize=7)
        a.set_yticks([])
        a.tick_params(labelsize=6)
    for j in range(k, rows * cols):
        axes[j // cols][j % cols].axis("off")
    fig.suptitle(title, fontsize=9)
    fig.tight_layout()
    os.makedirs(os.path.dirname(path), exist_ok=True)
    fig.savefig(path)
    plt.close(fig)
