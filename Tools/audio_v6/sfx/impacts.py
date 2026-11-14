"""Impacts and breaks : sfx_impact (pressure only), breaks by material (glass, ice, crate / wood, door / metal,
rock), punch, freeze, explosion (+ far), vehicle crash, crash, car hit, hammer, shatter, thunder (+ far), meteor."""
import numpy as np

from ..registry import asset, alias
from ..dsp import osc, env, filt, modal, motion, granular, stft
from ..dsp.util import mix, place
from . import layers as L


def _shards(dur, r, count, f_lo, f_hi, kind="glass", decay=0.05, alpha=1.7, t0=0.004):
    """Debris with a few bouncing big pieces on top of a power-law rain."""
    out = L.debris(dur, r, count=count, f_lo=f_lo, f_hi=f_hi, kind=kind, decay=decay, t0=t0, alpha=alpha, contact_ms=0.15)
    n = L.ns(dur)
    for _ in range(3):
        times, amps = granular.bounce(r.uniform(0.08, 0.25), r.uniform(0.08, 0.14), r.uniform(0.5, 0.7), 7, rng=r)
        f0 = np.exp(r.uniform(np.log(f_lo), np.log(f_hi)))
        for t, a in zip(times, amps):
            if t < dur:
                h = modal.hit(f0, kind, L.ns(decay * 3), r, decay=decay, damp=0.8, contact_ms=0.2)
                place(out, L.norm(h), L.ns(t), a * 0.5)
    return out / (np.max(np.abs(out)) + 1e-12)


@asset("sfx_impact", "impact", takes=3, family="impact", priority=30, cooldown_ms=80, max_voices=2,
       notes="pressure only : sub thump + air whomp + 2 ms crack")
def impact(ctx):
    r = ctx.rng
    crack = L.snap(r, 2.0, 1500, 11000, 0.6)
    thump = L.thump(r.uniform(62, 75), r, dur=0.22, drop=2.0, tau_p=0.02, hp=40, drive=2.0)
    whomp = L.air(0.45, r, 450, 160, q=0.8, peak_db=6, lo=60, hi=3000, env_pts=[(0, 0), (0.004, 1), (0.45, 0, -4)])
    # a long quiet canyon tail : the pressure hit stays punchy (crest) while the first 400 ms carry the loudness
    body = mix(L.widen(thump * 0.55, 0.0), L.widen(whomp * 0.6, 0.5, 301 + ctx.take), L.tail(mix(whomp, thump), "room", -14.0),
               L.tail(whomp, "canyon", -20.0, lp_hz=3000))
    return L.finish(L.fit_transient(L.widen(crack, 0.1, 300), filt.hp(body, 40.0, 3), "impact", gain=0.9))


def _break(ctx, material):
    r = ctx.rng
    if material == "glass":
        crack = L.snap(r, 2.0, 2500, 14000, 0.7)
        ring = modal.hit(r.uniform(1800, 2600), "glass", L.ns(0.8), r, decay=0.35, damp=1.0, contact_ms=0.12)
        shards = _shards(1.2, r, 70, 2500, 9000, "glass", 0.05)
        thud = L.thump(150, r, dur=0.06, hp=90)
        body = mix(L.norm(ring) * 0.35, shards * 0.55, thud * 0.35)
        tail_p = ("room", -12.0)
    elif material == "ice":
        crack = L.snap(r, 3.0, 1500, 10000, 0.5)
        creak = L.crackle(0.3, r, 120, 10, 0.01, 800, 6000) * env.decay(L.ns(0.3), 0.08)
        ring = modal.hit(r.uniform(1100, 1500), "glass", L.ns(0.9), r, decay=0.4, damp=0.9, contact_ms=0.3)
        shards = _shards(1.0, r, 55, 1500, 6000, "glass", 0.06)
        thud = L.thump(120, r, dur=0.08, hp=70)
        body = mix(creak * 0.18, L.norm(ring) * 0.35, shards * 0.5, thud * 0.35)
        tail_p = ("hall", -12.0)
    elif material == "crate":
        crack = L.snap(r, 3.0, 800, 6000, 0.5)
        plank = modal.hit(r.uniform(180, 260), "bar", L.ns(0.5), r, decay=0.2, damp=1.4, contact_ms=1.5)
        splinter = L.rustle(0.35, r, 3000, 1000, 6500, grain_ms=(0.3, 1.2), env_pts=[(0, 0), (0.003, 1), (0.35, 0, -4)])
        pieces = _shards(0.8, r, 22, 400, 1600, "bar", 0.05, 1.4)
        thud = L.thump(110, r, dur=0.1, hp=60)
        body = mix(L.norm(plank) * 0.45, splinter * 0.4, pieces * 0.22, thud * 0.3)
        tail_p = ("room", -9.0)
    elif material == "door":
        crack = L.snap(r, 3.0, 1000, 8000, 0.5)
        clang = modal.hit(r.uniform(140, 200), "tube", L.ns(1.6), r, decay=1.2, damp=0.6, contact_ms=0.5)
        clang2 = modal.hit(r.uniform(330, 420), "plate", L.ns(1.0), r, decay=0.6, damp=0.7, contact_ms=0.4)
        bits = _shards(0.7, r, 18, 800, 3000, "plate", 0.06, 1.4)
        thud = L.thump(90, r, dur=0.14, hp=50, drive=1.8)
        body = mix(L.norm(clang) * 0.45, L.norm(clang2) * 0.25, bits * 0.3, thud * 0.6)
        tail_p = ("hall", -18.0)
    else:  # rock
        crack = L.snap(r, 3.5, 700, 6000, 0.5)
        thud = L.thump(r.uniform(70, 85), r, dur=0.16, drop=1.8, hp=45, drive=2.0)
        rubble = _shards(0.9, r, 45, 400, 2600, "plate", 0.025, 1.5)
        dust = L.air(0.8, r, 1500, 500, q=0.8, peak_db=5, lo=200, hi=7000, env_pts=[(0, 0), (0.02, 1), (0.8, 0, -4)])
        body = mix(thud * 0.7, rubble * 0.5, dust * 0.3)
        tail_p = ("canyon", -20.0)
    body = filt.hp(L.dense(body, 1.8), 45.0, 3)
    st = mix(L.widen(body, 0.55, 310 + ctx.take), L.tail(body, *tail_p))
    return L.finish(L.fit_transient(L.widen(crack, 0.15, 309), st, "impact", gain=0.5))


for _m in ("glass", "ice", "crate", "door", "rock"):
    def _fn(ctx, _m=_m):
        return _break(ctx, _m)
    asset("sfx_break_%s" % _m, "impact", takes=3, family="break", priority=50, cooldown_ms=60, max_voices=3)(_fn)
alias("sfx_shatter", "sfx_break_glass_1")
alias("sfx_crate", "sfx_break_crate_2")
alias("sfx_rock", "sfx_break_rock_2")


@asset("sfx_punch_hit", "impact", takes=3, family="punch", priority=40, cooldown_ms=60, max_voices=2)
def punch_hit(ctx):
    r = ctx.rng
    crack = L.snap(r, 2.0, 1500, 9000, 0.6)
    thump = L.thump(r.uniform(95, 115), r, dur=0.12, drop=1.8, hp=55, drive=2.2)
    slap = filt.bp(r.standard_normal(L.ns(0.08)), 300, 2500) * env.decay(L.ns(0.08), 0.016, attack=0.0005)
    whomp = L.air(0.6, r, 900, 250, q=0.9, peak_db=6, lo=100, hi=6000, env_pts=[(0, 0), (0.003, 1), (0.15, 0.6), (0.6, 0, -4)])
    body = mix(L.widen(thump * 0.4, 0.0), L.widen(L.dense(mix(L.norm(slap) * 0.5, whomp * 0.7), 2.0), 0.4, 321 + ctx.take),
               L.tail(mix(whomp, slap), "room", -12.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.1, 320), filt.hp(body, 55.0), "impact", gain=0.6))


@asset("sfx_punch_whoosh", "core", takes=3, family="punch", priority=80, cooldown_ms=60, max_voices=2,
       meta=dict(waive={"time_to_peak": "a swing : onset immediate, the air peaks ~20 ms in"}))
def punch_whoosh(ctx):
    r = ctx.rng
    k = 1 + 0.08 * (ctx.take - 2)
    w = L.air(0.24, r, 700 * k, 3800 * k, q=1.5, peak_db=12, lo=300, hi=10000, curve=-1.5,
              env_pts=[(0, 0), (0.004, 1.0), (0.02, 0.85), (0.24, 0, -4)])
    cloth = L.snap(r, 2.5, 800, 5000, 0.3)
    body = mix(L.widen(L.dense(w, 1.8) * 0.8, 0.5, 331 + ctx.take), L.tail(w, "room", -14.0))
    return L.finish(L.fit_transient(L.widen(cloth, 0.1, 330), filt.hp(body, 200.0), "core", gain=0.4))


@asset("sfx_freeze", "impact", family="impact", priority=40, cooldown_ms=200, max_voices=1)
def freeze(ctx):
    r = ctx.rng
    crack = L.snap(r, 2.5, 1500, 10000, 0.5)
    thud = L.thump(70, r, dur=0.18, drop=1.6, hp=40, drive=2.0)
    frost = L.crackle(0.7, r, 90, 12, 0.02, 2500, 12000) * env.bp([(0, 0), (0.01, 1), (0.7, 0, -4)], L.ns(0.7))
    notes = L.world_chord(1, 6, "five")
    shimmer = L.glints(1.0, r, notes, count=10, decay=0.3, t_spread=0.4)
    body = mix(L.widen(thud * 0.6, 0.0), L.widen(mix(frost * 0.15, shimmer * 0.15), 0.7, 341), L.tail(mix(frost, thud), "hall", -14.0),
               L.tail(shimmer, "cosmic", -20.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.1, 340), filt.hp(body, 40.0, 3), "impact", gain=0.9))


# ============================================================================ explosions / crashes

def _fireball(dur, r, lo=40.0):
    def g(t, f):
        roll = 0.75 + 0.25 * np.sin(2 * np.pi * 3.0 * t + 1.0) * np.sin(2 * np.pi * 1.3 * t)
        fc = 900 * np.exp(-t / (dur * 0.4)) + 150
        return stft.band(f, lo, 9000, 18, 6) * stft.tilt(f, -4.5, 400) * roll * (stft.resonance(f, fc, 0.8, 6))
    n = L.ns(dur)
    y = stft.shaped_noise(n, g, r)
    return y * env.bp([(0, 0), (0.004, 1.0), (0.3, 0.6, -1.0), (dur, 0.0, -5.0)], n)


@asset("sfx_explosion", "hero", takes=2, family="explosion", priority=20, cooldown_ms=200, max_voices=3)
def explosion(ctx):
    r = ctx.rng
    crack = filt.hp(L.nwave(r.uniform(6, 10), 0.06), 120.0)
    sub = L.sub_drop(62, 30, 0.4)
    fire = _fireball(2.4, r)
    deb = _shards(2.0, r, 40, 300, 2500, "plate", 0.03, 1.4, t0=0.15)
    body = mix(L.widen(sub * 0.55, 0.0), L.widen(L.dense(L.norm(fire), 2.5), 0.8, 351 + ctx.take),
               L.widen(deb * 0.2, 0.8, 353), L.tail(fire, "canyon", -16.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.2, 350), filt.hp(body, 25.0), "hero", gain=0.6))


def _far(x, distance, r, delay_rumble=0.0):
    """Distant version : air absorption over `distance` metres, low end kept, a long echoey tail."""
    m = L.to_stereo(x).mean(axis=1)
    y = filt.air_absorb(m, distance)
    y = filt.lp(y, 2500.0)
    return L.widen(y, 0.5, 361)


@asset("sfx_explosion_far", "distant", takes=2, family="explosion", priority=120, cooldown_ms=200, max_voices=3)
def explosion_far(ctx):
    r = ctx.rng
    fire = _fireball(3.0, r, 30)
    thud = L.thump(55, r, dur=0.35, drop=1.5, hp=30)
    m = mix(thud * 0.6, L.norm(fire) * 0.8)
    m = filt.air_absorb(m, 900.0)
    body = mix(L.widen(L.dense(m, 1.6), 0.5, 362 + ctx.take), L.tail(m, "canyon", -10.0, lp_hz=2500))
    return L.finish(filt.hp(body, 28.0))


@asset("sfx_vehicle_crash", "impact", takes=2, family="crash", priority=40, cooldown_ms=150, max_voices=2)
def vehicle_crash(ctx):
    r = ctx.rng
    crack = L.snap(r, 3.0, 900, 9000, 0.5)
    thud = L.thump(r.uniform(65, 80), r, dur=0.2, drop=1.8, hp=40, drive=2.2)
    crunch = L.crackle(0.6, r, 120, 14, 0.02, 500, 6000) * env.bp([(0, 0), (0.005, 1), (0.6, 0, -4)], L.ns(0.6))
    metal = mix(L.norm(modal.hit(r.uniform(150, 220), "tube", L.ns(1.4), r, decay=0.9, damp=0.7, contact_ms=0.6)) * 0.4,
                L.at(L.norm(modal.hit(r.uniform(300, 450), "plate", L.ns(1.0), r, decay=0.5, damp=0.7)), 0.07) * 0.3)
    glass = L.at(_shards(0.9, r, 40, 2500, 8000, "glass", 0.04), 0.05) * 0.3
    scrape = L.at(L.air(0.8, r, 2200, 900, q=2.0, peak_db=10, lo=400, hi=8000, env_pts=[(0, 0), (0.05, 1), (0.8, 0, -4)]), 0.15)
    body = filt.hp(L.dense(mix(thud * 0.7, crunch * 0.45, metal, glass, scrape * 0.25), 1.8), 40.0, 3)
    st = mix(L.widen(body, 0.6, 371 + ctx.take), L.tail(body, "canyon", -20.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.15, 370), st, "impact", gain=0.5))


@asset("sfx_crash", "impact", takes=2, family="crash", priority=50, cooldown_ms=100, max_voices=2)
def crash(ctx):
    r = ctx.rng
    crack = L.snap(r, 2.5, 900, 8000, 0.5)
    thud = L.thump(r.uniform(80, 95), r, dur=0.14, drop=1.8, hp=50, drive=2.0)
    crunch = L.rustle(0.4, r, 2500, 700, 6000, grain_ms=(0.4, 1.5), env_pts=[(0, 0), (0.003, 1), (0.1, 0.6), (0.4, 0, -4)])
    metal = L.norm(modal.hit(r.uniform(260, 340), "plate", L.ns(0.9), r, decay=0.6, damp=0.8, contact_ms=0.5)) * 0.3
    body = filt.hp(L.dense(mix(thud * 0.3, crunch * 0.4, metal), 1.2), 50.0)
    st = mix(L.widen(body, 0.5, 381 + ctx.take), L.tail(body, "room", -9.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.1, 380), st, "impact", gain=0.5))


@asset("sfx_carhit", "impact", takes=2, family="crash", priority=50, cooldown_ms=100, max_voices=2)
def carhit(ctx):
    r = ctx.rng
    crack = L.snap(r, 2.5, 800, 7000, 0.5)
    thud = L.thump(r.uniform(85, 100), r, dur=0.12, drop=1.7, hp=55, drive=2.0)
    panel = L.norm(modal.hit(r.uniform(180, 260), "plate", L.ns(1.0), r, decay=0.7, damp=0.7, contact_ms=1.0)) * 0.45
    rattle = L.rustle(0.4, r, 1500, 1500, 6000, grain_ms=(0.5, 2.0), env_pts=[(0, 0), (0.003, 1), (0.4, 0, -4)])
    body = filt.hp(L.dense(mix(thud * 0.3, panel, rattle * 0.3), 1.2), 55.0)
    st = mix(L.widen(body, 0.45, 391 + ctx.take), L.tail(body, "room", -9.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.1, 390), st, "impact", gain=0.5))


@asset("sfx_hammer_hit", "impact", takes=2, family="crash", priority=50, cooldown_ms=100, max_voices=2)
def hammer_hit(ctx):
    r = ctx.rng
    crack = L.snap(r, 1.5, 2000, 12000, 0.8)
    anvil = modal.hit(r.uniform(420, 520), "bar", L.ns(2.2), r, decay=1.9, damp=0.5, contact_ms=0.3)
    thud = L.thump(r.uniform(85, 100), r, dur=0.12, drop=1.6, hp=55, drive=1.8)
    body = filt.hp(mix(L.norm(anvil) * 0.5, thud * 0.35), 55.0)
    st = mix(L.widen(body, 0.35, 401 + ctx.take), L.tail(anvil, "hall", -12.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.1, 400), st, "impact", gain=0.5))


# ============================================================================ thunder, meteor

def _thunder(dur, r, crack_amt=1.0):
    n = L.ns(dur)
    crackle = L.crackle(0.5, r, 200, 20, 0.006, 1500, 14000) * env.decay(L.ns(0.5), 0.08)

    def g(t, f):
        roll = 0.6 + 0.4 * np.abs(np.sin(2 * np.pi * 0.9 * t + 0.4) * np.sin(2 * np.pi * 0.37 * t))
        return stft.band(f, 35, 2500, 18, 12) * stft.tilt(f, -6.0, 200) * roll
    rumble = stft.shaped_noise(n, g, r) * env.bp([(0, 0), (0.05, 1.0, 2.0), (dur * 0.4, 0.6), (dur, 0.0, -5.0)], n)
    return L.norm(crackle) * crack_amt, L.norm(rumble)


@asset("sfx_thunder", "hero", takes=2, family="thunder", priority=30, cooldown_ms=500, max_voices=2)
def thunder(ctx):
    r = ctx.rng
    crack, rumble = _thunder(4.0, r)
    strike = L.snap(r, 2.0, 1500, 12000, 0.6)
    crack = mix(strike * 0.8, crack)
    body = mix(L.widen(L.dense(rumble, 2.5), 0.8, 411 + ctx.take), L.tail(rumble, "canyon", -14.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.5, 410), filt.hp(body, 30.0), "hero", gain=0.8))


@asset("sfx_thunder_far", "distant", takes=2, family="thunder", priority=120, cooldown_ms=500, max_voices=2)
def thunder_far(ctx):
    r = ctx.rng
    crack, rumble = _thunder(5.0, r, 0.2)
    m = filt.air_absorb(mix(crack * 0.2, rumble), 2500.0)
    body = mix(L.widen(L.dense(L.norm(m), 1.6), 0.7, 413 + ctx.take), L.tail(m, "canyon", -10.0, lp_hz=1500))
    return L.finish(filt.hp(body, 30.0))


@asset("sfx_meteor_fall", "core", takes=2, family="meteor", priority=60, cooldown_ms=300, max_voices=2,
       meta=dict(waive={"time_to_peak": "a fly-by : the roar peaks as it passes"}))
def meteor_fall(ctx):
    r = ctx.rng
    dur = 2.2
    n = L.ns(dur)

    def g(t, f):
        return stft.band(f, 120, 9000, 12, 6) * stft.tilt(f, -3.0, 600) * stft.resonance(f, 1400, 1.5, 6)
    roar = stft.shaped_noise(n, g, r)
    whistle = osc.sine(np.full(n, 1800.0) * (1 + 0.01 * np.sin(np.arange(n) / 900)), n) * 0.15
    crackle = L.crackle(dur, r, 30, 8, 0.02, 1500, 8000) * 0.4
    src = mix(L.norm(roar), whistle, crackle) * env.bp([(0, 0.3), (0.01, 0.5), (dur, 1.0, 1.0)], n)
    flyby = motion.pass_by(src, 120.0, 25.0, 1.0 if ctx.take == 1 else -1.0, height=20.0, t_closest=dur * 0.75)
    flyby = flyby * env.bp([(0, 1.0), (dur - 0.35, 1.0), (dur, 0.0, -2.0)], len(flyby))[:, None]
    snap = L.snap(r, 3.0, 800, 5000, 0.2)
    return L.finish(L.fit_transient(L.widen(snap, 0.1, 420), filt.hp(L.dense(L.norm(flyby), 1.5), 80.0), "core", gain=0.2, peak_ms=0))


@asset("sfx_meteor_warn", "core", family="meteor", priority=60, cooldown_ms=500, max_voices=1,
       meta=dict(waive={"time_to_peak": "a warning swell, by design"}))
def meteor_warn(ctx):
    r = ctx.rng
    n = L.ns(1.4)
    sh = L.shepard(n, 1, r, base=110.0, octaves=6, sigma=1.0)
    tick = L.snap(r, 1.5, 2000, 9000, 0.7)
    rumble = filt.bp(osc.colored(n, r, -4.0), 60, 400) * env.bp([(0, 0.2), (1.2, 1.0, 1.5), (1.4, 0, -3)], n)
    body = mix(L.widen(L.norm(sh) * env.bp([(0, 0.3), (1.2, 1.0, 1.5), (1.4, 0, -3)], n) * 0.5, 0.5, 431),
               L.widen(L.norm(rumble) * 0.4, 0.2, 432))
    return L.finish(L.fit_transient(L.widen(tick, 0.1, 430), filt.hp(body, 60.0), "core", gain=0.6, peak_ms=0))
