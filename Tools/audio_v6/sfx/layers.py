"""Designed SFX layers. Transient (0-15 ms) / body (15-150 ms, identity) / tail (designed) / sub 30-80 Hz mono
<= 400 ms on hero events only. Stereo is built mid/side : the mono core stays the mid, the side is a decorrelated copy
above 250 Hz, so the mono sum is exactly the design and the low end is mono. No Haas."""
import numpy as np
from scipy import signal

from ..config import SR, WORLDS
from ..dsp import osc, env, filt, stft, nonlin, modal, granular, space
from ..dsp.util import ns, fit, mix, to_stereo, place, ms_decode, undb
from ..dsp.loops import steady


# ------------------------------------------------------------------ pitch helpers

def hz(midi):
    return 440.0 * 2 ** ((np.asarray(midi, dtype=np.float64) - 69.0) / 12.0)


def world_note(world, degree, octave=4):
    """MIDI note of a scale degree (0-based, may exceed 7) of the world key, around octave `octave`."""
    w = WORLDS[world or 1]
    sc = w["scale"]
    root = w["root"] % 12 + 12 * (octave + 1)
    return root + sc[degree % 7] + 12 * (degree // 7)


def world_chord(world, octave=4, kind="tonic9"):
    """In-key chord tones for tonal sweeteners."""
    w = WORLDS[world or 1]
    r = w["root"] % 12 + 12 * (octave + 1)
    if kind == "tonic9":
        return [r, r + 3, r + 7, r + 10, r + 14]
    if kind == "rel_major":  # the relative major (bright resolution)
        return [r + 3, r + 7, r + 10, r + 14, r + 19]
    if kind == "five":
        return [r, r + 7, r + 12, r + 19]
    return [r, r + 3, r + 7]


# ------------------------------------------------------------------ transients

def snap(rng, ms=3.0, lo=1800.0, hi=11000.0, click=0.6):
    """2-5 ms snap : a band-passed noise burst with a ~ms/4 decay and a half-sine click."""
    n = ns(ms / 1000.0 * 3)
    burst = rng.standard_normal(n) * env.decay(n, ms / 1000.0 / 4.0, attack=0.0001)
    burst = filt.bp(burst, lo, hi, 2)
    c = np.zeros(n)
    p = modal.contact(0.12)
    c[:len(p)] = p / p.max()
    c = filt.hp(c, lo * 0.7)
    y = burst / (np.max(np.abs(burst)) + 1e-12) + click * c / (np.max(np.abs(c)) + 1e-12)
    return y / (np.max(np.abs(y)) + 1e-12)


def nwave(ms=10.0, rise_ms=0.08, amp=1.0):
    """Sonic-boom N-wave : a shock up, a linear fall through zero, a shock back : two cracks `ms` apart.
    Built at 8x then decimated (band-limited shocks)."""
    os_ = 8
    sr8 = SR * os_
    n = int(sr8 * (ms + 4 * rise_ms + 2) / 1000.0)
    t = np.arange(n) / sr8 * 1000.0
    r = rise_ms
    y = np.zeros(n)
    m1 = (t >= 1.0) & (t < 1.0 + r)
    y[m1] = (t[m1] - 1.0) / r
    m2 = (t >= 1.0 + r) & (t < 1.0 + r + ms)
    y[m2] = 1.0 - 2.0 * (t[m2] - 1.0 - r) / ms
    m3 = (t >= 1.0 + r + ms) & (t < 1.0 + 2 * r + ms)
    y[m3] = -1.0 + (t[m3] - 1.0 - r - ms) / r
    y = signal.resample_poly(y, 1, os_)
    return y * amp


def thump(f0=110.0, rng=None, dur=0.09, drop=1.7, tau_p=0.012, hp=60.0, drive=1.2):
    """Short body weight : sine dropping from f0 * drop to f0, decaying over dur. High-passed (non-hero)."""
    n = ns(dur * 3.0)
    t = np.arange(n) / SR
    f = f0 + f0 * (drop - 1.0) * np.exp(-t / tau_p)
    y = np.sin(2 * np.pi * np.cumsum(f) / SR) * env.decay(n, dur / 3.0, attack=0.0008)
    if drive > 1.0:
        y = nonlin.tanh(y, drive, 4)
    return filt.hp(y, hp, 2) if hp else y


def sub_drop(f0=70.0, f1=35.0, dur=0.4, curve=4.0, attack=0.002):
    """Hero sub : mono sine gliding f0 -> f1 (exponential), <= 400 ms."""
    n = ns(dur)
    u = np.arange(n) / n
    f = f1 + (f0 - f1) * np.exp(-curve * u)
    a = env.bp([(0, 0), (attack, 1.0), (dur * 0.35, 0.7, -1.0), (dur, 0.0, -2.5)], n)
    y = np.sin(2 * np.pi * np.cumsum(f) / SR) * a
    return filt.hp(y, 25.0, 2)


# ------------------------------------------------------------------ noise bodies

def air(dur, rng, f0, f1, q=1.5, peak_db=14.0, lo=150.0, hi=14000.0, env_pts=None, curve=0.0, tilt_db=-2.0,
        nfft=512, floor=0.25):
    """Whoosh / air body : noise with a moving resonant band (f0 -> f1, log glide with curvature) shaped in the
    STFT, then an amplitude envelope. env_pts : breakpoints [(t, v[, curve])]."""
    n = ns(dur)

    def g(t, f):
        u = np.clip(t / dur, 0.0, 1.0)
        if curve:
            u = (np.exp(curve * u) - 1) / (np.exp(curve) - 1)
        fc = f0 * (f1 / f0) ** u
        return (floor + stft.resonance(f, fc, q, peak_db) - 1.0 + 1e-3) * stft.band(f, lo, hi, 12, 18) * stft.tilt(f, tilt_db, 1000.0)
    y = stft.shaped_noise(n, g, rng, nfft=nfft, hop=nfft // 4)
    if env_pts is not None:
        y = y * env.bp(env_pts, n)
    return y / (np.max(np.abs(y)) + 1e-12)


def rustle(dur, rng, rate=400.0, lo=1500.0, hi=7000.0, grain_ms=(0.6, 3.0), env_pts=None):
    """Granular friction / rustle : Poisson micro-grains of band-passed noise."""
    n = ns(dur)
    times = granular.poisson(dur, rate, rng)

    def grain(i, r):
        m = ns(r.uniform(*grain_ms) / 1000.0) + 2
        g = r.standard_normal(m) * np.hanning(m)
        return g * r.uniform(0.3, 1.0)
    y = granular.render(n, times, grain, rng)
    y = filt.bp(y, lo, hi, 2)
    if env_pts is not None:
        y = y * env.bp(env_pts, n)
    return y / (np.max(np.abs(y)) + 1e-12)


def crackle(dur, rng, cluster_rate=18.0, size=6.0, spread=0.02, lo=2500.0, hi=12000.0, circular=False):
    """Electric arc crackle : clustered bursts of sub-millisecond impulses."""
    n = ns(dur)
    times = granular.clustered(dur, cluster_rate, size, spread, rng)

    def grain(i, r):
        m = ns(r.uniform(0.05, 0.6) / 1000.0) + 2
        g = r.standard_normal(m) * np.exp(-np.arange(m) / (m / 3.0))
        return g * r.uniform(0.2, 1.0) ** 2
    y = granular.render(n, times, grain, rng, circular=circular)
    y = steady(lambda z: filt.bp(z, lo, hi, 2), y) if circular else filt.bp(y, lo, hi, 2)
    return y / (np.max(np.abs(y)) + 1e-12)


def debris(dur, rng, count=20, f_lo=900.0, f_hi=4000.0, kind="plate", decay=0.04, t0=0.0, alpha=1.8, contact_ms=0.2):
    """Scattered small modal hits (gravel, shards) with a power-law time distribution."""
    n = ns(dur)
    times = granular.power_law(dur - t0, count, rng, alpha, t0)
    out = np.zeros(n)
    for t in times:
        m = ns(decay * 3)
        h = modal.hit(np.exp(rng.uniform(np.log(f_lo), np.log(f_hi))), kind, m, rng, decay=decay * rng.uniform(0.6, 1.4),
                      damp=0.6, contact_ms=contact_ms)
        place(out, h, ns(t), rng.uniform(0.2, 1.0) * (1.0 - 0.5 * t / dur))
    return out / (np.max(np.abs(out)) + 1e-12)


# ------------------------------------------------------------------ tonal

def bell(f, dur, rng, ratio=3.5, index=2.5, decay=1.2, bright_decay=0.25, detune=0.6):
    """FM bell : carrier f, modulator f * ratio, index decaying faster than the amplitude ; two slightly detuned
    voices for a slow beat."""
    n = ns(dur)
    idx = index * env.decay(n, bright_decay * decay)
    a = env.decay(n, decay / 6.9, attack=0.0015)
    y = osc.pm(f, n, [1.0, ratio], [idx]) + osc.pm(f + detune, n, [1.0, ratio], [idx * 0.8], phases=[0.3, 0.1])
    return y * a * 0.5


def glints(dur, rng, notes, count=10, decay=0.35, t_spread=None, octave_up=0):
    """Shimmer of in-key partial pings (NOT random frequencies) : chord tones scattered in time."""
    n = ns(dur)
    out = np.zeros(n)
    t_spread = t_spread or dur * 0.6
    for k in range(count):
        m = notes[rng.integers(0, len(notes))] + 12 * octave_up
        t = rng.uniform(0, t_spread) ** 1.5 / max(t_spread, 1e-6) ** 0.5
        p = bell(hz(m), decay * 3, rng, ratio=2.0 + rng.integers(0, 2), index=1.2, decay=decay, detune=0.3)
        place(out, p, ns(t), rng.uniform(0.3, 1.0))
    return out / (np.max(np.abs(out)) + 1e-12)


def chord_swell(notes, dur, rng, attack=0.3, release=None, bright=3000.0, detune=0.2, voices=3):
    """Soft supersaw-ish chord swell (pads for hyper / light / finish), in key."""
    n = ns(dur)
    release = release or dur * 0.6
    out = np.zeros(n)
    for m in notes:
        for v in range(voices):
            out += osc.saw(hz(m) * (1 + (v - (voices - 1) / 2) * detune * 0.01), n, phase0=rng.uniform())
    out = filt.lp(out, bright, 2)
    return out * env.bp([(0, 0), (attack, 1.0, 2.0), (dur - release, 0.8), (dur, 0.0, -2.0)], n) / (len(notes) * voices)


def shepard(n, cycles, rng, base=55.0, octaves=8, voices=None, direction=1.0, sigma=1.2, partial="sine"):
    """Shepard-Risset glissando over n samples, doing `cycles` octave climbs (integer -> seamless loop).
    Each voice glides one octave per cycle, weighted by a Gaussian over log-frequency."""
    voices = voices or octaves
    t = np.arange(n) / n
    out = np.zeros(n)
    centre = octaves / 2.0
    # over one loop voice v ends where voice v + shift starts : voices form chains that must share a phase offset
    # (with a whole number of cycles per voice the phase then carries over the seam exactly)
    shift = int(round(cycles * voices / octaves))
    chains = int(np.gcd(shift, voices)) if shift else voices
    chain_phase = rng.uniform(0, 1, chains)
    for v in range(voices):
        pos = (v * octaves / voices + direction * cycles * t) % octaves
        f = base * 2 ** pos
        w = np.exp(-0.5 * ((pos - centre) / sigma) ** 2)
        # integer number of cycles over the loop (seamless phase) ; the octave wrap happens where w ~ 0
        total = np.sum(f) / SR
        f = f * (max(1.0, np.round(total)) / total)
        ph = np.cumsum(f) / SR - f / SR
        out += w * np.sin(2 * np.pi * (ph + chain_phase[v % chains]))
    return out / voices


def hum(n, f=50.0, rect=True, harmonics_lp=1800.0, periodic=True):
    """Mains hum : full-wave rectified sine (energy at 2f and harmonics), low-passed. periodic : the frequency is
    nudged to a whole number of cycles over n and the filter runs in steady state (seamless loop)."""
    if periodic:
        f = max(1.0, np.round(f * n / SR)) * SR / n
    t = np.arange(n) / SR
    y = np.abs(np.sin(2 * np.pi * f * t)) if rect else np.sin(2 * np.pi * f * t)
    y = y - np.mean(y)
    if periodic:
        return steady(lambda z: filt.lp(z, harmonics_lp, 2), y)
    return filt.lp(y, harmonics_lp, 2)


# ------------------------------------------------------------------ space and stereo

def widen(mono, width=0.5, seed=1, hp_hz=250.0, length_ms=18.0):
    """Mono -> stereo : mid = mono, side = decorrelated copy above hp_hz * width. Mono sum == source."""
    mono = np.asarray(mono, dtype=np.float64)
    side = filt.decorrelate(mono, seed, length_ms, 1800.0, hp_hz) * width
    return ms_decode(mono, side)


def widen_loop(mono, width=0.5, seed=1, hp_hz=250.0, length_ms=18.0):
    """widen() for loops : decorrelation in steady state, so the side crosses the seam."""
    return steady(lambda z: widen(z, width, seed, hp_hz, length_ms), np.asarray(mono, dtype=np.float64))


def loop_freq(f, n):
    """Nudge a frequency to a whole number of cycles over n samples (seamless oscillators in loops)."""
    return max(1.0, np.round(f * n / SR)) * SR / n


def panned_hi(mono, p, fc=200.0):
    """Pan only above fc ; the low end stays in the centre (mono-compatible placement of point sources)."""
    lo, hi = filt.lr_split(np.asarray(mono, dtype=np.float64), fc)
    return panned(hi, p) + np.stack([lo, lo], axis=1)


def panned(mono, p):
    a = (np.clip(p, -1, 1) + 1.0) * np.pi / 4.0
    return np.stack([mono * np.cos(a), mono * np.sin(a)], axis=1) * np.sqrt(2.0)


def tail(x, preset="room", wet_db=-14.0, lp_hz=None, hp_hz=None):
    """Reverb return only (stereo, HP 250-300 on the return)."""
    return space.send(x, preset, wet_db, hp_hz=hp_hz, lp_hz=lp_hz)


def assemble(*parts):
    """Sum stereo / mono parts (mono ones go to the centre)."""
    return mix(*parts)


def norm(x, peak=1.0):
    return np.asarray(x, dtype=np.float64) * (peak / (np.max(np.abs(x)) + 1e-12))


def hp_all(x, f, order=2):
    return filt.hp(x, f, order)


def at(x, seconds):
    """Delay a part by `seconds` (pads the front)."""
    d = ns(seconds)
    x = np.asarray(x, dtype=np.float64)
    return np.concatenate([np.zeros((d,) + x.shape[1:]), x], axis=0)


def fit_transient(trans, body, category, gain=1.0, peak_ms=None, margin_db=1.0, gr_allow_db=0.4, min_ratio=0.35):
    """Mix a transient layer over a body at the designed `gain`, clamped into the window the category allows :
    high enough that the transient is the peak (within peak_ms, defaults to the category rule), low enough that
    peak-to-loudness (TP - LUFS-M max) fits the category budget, so mastering needs <= gr_allow_db of limiting.
    This is a mixing decision on the transient layer, made at design time ; the limiter is not involved."""
    from ..config import CATEGORIES
    from ..dsp import loudness
    c = CATEGORIES[category]
    t = to_stereo(trans)
    b = to_stereo(body)
    n = max(len(t), len(b))
    t, b = fit(t, n), fit(b, n)
    # peak-to-loudness budget : short sounds cannot be "fixed" by the limiter (their loudness sits where their peaks
    # are, so limiting lowers the loudness as much as the peak) ; the mix itself must fit TP - target + a little.
    budget = c["tp"] - c["target"] + gr_allow_db if c.get("target") is not None else 99.0
    pk_ms = peak_ms if peak_ms is not None else c.get("peak_ms")

    def psr(g):
        m = t * g + b
        return loudness.true_peak(m) + 0.1 - loudness.momentary_max(m)

    # a body that alone overshoots the budget is made denser (oversampled tanh, step by step) - the designer's
    # "saturate the body" move, decided here at design time, never by the limiter
    if c.get("target") is not None and psr(0.0) > budget - 0.3:
        b0 = b
        for drive in (1.4, 1.8, 2.2, 2.6, 3.0):
            b = dense(b0, drive)
            if psr(0.0) <= budget - 0.3:
                break

    lo_g = 0.0
    if pk_ms:
        # the transient's 5 ms RMS must beat the body's after pk_ms (same envelope as the QA time-to-peak)
        w = ns(pk_ms / 1000.0)
        et, eb = env5(t), env5(b)
        late = np.max(eb[w:]) if len(eb) > w else 0.0
        early_t = np.max(et[:w]) + 1e-12
        lo_g = late * 10 ** (margin_db / 20.0) / early_t
    g = max(gain, lo_g)
    if psr(g) > budget:
        a, z = 0.0, g
        for _ in range(18):
            mid = 0.5 * (a + z)
            if psr(mid) > budget:
                z = mid
            else:
                a = mid
        g = max(a, gain * min_ratio)
    return t * g + b


def dense(x, drive=2.0):
    """Body density : oversampled tanh normalised to the input peak (lowers the crest of noisy bodies by a few dB,
    the way a designer saturates a body layer). Never applied to transients."""
    x = np.asarray(x, dtype=np.float64)
    pk = np.max(np.abs(x)) + 1e-12
    return nonlin.tanh(x / pk, drive, 4) * pk


def env5(x):
    """5 ms RMS envelope, one value per sample (same definition as the QA time-to-peak)."""
    st = to_stereo(x)
    p = (st ** 2).sum(axis=1)
    w = ns(0.005)
    c = np.concatenate([[0.0], np.cumsum(p)])
    idx = np.arange(len(p))
    return np.sqrt((c[np.minimum(idx + w, len(p))] - c[idx]) / w)


def finish(x, fade_out=0.02, floor_db=-75.0):
    """Trim the silent end of a one-shot and fade it."""
    from ..dsp.util import trim_tail
    return trim_tail(x, floor_db, fade_out)
