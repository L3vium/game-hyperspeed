"""Rivals : mono, seamless, 3D-ready loops (the game plays them through a 3D AudioSource, linear rolloff 10..220 m,
no Doppler : any movement is built into the loop) and the T-Rex roar.

Engines are built from whole firing cycles : a periodic firing frequency (integer number of combustions over the
loop, a multiple of the cylinder count) drives per-cylinder pulses placed on a circular buffer, then exhaust /
intake resonances run in steady state. Every oscillator does an integer number of cycles over its loop."""
import numpy as np

from ..registry import asset
from ..config import SR
from ..dsp import osc, env, filt, modal, stft, granular
from ..dsp.util import mix, place, ns
from ..dsp.loops import steady
from . import layers as L

SPATIAL = dict(min_distance=10, max_distance=220, rolloff="linear", doppler=0.0)


def _rival(name, notes, loop_s, **kw):
    return asset(name, "engine", loop=True, mono=True, group="rival", family="rival", priority=70, max_voices=4,
                 import_class="loop", spatial=SPATIAL, notes=notes, meta=dict(loop_s=loop_s), **kw)


def _firing(n, r, f_mean, cylinders, pulse, wobble=0.025, cycles=2, cyl_var=0.18, jitter=0.04):
    """Combustion pulses at a periodic firing frequency : returns the circular pulse train."""
    lfo = 0.65 * osc.periodic_lfo(n, cycles, phase0=r.uniform()) + 0.35 * osc.periodic_lfo(n, cycles * 3 + 1, phase0=r.uniform())
    f = f_mean * (1.0 + wobble * lfo)
    total = f.sum() / SR
    N = max(cylinders, int(round(total / cylinders)) * cylinders)
    f = f * (N / total)
    ph = np.cumsum(f) / SR
    idx = np.searchsorted(ph, np.arange(N))
    gain = 1.0 + cyl_var * r.uniform(-1, 1, cylinders)
    tone = 1.0 + 0.08 * r.uniform(-1, 1, cylinders)
    out = np.zeros(n)
    for k, i in enumerate(idx):
        c = k % cylinders
        place(out, pulse(c, tone[c], r), int(i), gain[c] * (1.0 + jitter * r.standard_normal()), circular=True)
    return out, f


def _loop_saw(freq, n):
    """Band-limited saw that loops : a whole number of cycles, rendered over 3 copies (the oversampling FIR sees the
    loop wrapped) and the middle one kept."""
    total = np.sum(freq) / SR
    fr = np.tile(freq * (max(1.0, np.round(total)) / total), 3)
    return osc.saw(fr, 3 * n)[n:2 * n]


def _shaped(n, r, g):
    return stft.shaped_noise(n, g, r, circular=True)


def _finish_loop(m, hp=45.0):
    m = steady(lambda z: filt.hp(z, hp, 4), m)
    return m / (np.max(np.abs(m)) + 1e-12)


# ============================================================================ cars : sports / race (high-revving V8)

@_rival("sfx_engine_car", "sports / race car : high-revving V8, exhaust burble, intake, tyres", 4.0)
def engine_car(ctx):
    r = ctx.rng
    n = ns(4.0)

    def pulse(c, tone, rr):
        m = ns(0.006)
        t = np.arange(m) / SR
        p = np.sin(np.pi * np.clip(t / 0.0012, 0, 1)) * (t < 0.0012)
        ring = np.sin(2 * np.pi * 180 * tone * t) * np.exp(-t / 0.002) * 0.6
        return p + ring + rr.standard_normal(m) * np.exp(-t / 0.0008) * 0.3
    train, f = _firing(n, r, 330.0, 8, pulse, wobble=0.03, cycles=2, cyl_var=0.22)
    exhaust = steady(lambda z: filt.formant_bank(z, [(330, 120, 0.0), (700, 220, -3.0), (1500, 400, -8.0), (3200, 900, -16.0)])
                     + 0.3 * filt.lp(z, 250.0), train)
    exhaust = steady(lambda z: L.dense(z, 2.2), L.norm(exhaust))
    am = f / f.mean()
    intake = osc.colored(n, r, -1.0, lo=1200, hi=6000) * (0.6 + 0.4 * np.abs(np.sin(np.pi * np.cumsum(f) / SR)))
    tyres = osc.colored(n, r, -3.0, lo=120, hi=900) * (0.8 + 0.2 * osc.periodic_lfo(n, 3))
    m = mix(exhaust * 0.8 * am, L.norm(intake) * 0.07, L.norm(tyres) * 0.12)
    # weave : the rival drifts nearer / farther (built in, no Doppler)
    m = m * (0.85 + 0.15 * osc.periodic_lfo(n, 1, phase0=0.25))
    return _finish_loop(m, 60.0)


# ============================================================================ heavy : bus / train / machine (diesel)

@_rival("sfx_engine_heavy", "bus / train / heavy machine : 6-cyl diesel clatter, turbo, gearbox, rattles", 6.0)
def engine_heavy(ctx):
    r = ctx.rng
    n = ns(6.0)

    def pulse(c, tone, rr):
        m = ns(0.03)
        t = np.arange(m) / SR
        thump = np.sin(2 * np.pi * 95 * tone * t) * np.exp(-t / 0.012)
        knock = filt.bp(rr.standard_normal(m), 1400, 4500) * np.exp(-t / 0.0015) * 0.5
        return thump + knock
    train, f = _firing(n, r, 78.0, 6, pulse, wobble=0.02, cycles=1, cyl_var=0.25, jitter=0.06)
    body = steady(lambda z: filt.formant_bank(z, [(95, 40, 0.0), (240, 90, -4.0), (900, 300, -10.0), (2600, 900, -14.0)])
                  + 0.4 * z, train)
    body = steady(lambda z: L.dense(z, 2.0), L.norm(body))
    t = np.arange(n) / SR
    turbo = sum(np.sin(2 * np.pi * L.loop_freq(3150.0 * h, n) * t + r.uniform(0, 6)) * a for h, a in ((1, 1.0), (2, 0.25)))
    turbo = turbo * (0.7 + 0.3 * osc.periodic_lfo(n, 2))
    gear = np.sin(2 * np.pi * L.loop_freq(610.0, n) * t) * (0.6 + 0.4 * osc.periodic_lfo(n, 5, phase0=0.4))
    rattle = L.crackle(6.0, r, 25, 6, 0.03, 1200, 6000, circular=True)
    rumble = osc.colored(n, r, -5.0, lo=50, hi=400)
    m = mix(body * 0.8, turbo * 0.02, gear * 0.03, rattle * 0.12, L.norm(rumble) * 0.2)
    m = m * (0.88 + 0.12 * osc.periodic_lfo(n, 1, phase0=0.6))
    return _finish_loop(m, 45.0)


# ============================================================================ jet : airliner / fighter

@_rival("sfx_engine_jet", "airliner / fighter jet : turbine roar, fan whine, buzz-saw tones, ground-reflection flanging", 6.0)
def engine_jet(ctx):
    r = ctx.rng
    n = ns(6.0)
    dur = 6.0

    def g(t, f):
        flick = 1.0 + 0.08 * np.sin(2 * np.pi * t * 3 / dur + 0.3)
        return (stft.band(f, 70, 12000, 12, 6) * stft.tilt(f, -3.5, 600) * stft.resonance(f, 900, 0.7, 6)) * flick
    roar = L.norm(_shaped(n, r, g))
    t = np.arange(n) / SR
    wob = 1 + 0.004 * osc.periodic_lfo(n, 3)
    whine = np.zeros(n)
    for h, a in ((1, 1.0), (2, 0.35), (3, 0.12)):
        fq = L.loop_freq(2550.0 * h, n)
        whine += a * np.sin(2 * np.pi * np.cumsum(fq * wob) / SR * (round(fq * dur) / max(1e-9, np.sum(fq * wob) / SR)))
    shaft = L.loop_freq(92.0, n)
    buzz = sum(np.sin(2 * np.pi * shaft * k * t + r.uniform(0, 6)) * r.uniform(0.3, 1.0) / k ** 0.5 for k in range(12, 30))
    m = mix(roar * 0.8, whine * 0.035, L.norm(buzz) * 0.06)
    # ground reflection : a slowly sweeping comb (the classic aircraft "phasing"), periodic over the loop
    d = 0.0006 + 0.0034 * (0.5 + 0.5 * osc.periodic_lfo(n, 2))
    m = steady(lambda z: filt.comb_ff(z, np.tile(d, 3), 0.55), m)
    return _finish_loop(m, 50.0)


# ============================================================================ rocket

@_rival("sfx_engine_rocket", "rocket : thrust roar, shock-diamond crackle, flame flutter", 4.0)
def engine_rocket(ctx):
    r = ctx.rng
    n = ns(4.0)
    dur = 4.0

    def g(t, f):
        return stft.band(f, 50, 10000, 18, 6) * stft.tilt(f, -4.0, 300) * stft.resonance(f, 380, 0.8, 6)
    roar = L.norm(_shaped(n, r, g))
    flutter = 1.0 + 0.18 * osc.smooth_random(n, 14.0, r)
    crack = L.crackle(dur, r, 70, 10, 0.012, 1500, 9000, circular=True)
    hiss = osc.colored(n, r, -1.0, lo=3000, hi=12000)
    m = mix(roar * flutter * 0.8, crack * 0.25, L.norm(hiss) * 0.05)
    m = steady(lambda z: L.dense(z, 1.6), L.norm(m))
    m = m * (0.9 + 0.1 * osc.periodic_lfo(n, 1))
    return _finish_loop(m, 45.0)


# ============================================================================ hum : spaceship / time / light (tonal)

@_rival("sfx_engine_hum", "spaceship / light : tonal hum, beating fifths, shimmering partials, energy hiss", 6.0)
def engine_hum(ctx):
    r = ctx.rng
    n = ns(6.0)
    t = np.arange(n) / SR
    out = np.zeros(n)
    for f0, a in ((55.0, 0.6), (82.5, 0.5), (110.0, 0.55), (165.0, 0.35), (220.0, 0.25), (330.0, 0.15), (440.0, 0.08)):
        for d in (0.0, 1.0 / 6.0 * (1 + int(f0) % 3)):  # pairs beating 1-3 times per loop
            out += a * np.sin(2 * np.pi * L.loop_freq(f0 + d, n) * t + r.uniform(0, 6))
    shimmer = sum(np.sin(2 * np.pi * L.loop_freq(f, n) * t + r.uniform(0, 6)) * (0.5 + 0.5 * osc.periodic_lfo(n, k, phase0=r.uniform()))
                  for k, f in ((2, 880.0), (3, 1320.0), (5, 1760.0)))
    hiss = osc.colored(n, r, -1.0, lo=2500, hi=9000) * (0.5 + 0.5 * osc.periodic_lfo(n, 3))
    m = mix(out / 4.0, shimmer * 0.05, L.norm(hiss) * 0.04)
    m = m * (0.85 + 0.15 * osc.periodic_lfo(n, 1, phase0=0.1))
    return _finish_loop(m, 45.0)


# ============================================================================ tick : clock (Time)

@_rival("sfx_engine_tick", "Time : tick-tock escapement, gear train whirr, two bell strikes per loop", 8.0)
def engine_tick(ctx):
    r = ctx.rng
    n = ns(8.0)
    out = np.zeros(n)
    for i in range(16):
        f0 = 2850.0 if i % 2 == 0 else 2150.0
        h = modal.modes(f0, [1.0, 2.32, 4.25], ns(0.2), r, decay=0.09, damp=1.0, amps=[1.0, 0.4, 0.15], contact_ms=0.35)
        thunk = modal.modes(420.0 if i % 2 == 0 else 360.0, [1.0, 2.76], ns(0.12), r, decay=0.06, amps=[1.0, 0.3], contact_ms=0.6)
        place(out, mix(L.norm(h) * 0.8, L.norm(thunk) * 0.35), ns(i * 0.5 + r.uniform(-0.002, 0.002)), r.uniform(0.9, 1.0), circular=True)
    whirr = np.zeros(n)
    for i in range(int(8.0 * 24)):
        c = filt.bp(r.standard_normal(ns(0.004)), 1500, 5000) * np.hanning(ns(0.004))
        place(whirr, c, int(i * n / (8.0 * 24)), r.uniform(0.3, 1.0), circular=True)
    bells = np.zeros(n)
    for tb, m in ((0.0, 76), (4.0, 71)):  # E5 then B4 : in the key of the cosmic world
        b = L.bell(L.hz(m), 3.5, r, ratio=3.5, index=1.4, decay=3.0)
        place(bells, b, ns(tb), 1.0, circular=True)
    ticks = steady(lambda z: L.dense(z, 2.0), L.norm(out))
    m = mix(ticks * 0.6, L.norm(whirr) * 0.1, L.norm(bells) * 0.35)
    # the clock sits in a stone room (circular : the tail of the loop end lands on its start)
    from ..dsp import space
    m = mix(m, steady(lambda z: space.send(z, "room", -10.0)[:len(z)].mean(axis=1), m))
    return _finish_loop(m, 150.0)


# ============================================================================ dread : Death

@_rival("sfx_engine_dread", "Death : low formant choir in a minor cluster, scythe whooshes, cold wind", 4.0)
def engine_dread(ctx):
    r = ctx.rng
    n = ns(4.0)
    t = np.arange(n) / SR
    voices = np.zeros(n)
    for m in (38, 45, 50, 51, 57):  # D2 A2 D3 Eb3 A3 : the semitone gives the dread
        for k in range(3):
            f = L.loop_freq(float(L.hz(m)) * 2 ** (r.uniform(-7, 7) / 1200.0), n)
            vib = 1 + 0.004 * osc.periodic_lfo(n, 20 + k, phase0=r.uniform())
            voices += _loop_saw(f * vib, n)
    choir = steady(lambda z: filt.formant_bank(filt.lp(z, 3000.0), [(400, 60, 0.0), (750, 80, -4.0), (2400, 150, -18.0)]), voices)
    choir = choir * (0.75 + 0.25 * osc.periodic_lfo(n, 1, phase0=0.75))
    swing = np.zeros(n)
    for tw in (0.6, 2.6):
        w = L.air(0.5, r, 900, 3500, q=2.0, peak_db=12, lo=300, hi=9000, env_pts=[(0, 0), (0.25, 1.0, 2.0), (0.5, 0, -4)])
        shing = modal.hit(r.uniform(2200, 2600), "plate", ns(0.8), r, decay=0.6, damp=0.8, contact_ms=0.3)
        place(swing, mix(w, L.at(L.norm(shing) * 0.25, 0.24)), ns(tw), 1.0, circular=True)
    wind = osc.colored(n, r, -3.0, lo=150, hi=2500) * (0.6 + 0.4 * osc.periodic_lfo(n, 2, phase0=0.2))
    m = mix(L.norm(choir) * 0.7, L.norm(swing) * 0.35, L.norm(wind) * 0.12)
    return _finish_loop(m, 60.0)


# ============================================================================ birds : falcon flock

@_rival("sfx_engine_birds", "falcons : 16 wingbeat streams, screeching wind, the odd 'kek-kek' cry", 6.0)
def engine_birds(ctx):
    r = ctx.rng
    dur = 6.0
    n = ns(dur)
    flaps = np.zeros(n)
    for b in range(16):
        beats = int(r.integers(22, 34))            # 3.7 - 5.7 beats per second, whole beats per loop
        dist = r.uniform(0.25, 1.0)
        glide = r.uniform(0, dur)                    # each bird glides (no flaps) for a while
        glide_len = r.uniform(0.6, 1.8)
        for k in range(beats):
            tk = k * dur / beats + r.uniform(-0.01, 0.01)
            if 0 <= (tk - glide) % dur < glide_len:
                continue
            m = ns(0.07)
            puff = filt.bp(r.standard_normal(m), 250 + 200 * dist, 2200 * dist + 600) * env.bp([(0, 0), (0.012, 1, 1), (0.07, 0, -3)], m)
            place(flaps, puff, ns(tk), dist * r.uniform(0.7, 1.0), circular=True)

    def g(t, f):
        fc = 3000 * (1 + 0.25 * np.sin(2 * np.pi * t * 2 / dur + 0.5))
        return stft.band(f, 400, 12000, 12, 12) * (0.3 + stft.resonance(f, fc, 5.0, 12) - 1)
    screech = L.norm(_shaped(n, r, g)) * (0.7 + 0.3 * osc.periodic_lfo(n, 3))
    cries = np.zeros(n)
    for tc in (1.3, 4.4):
        for j in range(int(r.integers(3, 5))):
            m = ns(0.07)
            tt = np.arange(m) / SR
            f0 = r.uniform(2300, 2700) * (1 - 0.18 * tt / tt[-1])
            c = osc.pm(f0, m, [1.0, 2.0], [1.2]) * env.bp([(0, 0), (0.008, 1), (0.07, 0, -3)], m)
            place(cries, c, ns(tc + j * 0.11), 1.0, circular=True)
    m = mix(L.norm(flaps) * 0.7, screech * 0.12, cries * 0.18)
    return _finish_loop(m, 150.0)


# ============================================================================ dog pack : 8-12 greyhounds

def _bark(r, yelp=False):
    m = ns(0.18 if yelp else 0.12)
    t = np.arange(m) / SR
    if yelp:
        f0 = 950 + 450 * np.sin(np.pi * t / t[-1])
    else:
        f0 = r.uniform(430, 620) * (1.25 - 0.35 * t / t[-1])
    f0 = f0 * (1 + 0.02 * r.standard_normal(m).cumsum() / np.sqrt(m))
    src = osc.saw(f0, m) + 0.25 * r.standard_normal(m)
    v = filt.formant_bank(src, [(800, 150, 0.0), (1600, 200, -4.0), (3000, 300, -10.0)])
    return L.norm(v) * env.bp([(0, 0), (0.006, 1.0), (t[-1] * 0.5, 0.7), (t[-1], 0.0, -3)], m)


@_rival("sfx_dog_pack_loop", "a pack of ~10 greyhounds galloping on grass : rotary gallop, panting, the odd bark and yelp", 5.4)
def dog_pack_loop(ctx):
    r = ctx.rng
    dur = 5.4
    n = ns(dur)
    paws = np.zeros(n)
    pant = np.zeros(n)
    dogs = int(r.integers(9, 12))
    for d in range(dogs):
        strides = int(r.integers(14, 18))          # 2.6 - 3.1 strides per second, whole strides per loop
        sp = dur / strides
        dist = r.uniform(0.35, 1.0)
        phase = r.uniform(0, sp)
        lp_hz = 3000 + 6000 * dist
        for s in range(strides):
            t0 = phase + s * sp
            # rotary gallop : hind, hind, fore, fore, then the flight phase
            for frac, a in ((0.0, 0.8), (0.11, 0.9), (0.36, 1.0), (0.47, 0.85)):
                tf = t0 + (frac + r.uniform(-0.015, 0.015)) * sp
                thud = L.thump(r.uniform(95, 140), r, dur=0.025, drop=1.4, hp=70, drive=1.0)
                turf = L.rustle(0.04, r, 2500, 1200, 5500, grain_ms=(0.3, 1.0), env_pts=[(0, 0), (0.002, 1), (0.04, 0, -3)])
                claw = filt.bp(r.standard_normal(ns(0.003)), 3500, 8000) * np.hanning(ns(0.003))
                f = mix(thud * 0.35, turf * 0.45, claw * 0.2 * r.uniform())
                place(paws, filt.lp(f, lp_hz), ns(tf), a * dist * r.uniform(0.75, 1.0), circular=True)
            # one pant per stride (exhale on the gather), a weak inhale on the stretch
            for off, a, ln in ((0.5, 1.0, 0.09), (0.05, 0.4, 0.07)):
                m = ns(ln)
                b = filt.formant_bank(r.standard_normal(m), [(650, 200, 0.0), (1250, 250, -4.0), (2600, 400, -10.0)])
                b = L.norm(b) * env.bp([(0, 0), (ln * 0.3, 1.0), (ln, 0, -2)], m)
                place(pant, b, ns(t0 + off * sp), a * dist * r.uniform(0.6, 1.0), circular=True)
    calls = np.zeros(n)
    place(calls, _bark(r), ns(r.uniform(0.5, 1.5)), 1.0, circular=True)
    place(calls, _bark(r) * 0.6, ns(r.uniform(2.6, 3.4)), 1.0, circular=True)
    place(calls, _bark(r, yelp=True) * 0.5, ns(r.uniform(4.0, 5.0)), 1.0, circular=True)
    m = mix(L.norm(paws) * 0.8, L.norm(pant) * 0.15, calls * 0.32)
    return _finish_loop(m, 70.0)


# ============================================================================ T-Rex roar (one-shot)

@asset("sfx_trex_roar", "core", mono=True, group="rival", family="rival", priority=40, cooldown_ms=3000, max_voices=1,
       spatial=SPATIAL, notes="growled glottal source with period doubling, moving jaw formants, breath",
       meta=dict(waive={"time_to_peak": "a roar swells : the onset is immediate, the peak comes ~0.4 s in"}))
def trex_roar(ctx):
    r = ctx.rng
    dur = 2.4
    n = ns(dur)
    t = np.arange(n) / SR
    f0 = env.bp([(0, 58.0), (0.35, 84.0, -1.0), (1.4, 70.0), (2.4, 50.0)], n)
    f0 = f0 * (1 + 0.03 * osc.smooth_random(n, 18.0, r))
    src = osc.saw(f0, n) * (1 + 0.3 * np.sign(np.sin(np.pi * np.cumsum(f0) / SR)))  # period doubling : the growl
    breath = r.standard_normal(n)
    jaw = env.bp([(0, 0.0), (0.3, 1.0, -1.0), (1.4, 0.8), (2.4, 0.2)], n)
    tracks = [(260 + 220 * jaw, 3.0, 1.0), (650 + 380 * jaw, 4.0, 0.7), (1500 + 700 * jaw, 5.0, 0.35), (2600 + 500 * jaw, 6.0, 0.15)]
    voiced = filt.formant_bank_tv(src, tracks)
    noisy = filt.formant_bank_tv(breath, tracks)
    chest = filt.lp(src, 160.0)
    x = mix(L.norm(voiced), L.norm(noisy) * 0.35 * (0.4 + 0.6 * jaw), L.norm(chest) * 0.25)
    x = x * env.bp([(0, 0.12), (0.02, 0.3), (0.4, 1.0, 1.0), (1.5, 0.85), (dur, 0.0, -3.0)], n)
    x = L.dense(x, 2.2)
    x = filt.hp(mix(x, L.tail(x, "canyon", -14.0).mean(axis=1)), 45.0, 4)
    return L.finish(x)
