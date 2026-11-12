"""Spaces : synthesised stereo impulse responses (independent L/R noise with ~0.2 inter-channel correlation,
per-bin RT60(f) decay applied in the STFT, sparse early reflections, pre-delay), presets, reverb SENDS with a
250-300 Hz high-pass on the return. Decorrelation only above ~250 Hz ; the low end stays mono ; no Haas."""
import functools

import numpy as np
from scipy import signal

from ..config import SR
from . import filt, stft
from .util import to_stereo, to_mono, fit

# rt60 at (125 Hz, 1 kHz, 8 kHz) seconds ; predelay ms ; early reflections (count, span ms) ; length s ; density
PRESETS = {
    "tight": dict(rt=(0.30, 0.28, 0.16), pre=4.0, er=(6, 18.0), length=0.6, corr=0.2, hp=300.0),
    "room": dict(rt=(0.65, 0.6, 0.35), pre=8.0, er=(10, 35.0), length=1.2, corr=0.2, hp=280.0),
    "plate": dict(rt=(1.1, 1.3, 1.0), pre=12.0, er=(0, 0.0), length=2.4, corr=0.15, hp=280.0),
    "hall": dict(rt=(3.0, 2.8, 1.6), pre=24.0, er=(14, 80.0), length=5.0, corr=0.2, hp=260.0),
    "canyon": dict(rt=(2.2, 2.0, 0.9), pre=40.0, er=(0, 0.0), length=4.0, corr=0.2, hp=250.0,
                   slaps=((0.15, 0.55, -0.6), (0.32, 0.4, 0.7), (0.61, 0.3, -0.4), (0.95, 0.2, 0.5), (1.5, 0.13, -0.2))),
    "cosmic": dict(rt=(5.5, 5.0, 3.5), pre=30.0, er=(0, 0.0), length=9.0, corr=0.15, hp=250.0, shimmer=0.35),
}


def _rt_of_f(f, rt):
    """Piecewise log-frequency interpolation of RT60 through (125 Hz, 1 kHz, 8 kHz)."""
    lf = np.log2(np.maximum(f, 20.0))
    return np.interp(lf, [np.log2(125.0), np.log2(1000.0), np.log2(8000.0)], rt)


@functools.lru_cache(maxsize=16)
def make_ir(preset, sr=SR, seed=7):
    p = PRESETS[preset]
    rng = np.random.default_rng(seed + sum(map(ord, preset)))
    n = int(p["length"] * sr)
    common = rng.standard_normal(n)
    a = np.sqrt(p["corr"])
    b = np.sqrt(1.0 - p["corr"])
    out = []
    for ch in range(2):
        nz = a * common + b * rng.standard_normal(n)
        nfft, hop = 1024, 256
        X = stft.stft(nz, nfft, hop)
        t, f = stft.frame_axes(n, nfft, hop, sr)
        rt = _rt_of_f(f, p["rt"])[None, :]
        tt = np.maximum(t[:len(X), None], 0.0)
        G = 10 ** (-3.0 * tt / rt)
        # gentle air roll-off of the diffuse tail
        G = G * stft.tilt(f, -0.8, 2000.0)[None, :]
        y = stft.istft(X * G, n, nfft, hop)
        # diffuse onset ramp (build-up of density)
        ramp = int(0.012 * sr)
        y[:ramp] *= np.linspace(0, 1, ramp) ** 2
        ir = np.zeros(n)
        pre = int(p["pre"] * sr / 1000.0)
        ir[pre:] += y[:n - pre]
        cnt, span = p["er"]
        if cnt:
            times = np.sort(rng.uniform(1.0, span, cnt)) * sr / 1000.0
            for k, tm in enumerate(times):
                i = int(tm)
                ir[i] += rng.choice([-1, 1]) * 0.9 * (1.0 - k / (cnt + 2)) * np.sqrt(np.mean(y[:int(0.05 * sr)] ** 2)) * 12
        for (ts, g, pan) in p.get("slaps", ()):
            i = int(ts * sr)
            if i < n:
                gch = g * (1.0 + (pan if ch == 1 else -pan) * 0.6)
                burst = rng.standard_normal(int(0.02 * sr)) * np.hanning(int(0.02 * sr))
                burst = filt.lp(burst, 4000.0 / (1.0 + ts * 2.5))
                ir[i:i + len(burst)] += burst[:max(0, n - i)] * gch * 0.25
        out.append(ir)
    ir = np.stack(out, axis=1)
    ir /= np.sqrt(np.sum(ir ** 2) / 2.0) + 1e-12
    return ir


def convolve(x, ir):
    """Mono or stereo input through a stereo IR (L->L, R->R) ; output length len(x) + len(ir) - 1."""
    x = to_stereo(x)
    yl = signal.oaconvolve(x[:, 0], ir[:, 0])
    yr = signal.oaconvolve(x[:, 1], ir[:, 1])
    return np.stack([yl, yr], axis=1)


def send(x, preset="room", wet_db=-12.0, hp_hz=None, lp_hz=None, sr=SR, tail=True, pre_hp=None):
    """Reverb SEND : returns the wet return only (stereo), high-passed (250-300 Hz) so the low end stays dry/mono.
    The input is summed to mono for the send. Length = len(x) + IR tail when tail=True."""
    p = PRESETS[preset]
    ir = make_ir(preset, sr)
    src = to_mono(x)
    if pre_hp:
        src = filt.hp(src, pre_hp)
    wet = convolve(src, ir)
    if p.get("shimmer"):
        # shimmer : the send pitched up an octave (2x resample) and fed into the same tail, a slow bloom
        up = signal.resample_poly(src, 1, 2)
        up = filt.hp(up, 800.0)
        sh = convolve(up, ir)
        sh[:, 0], sh[:, 1] = sh[:, 1].copy(), sh[:, 0].copy()
        wet = wet + fit(sh, len(wet)) * p["shimmer"]
    hpf = hp_hz or p["hp"]
    wet = filt.hp(filt.hp(wet, hpf, 4), hpf, 4)
    if lp_hz:
        wet = filt.lp(wet, lp_hz)
    wet *= 10 ** (wet_db / 20.0)
    if not tail:
        wet = wet[:len(x)]
    return wet


def with_send(dry, preset="room", wet_db=-12.0, **kw):
    """dry (mono or stereo) + its reverb return, length extended by the tail."""
    wet = send(dry, preset, wet_db, **kw)
    d = to_stereo(dry)
    out = wet.copy()
    out[:len(d)] += d
    return out
