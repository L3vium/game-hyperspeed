"""Instruments. Oscillators are PolyBLEP at 2x (band-limited), drive is oversampled, filters are TPT.
Every instrument renders one note / chord (mono unless stated) ; the sequencer places them."""
import numpy as np

from ..config import SR
from ..dsp import osc, env, filt, nonlin
from ..dsp.util import ns, mix
from .theory import hz


# ------------------------------------------------------------------ bass

def sub_line(events, n, glide=0.035, attack=0.004, release=0.02):
    """Phase-continuous sine sub over n samples. events : [(t0, dur, midi, vel)] sorted, non overlapping.
    Legato notes (gap < 20 ms) glide ; others retrigger with short ramps. Mono."""
    f = np.zeros(n)
    a = np.zeros(n)
    last_f = hz(events[0][2]) if events else 50.0
    prev_end = -1.0
    for t0, dur, m, vel in events:
        i0, i1 = ns(t0), min(n, ns(t0 + dur))
        if i1 <= i0:
            continue
        ft = float(hz(m))
        legato = (t0 - prev_end) < 0.02
        seg = np.full(i1 - i0, ft)
        if legato:
            g = min(i1 - i0, ns(glide))
            u = np.linspace(0, 1, g)
            seg[:g] = last_f * (ft / last_f) ** (1 - (1 - u) ** 2)
        f[i0:i1] = seg
        ea = np.ones(i1 - i0) * vel
        if not legato:
            k = min(len(ea), ns(attack))
            ea[:k] *= np.linspace(0, 1, k)
        k = min(len(ea), ns(release))
        ea[-k:] *= np.linspace(1, 0, k) ** 1.5
        a[i0:i1] = np.maximum(a[i0:i1], ea)
        last_f, prev_end = ft, t0 + dur
    # hold the pitch through gaps (no phase jumps), amplitude is 0 there
    idx = np.nonzero(f)[0]
    if len(idx):
        f = np.interp(np.arange(n), idx, f[idx])
    ph = np.cumsum(f) / SR
    y = np.sin(2 * np.pi * ph) * a
    return y + 0.08 * np.sin(4 * np.pi * ph) * a  # a touch of 2nd harmonic for small speakers


def reese(midi, dur, rng, cutoff=900.0, res=0.35, lfo_hz=0.6, depth=0.5, drive=2.0, voices=3, detune=11.0,
          env_amt=1.2, release=0.04, talk=0.0):
    """Reese : detuned PolyBLEP saws (+-detune cents) through a modulated TPT ladder, oversampled drive.
    talk : fast formant-ish wobble depth (jump-up / neuro talking bass)."""
    n = ns(dur + release)
    f = float(hz(midi))
    x = np.zeros(n)
    spread = np.linspace(-1, 1, voices)
    for s in spread:
        x += osc.saw(f * 2 ** (s * detune / 1200.0), n, phase0=rng.uniform())
    x /= voices
    t = np.arange(n) / SR
    lfo = np.sin(2 * np.pi * lfo_hz * t + rng.uniform(0, 2 * np.pi))
    fe = env.bp([(0, 0), (0.004, 1.0), (0.25, 0.3, -2.0), (dur, 0.15)], n)
    fc = cutoff * 2 ** (depth * lfo + env_amt * fe)
    if talk:
        fc = fc * 2 ** (talk * np.sin(2 * np.pi * 5.8 * t) ** 2)
    y = filt.ladder(x, np.clip(fc, 60, 16000), res)
    y = nonlin.tanh(y * drive, 1.0, 4)
    a = env.bp([(0, 0), (0.003, 1.0), (dur, 1.0), (dur + release, 0.0, -2.0)], n)
    return y * a


def neuro(midi, dur, rng, fold=2.2, formant_rate=4.0, cutoff=2500.0):
    """Neuro top : FM + wavefolder + comb + moving formants, OTT-squashed, high-passed (W2 / W3)."""
    from ..dsp import dyn
    n = ns(dur)
    f = float(hz(midi))
    t = np.arange(n) / SR
    idx = 2.0 + 1.5 * np.sin(2 * np.pi * formant_rate * t)
    y = osc.pm(f, n, [1.0, 2.0, 0.5], [idx, 1.2])
    y = nonlin.fold(y * 0.8, fold, 4)
    y = filt.comb_ff(y, 1.0 / (f * 4), 0.5)
    y = filt.formant_bank_tv(y, [(700 * 2 ** (0.6 * np.sin(2 * np.pi * formant_rate * t)), 6.0, 1.0),
                                 (1300 * 2 ** (0.5 * np.sin(2 * np.pi * formant_rate * t + 1)), 7.0, 0.8),
                                 (2600, 5.0, 0.4)])
    y = dyn.ott(y, 0.6)
    y = filt.hp(filt.lp(y, cutoff * 2), 150.0, 4)
    return y * env.bp([(0, 0), (0.004, 1), (dur - 0.03, 1), (dur, 0, -2)], n)


# ------------------------------------------------------------------ keys and pads

def epiano(notes, dur, vel, rng, release=0.12):
    """FM electric piano : tine pair (1:1, index bark decaying) + bell pair (1:14, very short), per note."""
    total = ns(dur + 2.5)
    out = np.zeros(total)
    for m in notes:
        f = float(hz(m))
        n = total
        t = np.arange(n) / SR
        decay = 2.8 * (110.0 / f) ** 0.35
        a = np.exp(-t / decay) * env.bp([(0, 0), (0.002, 1.0), (dur, 1.0), (dur + release, 0.0, -2)], n)
        idx = (0.9 + 1.4 * vel) * np.exp(-t / 0.45) + 0.25
        tine = osc.pm(f, n, [1.0, 1.0], [idx], detune_hz=[0.0, 0.3])
        bell = osc.pm(f, n, [1.0, 14.0], [1.5 * vel * np.exp(-t / 0.012)]) * np.exp(-t / 0.08) * 0.25 * vel
        out += (tine * 0.8 + bell) * a / len(notes) ** 0.6
    return nonlin.tape(out * 0.8, 1.2, 4)


def warm_pad(notes, dur, rng, cutoff=1600.0, attack=0.35, release=0.6):
    n = ns(dur + release)
    x = np.zeros(n)
    for m in notes:
        for d in (-7.0, 7.0):
            x += osc.saw(float(hz(m)) * 2 ** (d / 1200.0), n, phase0=rng.uniform())
    t = np.arange(n) / SR
    fc = cutoff * (1 + 0.25 * np.sin(2 * np.pi * 0.2 * t + rng.uniform(0, 6)))
    y = filt.svf(x, fc, 0.8, "lp") / (2 * len(notes))
    return y * env.bp([(0, 0), (attack, 1.0, 2.0), (dur, 0.85), (dur + release, 0.0, -3.0)], n)


def supersaw_chord(notes, dur, rng, cutoff=5000.0, attack=0.06, release=0.4, detune=0.35):
    n = ns(dur + release)
    x = np.zeros(n)
    for m in notes:
        x += osc.supersaw(float(hz(m)), n, rng, detune=detune)
    y = filt.svf(x, cutoff, 0.7, "lp") / len(notes)
    return y * env.bp([(0, 0), (attack, 1.0, 1.0), (dur, 0.8), (dur + release, 0.0, -3.0)], n)


def stab(notes, dur, rng, bright=7000.0):
    """Short chord stab : supersaw through a fast closing filter."""
    n = ns(dur + 0.25)
    x = np.zeros(n)
    for m in notes:
        x += osc.supersaw(float(hz(m)), n, rng, detune=0.3)
    fc = 700 + bright * np.exp(-np.arange(n) / (0.07 * SR))
    y = filt.svf(x, fc, 1.2, "lp") / len(notes)
    y = nonlin.tanh(y * 1.5, 1.0, 4)
    return y * env.bp([(0, 0), (0.002, 1.0), (dur, 0.6, -1.0), (dur + 0.25, 0.0, -3.0)], n)


def brass(notes, dur, rng, bright=3500.0):
    """Brass stab : saws with a pitch scoop and a 'blat' filter envelope (attack ~25 ms), a little FM growl."""
    n = ns(dur + 0.2)
    t = np.arange(n) / SR
    scoop = 2 ** (-0.6 / 12.0 * np.exp(-t / 0.03))
    x = np.zeros(n)
    for m in notes:
        f = float(hz(m))
        for d in (-6.0, 0.0, 6.0):
            x += osc.saw(f * scoop * 2 ** (d / 1200.0), n, phase0=rng.uniform())
        x += 0.3 * osc.pm(f * scoop, n, [1.0, 1.0], [1.5 * np.exp(-t / 0.08)])
    fc = 500 + bright * env.bp([(0, 0), (0.025, 1.0, 1.0), (0.2, 0.45), (dur, 0.3)], n)
    y = filt.svf(x / (3 * len(notes)), fc, 0.9, "lp")
    y = nonlin.tanh(y * 1.8, 1.0, 4)
    return y * env.bp([(0, 0), (0.01, 1.0), (dur, 0.7), (dur + 0.2, 0.0, -3.0)], n)


def choir(notes, dur, rng, vowel="a", attack=0.4, release=0.8):
    """Formant choir : vibrato saws per voice through a vowel formant bank, with breath."""
    forms = {"a": [(800, 80, 0.0), (1150, 90, -4.0), (2900, 120, -14.0), (3900, 130, -20.0)],
             "o": [(450, 70, 0.0), (800, 80, -6.0), (2830, 100, -18.0)],
             "u": [(325, 50, 0.0), (700, 60, -12.0), (2530, 170, -24.0)]}[vowel]
    n = ns(dur + release)
    t = np.arange(n) / SR
    x = np.zeros(n)
    for m in notes:
        for k in range(3):
            vib = 1 + 0.004 * np.sin(2 * np.pi * rng.uniform(4.6, 5.6) * t + rng.uniform(0, 6))
            x += osc.saw(float(hz(m)) * vib * 2 ** (rng.uniform(-8, 8) / 1200.0), n, phase0=rng.uniform())
    breath = filt.bp(rng.standard_normal(n), 1500, 6000) * 0.05
    y = filt.formant_bank(x / (3 * len(notes)) + breath, forms)
    return y * env.bp([(0, 0), (attack, 1.0, 2.0), (dur, 0.9), (dur + release, 0.0, -3.0)], n)


# ------------------------------------------------------------------ plucks, bells, lead

def pluck(midi, dur, rng, bright=0.6, decay=0.9):
    n = ns(dur + 0.3)
    y = osc.karplus_strong(float(hz(midi)), n, rng, decay=decay, bright=bright)
    return filt.hp(y, 150.0) * env.bp([(0, 1.0), (dur, 1.0), (dur + 0.3, 0.0, -3.0)], n)


def fm_bell(midi, dur, rng, ratio=3.5, index=2.0, decay=1.5):
    n = ns(dur + decay)
    t = np.arange(n) / SR
    f = float(hz(midi))
    idx = index * np.exp(-t / (0.3 * decay))
    y = osc.pm(f, n, [1.0, ratio], [idx]) * 0.6 + osc.pm(f * 2.0, n, [1.0, 1.41], [idx * 0.5]) * 0.25
    return y * np.exp(-t / (decay / 4.0)) * env.bp([(0, 0), (0.0015, 1.0)], n)


def lead(midi, dur, rng, prev_midi=None, glide=0.035, cutoff=4200.0, vib_depth=14.0):
    """Lead synth : saw + PWM pulse, legato glide from the previous note, delayed vibrato, filter envelope."""
    n = ns(dur + 0.12)
    t = np.arange(n) / SR
    f1 = float(hz(midi))
    f = np.full(n, f1)
    if prev_midi is not None:
        f0 = float(hz(prev_midi))
        g = min(n, ns(glide))
        u = np.linspace(0, 1, g)
        f[:g] = f0 * (f1 / f0) ** (1 - (1 - u) ** 2)
    vib = 2 ** (vib_depth / 1200.0 * np.sin(2 * np.pi * 5.6 * t) * np.clip((t - 0.12) / 0.15, 0, 1))
    f = f * vib
    pw = 0.5 + 0.18 * np.sin(2 * np.pi * 0.7 * t + rng.uniform(0, 6))
    x = osc.saw(f, n, phase0=rng.uniform()) * 0.6 + osc.pulse(f * 1.002, n, pw, phase0=rng.uniform()) * 0.5
    fc = cutoff * (0.6 + 0.8 * np.exp(-t / 0.12))
    y = filt.svf(x, fc, 1.0, "lp")
    y = nonlin.tanh(y * 1.3, 1.0, 4)
    return y * env.bp([(0, 0), (0.006, 1.0), (dur, 0.85), (dur + 0.12, 0.0, -3.0)], n)


# ------------------------------------------------------------------ textures

def swell(dur, rng, lo=400.0, hi=12000.0):
    """Reverse-cymbal style noise swell ending at dur (place it so it ends on the downbeat)."""
    n = ns(dur)
    t = np.arange(n) / SR
    x = rng.standard_normal(n)
    fc = lo * (hi / lo) ** (t / dur) ** 2
    y = filt.svf(x, fc, 1.5, "bpn") + filt.hp(x, 6000) * 0.3
    y = y * (t / dur) ** 2.5
    k = ns(0.012)
    y[-k:] *= np.linspace(1, 0, k)
    return y


def riser(dur, rng, f0=200.0, f1=4000.0):
    """Build riser : noise band and a tone gliding up together."""
    n = ns(dur)
    t = np.arange(n) / SR
    u = t / dur
    fc = f0 * (f1 / f0) ** u
    nz = filt.svf(rng.standard_normal(n), fc * 2, 3.0, "bpn")
    tone = osc.saw(fc, n) * 0.25
    tone = filt.svf(tone, fc * 3, 0.8, "lp")
    return mix(nz, tone) * u ** 1.5


def atmos(n, rng, notes, cutoff=1500.0):
    """Slow textural bed : filtered pink noise breathing + a very soft drone on the given notes (periodic)."""
    nz = osc.colored(n, rng, -3.0, lo=200, hi=min(8000, cutoff * 2))
    breath = 0.5 + 0.5 * osc.periodic_lfo(n, 16)
    dr = np.zeros(n)
    for m in notes:
        f = float(hz(m))
        f = max(1.0, np.round(f * n / SR)) * SR / n
        dr += np.sin(2 * np.pi * f * np.arange(n) / SR + rng.uniform(0, 6))
    return nz * breath * 0.25 + dr / max(1, len(notes)) * 0.15
