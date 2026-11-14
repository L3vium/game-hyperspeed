"""Speed : pads and boosts (sonic boom N-wave + tuned sub + afterburner), rings (in-key FM bell + pass-by air),
hyper / light transformations per world key, light loop, lightspeed warp tunnel, wind layers, Shepard-Risset
charge loops."""
import numpy as np

from ..registry import asset, alias
from ..config import WORLDS
from ..dsp import osc, env, filt, motion, stft
from ..dsp.util import mix, place
from ..dsp.loops import steady
from . import layers as L


def _root_hz(world, octave=1):
    """World tonic in a given octave (octave 1 : F1 = 43.65 Hz for world 1)."""
    return L.hz(WORLDS[world or 1]["root"] % 12 + 12 * (octave + 1))


# ============================================================================ boom parts

def _afterburner(dur, r, f0=300.0, f1=2500.0, q=3.0, rumble=0.6, attack=0.004):
    """Resonant afterburner sweep : pink noise through a modulated SVF band-pass rising f0 -> f1, a roar bed, flame
    crackle, made dense."""
    n = L.ns(dur * 1.25)
    t = np.arange(n) / L.SR
    src = osc.colored(n, r, -2.0)
    fc = f0 * (f1 / f0) ** np.clip(t / (dur * 0.6), 0, 1)
    sweep = filt.svf(src, fc, q, "bpn")
    roar = filt.hp(filt.bp(osc.colored(n, r, -4.0), 90, 900), 70.0, 4)
    crack = L.crackle(dur * 1.25, r, 40, 6, 0.015, 1500, 8000)
    e = env.bp([(0, 0), (attack, 1.0), (dur * 0.3, 0.75), (dur, 0.02, -4.0), (dur * 1.25, 0.0, -2.0)], n)
    body = mix(L.norm(sweep) * 0.6, L.norm(roar) * rumble, crack * 0.18) * e
    return filt.hp(L.dense(body, 2.5), 60.0, 4)


def _slapback(x, delay=0.16, gain_db=-12.0, side=1.0):
    """A canyon slapback : one LP'd echo, panned to one side."""
    d = L.at(filt.lp(L.to_stereo(x).mean(axis=1), 3500.0), delay) * 10 ** (gain_db / 20.0)
    return L.panned(filt.hp(d, 250.0, 4), 0.6 * side)


def _boom(r, gap_ms, sub_f0, sub_f1, sub_dur, ab_dur, ab_f1, sub_gain=0.5, ab_gain=1.0, crack_gain=1.0):
    crack = filt.hp(L.nwave(gap_ms, 0.06), 120.0)
    sub = L.sub_drop(sub_f0, sub_f1, sub_dur)
    ab = _afterburner(ab_dur, r, 280, ab_f1)
    whomp = filt.bp(r.standard_normal(L.ns(0.25)), 60, 500) * env.bp([(0, 0), (0.003, 1), (0.25, 0, -4)], L.ns(0.25))
    return crack * crack_gain, sub * sub_gain, ab * ab_gain, L.norm(whomp) * 0.5


@asset("sfx_pad", "hero", takes=2, family="pad", priority=20, cooldown_ms=150, max_voices=2)
def pad(ctx):
    r = ctx.rng
    crack, sub, ab, whomp = _boom(r, r.uniform(8, 12), 70, 35, 0.4, 1.1, 2600)
    body = mix(L.widen(sub, 0.0), L.widen(whomp, 0.1, 201), L.widen(ab, 0.7, 202 + ctx.take))
    body = mix(body, _slapback(ab, 0.15 + 0.03 * ctx.take, -14, 1 if ctx.take == 1 else -1), L.tail(ab, "canyon", -22.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.15, 200), filt.hp(body, 25.0), "hero", gain=0.6))


@asset("sfx_boost", "hero", takes=2, family="pad", priority=20, cooldown_ms=150, max_voices=2)
def boost(ctx):
    r = ctx.rng
    crack, sub, ab, whomp = _boom(r, r.uniform(12, 18), 65, 33, 0.4, 1.5, 3200, ab_gain=1.1)
    body = mix(L.widen(sub, 0.0), L.widen(whomp, 0.1, 211), L.widen(ab, 0.8, 212 + ctx.take))
    body = mix(body, _slapback(ab, 0.19, -14, -1), L.tail(ab, "canyon", -20.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.15, 210), filt.hp(body, 25.0), "hero", gain=0.6))


@asset("sfx_boost_small", "core", takes=3, family="pad", priority=60, cooldown_ms=100, max_voices=2,
       meta=dict(waive={"time_to_peak": "boom then afterburner : the crack leads, the burner can top it ~20 ms in"}))
def boost_small(ctx):
    r = ctx.rng
    crack = filt.hp(L.nwave(r.uniform(3, 5), 0.05), 200.0)
    thump = L.thump(r.uniform(85, 100), r, dur=0.12, drop=1.7, hp=50)
    ab = _afterburner(0.5, r, 400, 2400, q=2.5, rumble=0.3, attack=0.018)
    body = mix(L.widen(thump * 0.6, 0.0), L.widen(filt.hp(ab, 150.0), 0.6, 221 + ctx.take), L.tail(ab, "room", -20.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.1, 220), body, "core", gain=0.5))


@asset("sfx_boost_loop", "loop", loop=True, group="loop", family="pad", priority=60, max_voices=1, import_class="loop")
def boost_loop(ctx):
    r = ctx.rng
    n = L.ns(3.0)

    def g(t, f):
        flick = 1.0 + 0.15 * np.sin(2 * np.pi * t * 7 / 3.0) * np.sin(2 * np.pi * t * 2 / 3.0 + 0.5)
        return (stft.band(f, 90, 5000, 12, 8) * stft.tilt(f, -3.0, 500) + stft.resonance(f, 900, 1.2, 6) - 1) * flick
    roar = stft.shaped_noise(n, g, r, circular=True)
    crack = L.crackle(3.0, r, 30, 6, 0.02, 1500, 8000, circular=True)
    m = mix(L.norm(roar), crack * 0.15)
    m = steady(lambda z: filt.hp(z, 80.0), m)
    return L.widen_loop(m, 0.6, 225)


# ============================================================================ rings

@asset("sfx_ring", "core", takes=3, worlds=(1, 2, 3), family="ring", priority=50, cooldown_ms=60, max_voices=3)
def ring(ctx):
    r = ctx.rng
    # three climbing takes : 5th, octave, 9th of the world key (consecutive rings climb)
    deg = [4, 7, 8][ctx.take - 1]
    note = L.world_note(ctx.world, deg, 5)
    f = L.hz(note)
    bell = mix(L.bell(f, 1.8, r, ratio=3.5, index=2.2, decay=1.4), L.bell(f * 2, 1.2, r, ratio=1.41, index=1.0, decay=0.7) * 0.35)
    strike = L.snap(r, 1.2, 3000, 12000, 0.6)
    air = L.air(1.0, r, 900, 3000, q=1.0, peak_db=6, lo=300, hi=12000, env_pts=[(0, 0.2), (0.25, 1.0, 2.0), (1.0, 0, -4)])
    passed = motion.pass_by(air, 35.0, 3.0, 1.0 if ctx.take % 2 else -1.0, t_closest=0.12, ground=0.3)
    chord = L.world_chord(ctx.world, 6, "tonic9")
    sh = L.glints(1.4, r, chord, count=7, decay=0.35, t_spread=0.4)
    tonal = mix(L.norm(bell) * 0.75, sh * 0.12)
    body = mix(L.widen(tonal, 0.35, 230 + ctx.take), L.norm(passed) * 0.1, L.tail(tonal, "hall", -14.0))
    return L.finish(L.fit_transient(L.widen(strike, 0.2, 229), filt.hp(body, 200.0), "core", gain=0.25))


# ============================================================================ hyper / light (per world key)

def _swell_chord(world, dur, r, octave=4, kind="tonic9", bright=3500.0, attack=0.04):
    notes = L.world_chord(world, octave, kind)
    pad = L.chord_swell(notes, dur, r, attack=attack, release=dur * 0.7, bright=bright, detune=0.25)
    return L.norm(pad)


@asset("sfx_hyper", "hero", worlds=(1, 2, 3), family="hyper", priority=10, cooldown_ms=300, max_voices=1)
def hyper(ctx):
    r = ctx.rng
    w = ctx.world
    f_root = _root_hz(w, 1)
    crack = filt.hp(L.nwave(7.0, 0.05), 150.0)
    sub = L.sub_drop(f_root * 2, f_root, 0.4)
    ab = _afterburner(1.4, r, 250, 3500, q=2.5, rumble=0.5)
    chord = _swell_chord(w, 2.2, r, 4, "tonic9", 4000.0, 0.03)
    rise = L.air(1.2, r, 600, 7000, q=2.0, peak_db=12, lo=300, hi=14000, curve=-1.0,
                 env_pts=[(0, 0.4), (0.02, 1.0), (1.2, 0.0, -4)])
    gl = L.glints(1.6, r, [n + 12 for n in L.world_chord(w, 5)], count=12, decay=0.4, t_spread=0.8)
    tonal = mix(chord * 0.85, gl * 0.14)
    body = mix(L.widen(sub * 0.5, 0.0), L.widen(L.dense(mix(ab, rise * 0.6), 2.0) * 0.65, 0.8, 241),
               L.widen(tonal, 0.6, 242), L.tail(tonal, "hall", -12.0), L.tail(ab, "canyon", -20.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.15, 240), filt.hp(body, 25.0), "hero", gain=0.5))


@asset("sfx_hyper_out", "core", worlds=(1, 2, 3), family="hyper", priority=40, cooldown_ms=300, max_voices=1)
def hyper_out(ctx):
    r = ctx.rng
    w = ctx.world
    n = L.ns(1.1)
    t = np.arange(n) / L.SR
    notes = L.world_chord(w, 4, "tonic9")[:3]
    bend = 2 ** (-5.0 / 12.0 * np.clip(t / 0.8, 0, 1) ** 1.5)
    down = np.zeros(n)
    for m in notes:
        for d in (-0.15, 0.15):
            down += osc.saw(L.hz(m) * (1 + d * 0.01) * bend, n, phase0=r.uniform())
    down = filt.svf(down, 3000 * bend ** 2, 0.9, "lp") * env.bp([(0, 0), (0.01, 1), (1.1, 0, -4)], n)
    fall = L.air(1.0, r, 5000, 350, q=1.5, peak_db=12, lo=200, hi=12000, curve=-1.5,
                 env_pts=[(0, 0.5), (0.01, 1.0), (1.0, 0, -4)])
    thump = L.thump(95, r, dur=0.1, hp=50)
    snap = L.snap(r, 2.5, 1500, 9000, 0.4)
    body = mix(L.widen(thump * 0.5, 0.0), L.widen(L.norm(down) * 0.35, 0.5, 251), L.widen(L.dense(fall, 1.8) * 0.5, 0.7, 252),
               L.tail(down, "hall", -18.0))
    return L.finish(L.fit_transient(L.widen(snap, 0.15, 250), filt.hp(body, 50.0), "core", gain=0.4))


@asset("sfx_light", "hero", worlds=(1, 2, 3), family="light", priority=5, cooldown_ms=400, max_voices=1)
def light(ctx):
    r = ctx.rng
    w = ctx.world
    f_root = _root_hz(w, 1)
    crack = filt.hp(L.nwave(12.0, 0.05), 150.0)
    sub = L.sub_drop(f_root * 2, f_root, 0.4)
    ab = _afterburner(1.8, r, 300, 6000, q=2.0, rumble=0.45)
    notes = L.world_chord(w, 5, "rel_major")
    n = L.ns(3.0)
    bright = np.zeros(n)
    for m in notes:
        bright += osc.supersaw(L.hz(m), n, r, detune=0.4)
    bright = filt.svf(bright, np.geomspace(9000, 2500, n), 0.8, "lp") * env.bp([(0, 0), (0.01, 1), (0.4, 0.6), (3.0, 0, -4)], n)
    burst = L.air(0.8, r, 9000, 3000, q=1.0, peak_db=8, lo=2000, hi=16000, env_pts=[(0, 0.5), (0.005, 1), (0.8, 0, -4)])
    gl = L.glints(2.2, r, [m + 12 for m in notes], count=18, decay=0.5, t_spread=1.2)
    tonal = mix(L.norm(bright) * 0.85, gl * 0.18)
    body = mix(L.widen(sub * 0.45, 0.0), L.widen(L.dense(mix(ab, burst * 0.4), 2.0) * 0.6, 0.85, 261),
               L.widen(tonal, 0.7, 262), L.tail(tonal, "cosmic", -12.0), L.tail(ab, "hall", -18.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.2, 260), filt.hp(body, 25.0), "hero", gain=0.5))


@asset("sfx_light_loop", "loop", worlds=(1, 2, 3), loop=True, group="loop", family="light", priority=30, max_voices=1,
       import_class="loop")
def light_loop(ctx):
    r = ctx.rng
    w = ctx.world
    n = L.ns(4.0)
    root = WORLDS[w]["root"] % 12 + 36
    notes = [root + 12, root + 19, root + 26, root + 31]  # root, 5th, 9th, 5th up : open, in key
    drone = np.zeros(n)
    for m in notes:
        for d in (-0.12, 0.0, 0.12):
            f = L.loop_freq(L.hz(m) * (1 + d * 0.01), n)
            drone += osc.saw(f, n, phase0=r.uniform())
    lfo = osc.periodic_lfo(n, 2)
    drone = steady(lambda z: filt.svf(z, np.tile(1800 + 700 * lfo, 3), 0.9, "lp"), drone)
    shimmer = osc.colored(n, r, -1.0, lo=5000, hi=12000) * (0.5 + 0.5 * osc.periodic_lfo(n, 6))
    crackle = L.crackle(4.0, r, 6, 4, 0.02, 3000, 11000, circular=True)
    m = mix(L.norm(drone) * 0.6, L.norm(shimmer) * 0.06, crackle * 0.12)
    m = steady(lambda z: filt.hp(z, 120.0), m)
    return L.widen_loop(m, 0.7, 265 + w)


for _n in ("sfx_hyper", "sfx_hyper_out", "sfx_light", "sfx_light_loop"):
    alias(_n, _n + "_w1")
alias("sfx_ring", "sfx_ring_w1_1")


# ============================================================================ lightspeed (warp tunnel)

def _warp(n, r, comb_from, comb_to, circular=False):
    """Detuned saws through modulated feedback combs + a cloud of tiny Doppler chirps + noise rush (mono)."""
    t = np.arange(n) / L.SR
    saws = np.zeros(n)
    for i, f in enumerate((55.0, 82.4, 110.0, 164.8)):
        for d in (-0.3, 0.3):
            ff = L.loop_freq(f * (1 + d * 0.01), n) if circular else f * (1 + d * 0.01)
            saws += osc.saw(ff, n, phase0=r.uniform())
    rush = osc.colored(n, r, -1.0, lo=300, hi=14000)
    src = mix(filt.hp(saws, 150.0) * 0.4, rush * 0.6)
    if circular:
        dl = comb_from * (comb_to / comb_from) ** (0.5 + 0.5 * osc.periodic_lfo(n, 2))
        combed = steady(lambda z: filt.comb_fb(z, np.tile(dl, 3), 0.82, damp=0.3), src)
    else:
        dl = comb_from * (comb_to / comb_from) ** (t / t[-1])
        combed = filt.comb_fb(src, dl, 0.82, damp=0.3)
    # chirps : tiny pings flying past (pitch falling through the pass)
    chirps = np.zeros(n)
    k = int(n / L.SR * 40)
    for i in range(k):
        m = L.ns(r.uniform(0.03, 0.08))
        tt = np.arange(m) / L.SR
        f0 = r.uniform(2500, 7000)
        c = np.sin(2 * np.pi * np.cumsum(f0 * (1 - 0.35 * tt / tt[-1])) / L.SR) * np.hanning(m)
        place(chirps, c, r.integers(0, n), r.uniform(0.2, 1.0), circular=circular)
    return combed, chirps


@asset("sfx_lightspeed_enter", "hero", family="lightspeed", priority=5, cooldown_ms=500, max_voices=1)
def lightspeed_enter(ctx):
    r = ctx.rng
    n = L.ns(2.4)
    combed, chirps = _warp(n, r, 0.006, 0.0006)
    crack = filt.hp(L.nwave(9.0, 0.05), 150.0)
    sub = L.sub_drop(60, 30, 0.4)
    swell = env.bp([(0, 0.6), (0.01, 1.0), (0.5, 0.8), (1.6, 0.6), (2.4, 0.0, -4)], n)
    body = mix(L.widen(sub * 0.55, 0.0), L.widen(L.dense(L.norm(combed), 2.0) * swell, 0.8, 271),
               L.widen(L.norm(chirps) * 0.12 * swell, 0.9, 272), L.tail(combed * swell, "cosmic", -18.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.2, 270), filt.hp(body, 25.0), "hero", gain=0.5))


@asset("sfx_lightspeed_loop", "hero_loop", loop=True, group="loop", family="lightspeed", priority=5, max_voices=1,
       import_class="loop")
def lightspeed_loop(ctx):
    r = ctx.rng
    n = L.ns(4.0)
    combed, chirps = _warp(n, r, 0.004, 0.0012, circular=True)
    press = np.sin(2 * np.pi * L.loop_freq(38.0, n) * np.arange(n) / L.SR) * (0.8 + 0.2 * osc.periodic_lfo(n, 4))
    centre = mix(press * 0.25, steady(lambda z: L.dense(z, 1.5), L.norm(combed)) * 0.6)
    centre = steady(lambda z: filt.hp(z, 28.0), centre)
    st = mix(L.widen_loop(centre, 0.7, 275), L.widen_loop(L.norm(chirps) * 0.1, 0.9, 276))
    return st


@asset("sfx_lightspeed_exit", "core", family="lightspeed", priority=10, cooldown_ms=500, max_voices=1)
def lightspeed_exit(ctx):
    r = ctx.rng
    n = L.ns(1.6)
    combed, chirps = _warp(n, r, 0.0008, 0.008)
    e = env.bp([(0, 1.0), (1.6, 0.0, -4)], n)
    thump = L.thump(80, r, dur=0.15, hp=45)
    snap = L.snap(r, 3.0, 1200, 9000, 0.4)
    body = mix(L.widen(thump * 0.5, 0.0), L.widen(L.dense(L.norm(combed), 2.0) * e * 0.6, 0.8, 281),
               L.widen(L.norm(chirps) * e * 0.1, 0.9, 282), L.tail(combed * e, "hall", -18.0))
    return L.finish(L.fit_transient(L.widen(snap, 0.2, 280), filt.hp(body, 45.0), "core", gain=0.4))


# ============================================================================ wind (three layers) and charge loops

def _wind(n, r, lo, hi, gust_cycles, whistle=None, tilt=-3.0, flutter=0.0):
    """Loopable wind : circular STFT-shaped noise, independent L / R above 250 Hz, common (mono) below."""
    dur = n / L.SR

    def g(t, f):
        gust = 0.75 + 0.25 * np.sin(2 * np.pi * t * gust_cycles / dur) * np.sin(2 * np.pi * t * (gust_cycles + 1) / dur + 1.3)
        out = stft.band(f, lo, hi, 18, 18) * stft.tilt(f, tilt, (lo * hi) ** 0.5) * gust
        if whistle:
            fc = whistle * (1 + 0.15 * np.sin(2 * np.pi * t * 2 / dur))
            out = out * (1 + (stft.resonance(f, fc, 6.0, 9.0) - 1) * gust)
        if flutter:
            out = out * (1 + flutter * np.sin(2 * np.pi * t * 37 / dur))
        return out
    common = stft.shaped_noise(n, g, r, circular=True)
    lch = stft.shaped_noise(n, g, r, circular=True)
    rch = stft.shaped_noise(n, g, r, circular=True)
    low_c = steady(lambda z: filt.lr_split(z, 250.0)[0], common)
    hc = steady(lambda z: filt.lr_split(z, 250.0)[1], common)
    hl = steady(lambda z: filt.lr_split(z, 250.0)[1], lch)
    hr = steady(lambda z: filt.lr_split(z, 250.0)[1], rch)
    # mostly independent L / R above 250 Hz with a little common part (inter-channel correlation ~0.15)
    a, b = np.sqrt(0.15), np.sqrt(0.85)
    return np.stack([low_c + a * hc + b * hl, low_c + a * hc + b * hr], axis=1)


@asset("sfx_wind_low", "loop", loop=True, group="loop", family="wind", priority=90, max_voices=1, import_class="loop")
def wind_low(ctx):
    return _wind(L.ns(8.0), ctx.rng, 90, 450, 1, tilt=-4.0)


@asset("sfx_wind_mid", "loop", loop=True, group="loop", family="wind", priority=90, max_voices=1, import_class="loop")
def wind_mid(ctx):
    return _wind(L.ns(8.0), ctx.rng, 450, 2600, 2, whistle=1100.0, tilt=-3.0)


@asset("sfx_wind_high", "loop", loop=True, group="loop", family="wind", priority=90, max_voices=1, import_class="loop")
def wind_high(ctx):
    return _wind(L.ns(8.0), ctx.rng, 2600, 11000, 3, tilt=-2.0, flutter=0.12)


def _charge(n, r, base, cycles, sigma, saw_mix, bright, seed):
    """Shepard-Risset glissando (endless rise) of sines and band-limited saw partials, plus a rising noise band."""
    sh = L.shepard(n, cycles, r, base=base, octaves=8, sigma=sigma)
    sh2 = L.shepard(n, cycles, r, base=base * 1.5, octaves=8, sigma=sigma) * saw_mix
    dur = n / L.SR

    def g(t, f):
        pos = (t * cycles / dur) % 1.0
        fc = 600 * 2 ** (pos * 3)
        w = np.sin(np.pi * pos) ** 2
        return stft.band(f, 300, 14000) * (0.25 + w * (stft.resonance(f, fc, 1.5, 10) - 1) / 3.0)
    rush = stft.shaped_noise(n, g, r, circular=True)
    m = mix(L.norm(sh + sh2) * 0.7, L.norm(rush) * bright)
    m = steady(lambda z: filt.hp(z, 150.0), m)
    return L.widen_loop(m, 0.6, seed)


@asset("sfx_hyper_charge_loop", "loop", loop=True, group="loop", family="charge", priority=40, max_voices=1,
       import_class="loop")
def hyper_charge_loop(ctx):
    return _charge(L.ns(4.0), ctx.rng, 55.0, 2, 1.1, 0.4, 0.25, 290)


@asset("sfx_light_charge_loop", "loop", loop=True, group="loop", family="charge", priority=40, max_voices=1,
       import_class="loop")
def light_charge_loop(ctx):
    return _charge(L.ns(4.0), ctx.rng, 110.0, 4, 1.0, 0.6, 0.35, 291)
