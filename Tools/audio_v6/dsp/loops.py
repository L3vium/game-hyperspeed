"""Seamless loops : circular construction (events on a circular buffer), steady-state processing on [x, x, x]
keeping the middle, seam metrics."""
import numpy as np

from ..config import SR


def steady(fn, x, copies=3):
    """Process a loop in steady state : fn([x]*copies) and keep the middle copy, so filter / reverb state and
    tails cross the seam exactly as they would while looping."""
    x = np.asarray(x, dtype=np.float64)
    n = len(x)
    y = fn(np.concatenate([x] * copies, axis=0))
    mid = copies // 2
    return y[mid * n:(mid + 1) * n]


def steady_ctx(fn, x, ctx_s=2.0, sr=SR):
    """Steady state for processes with a short memory (filters, compressors, limiters, delays, reverbs up to
    ctx_s long) : process [end of loop, loop, start of loop] and keep the loop. Much cheaper than 3 full copies."""
    x = np.asarray(x, dtype=np.float64)
    n = len(x)
    c = min(n, int(ctx_s * sr))
    y = fn(np.concatenate([x[n - c:], x, x[:c]], axis=0))
    return y[c:c + n]


def wrap_tail(x, n):
    """Fold everything past n samples back onto the start (a loop's overflowing tails)."""
    x = np.asarray(x, dtype=np.float64)
    out = x[:n].copy()
    k = n
    while k < len(x):
        m = min(n, len(x) - k)
        out[:m] += x[k:k + m]
        k += m
    return out


def seam_metrics(x, sr=SR):
    """Seam quality of a loop : jump at the seam vs the p99 of |diff| inside, RMS dip over +-50 ms (dB),
    spectral flux at the seam relative to the median flux."""
    x = np.asarray(x, dtype=np.float64)
    if x.ndim == 1:
        x = x[:, None]
    d = np.abs(np.diff(x, axis=0))
    p99 = float(np.percentile(d.max(axis=1), 99)) + 1e-12
    jump = float(np.max(np.abs(x[0] - x[-1])))
    # a transient that starts right on the seam has steep slopes around it too : the jump is only a click when it
    # stands out of BOTH the file's p99 slope and the slopes within +-10 ms of the seam
    k = int(0.01 * sr)
    local = float(max(d[:k].max(), d[-k:].max())) if len(d) > 2 * k else 0.0
    # a click stands out of its neighbourhood : the seam slope must exceed 1.5 x the steepest slope within 10 ms
    p99 = max(p99, 1.5 * local)
    # RMS dip : a notch AT the seam (+-5 ms) relative to its surroundings (15..50 ms on both sides) - what a bad
    # crossfade or a gap would leave ; a section change at the seam (sparse -> dense) is not a notch
    w = int(0.05 * sr)
    c, h = int(0.005 * sr), int(0.015 * sr)
    seg = 2 * c

    def notch(joined):
        """10 ms RMS at the centre vs the quieter side (median 10 ms RMS 15..50 ms before / after). A notch is lower
        than BOTH sides ; a section change (quiet -> loud) is not. Robust to a transient next to it."""
        r_c = np.sqrt(np.mean(joined[w - c:w + c] ** 2)) + 1e-12

        def side(starts):
            v = [np.sqrt(np.mean(joined[i:i + seg] ** 2)) for i in starts]
            return float(np.median(v)) if v else r_c
        r_a = min(side(range(0, w - h - seg + 1, c)), side(range(w + h, 2 * w - seg + 1, c))) + 1e-12
        return float(min(0.0, 20 * np.log10(r_c / r_a))) if r_a > 1e-5 else 0.0
    # calibrated against the loop itself : noisy loops dip by chance, so the seam is compared with the 5th percentile
    # of the same notch measured at 64 positions inside the loop (a seam artefact sits below what the content does)
    n = len(x)
    inner = [notch(x[p - w:p + w]) for p in np.linspace(w, n - w, 66)[1:-1].astype(int)] if n > 4 * w else [0.0]
    dip = float(min(0.0, notch(np.concatenate([x[-w:], x[:w]], axis=0)) - min(0.0, np.percentile(inner, 5))))
    nfft = 1024
    m = x.mean(axis=1)
    win = np.hanning(nfft)

    def spec(seg_):
        return np.abs(np.fft.rfft(seg_ * win))

    def flux(a, b):
        return float(np.sqrt(np.mean((np.log10(spec(a) + 1e-9) - np.log10(spec(b) + 1e-9)) ** 2)))

    seam_flux = flux(m[-nfft:], m[:nfft])
    fl = []
    for k in range(8):
        c = int(len(m) * (k + 1) / 10)
        if c - nfft >= 0 and c + nfft <= len(m):
            fl.append(flux(m[c - nfft:c], m[c:c + nfft]))
    med = float(np.median(fl)) if fl else seam_flux
    return dict(seam_jump=jump, seam_p99=p99, seam_ratio=jump / p99, seam_dip_db=dip, seam_flux_ratio=seam_flux / (med + 1e-9))
