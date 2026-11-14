"""World chaos : craters (+ far), telegraphs (tick, meteor / lightning loops), distant background explosions and
thunder, machinery bed, falling objects (pass-by), victory-lap fireworks. Anything positioned in 3D is mono."""
import numpy as np

from ..registry import asset
from ..dsp import osc, env, filt, modal, motion, stft
from ..dsp.util import mix, place
from ..dsp.loops import steady
from . import layers as L
from .impacts import _fireball, _shards, _thunder

SPATIAL_FAR = dict(min_distance=30, max_distance=3000, rolloff="log", doppler=0.0, spread=60)
SPATIAL_NEAR = dict(min_distance=4, max_distance=120, rolloff="log", doppler=0.5)


@asset("sfx_crater_impact", "hero", takes=3, family="crater", priority=15, cooldown_ms=200, max_voices=3)
def crater_impact(ctx):
    r = ctx.rng
    crack = filt.hp(L.nwave(r.uniform(11, 16), 0.07), 100.0)
    sub = L.sub_drop(r.uniform(52, 60), 28, 0.4)
    fire = _fireball(3.0, r, 35)
    rain = _shards(2.6, r, 60, 300, 2200, "plate", 0.03, 1.3, t0=0.25)
    body = mix(L.widen(sub * 0.45, 0.0), L.widen(L.dense(L.norm(fire), 3.2), 0.85, 501 + ctx.take),
               L.widen(rain * 0.18, 0.9, 504), L.tail(fire, "canyon", -14.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.2, 500), filt.hp(body, 24.0), "hero", gain=0.6))


@asset("sfx_crater_impact_far", "distant", takes=2, family="crater", priority=110, cooldown_ms=200, max_voices=3,
       mono=True, spatial=SPATIAL_FAR)
def crater_impact_far(ctx):
    r = ctx.rng
    fire = _fireball(3.5, r, 28)
    thud = L.thump(48, r, dur=0.4, drop=1.4, hp=26)
    m = filt.air_absorb(mix(thud * 0.8, L.norm(fire)), r.uniform(1200, 2000))
    m = mix(L.dense(L.norm(m), 1.6), L.tail(m, "canyon", -8.0, lp_hz=1500).mean(axis=1))
    return L.finish(filt.hp(m, 26.0))


@asset("sfx_telegraph_tick", "micro", takes=2, family="telegraph", priority=60, cooldown_ms=80, max_voices=4)
def telegraph_tick(ctx):
    r = ctx.rng
    f = [3150.0, 2650.0][ctx.take - 1]
    ping = modal.modes(f, [1.0, 2.32, 4.25], L.ns(0.12), r, decay=0.07, damp=1.0, amps=[1.0, 0.4, 0.2], contact_ms=0.1)
    tick = L.snap(r, 1.0, 2500, 12000, 0.8)
    body = L.widen(filt.hp(L.norm(ping) * 0.6, 400.0), 0.2, 511)
    return L.finish(L.fit_transient(L.widen(tick, 0.1, 510), body, "micro", gain=0.3))


@asset("sfx_meteor_telegraph_loop", "loop", loop=True, mono=True, group="world", family="telegraph", priority=40,
       max_voices=4, import_class="loop", spatial=SPATIAL_NEAR)
def meteor_telegraph_loop(ctx):
    r = ctx.rng
    n = L.ns(2.0)
    sh = L.shepard(n, 1, r, base=90.0, octaves=7, sigma=1.0)
    pulse = 0.55 + 0.45 * (0.5 + 0.5 * osc.periodic_lfo(n, 8)) ** 2
    rumble = osc.colored(n, r, -5.0, lo=60, hi=500)
    roar = osc.colored(n, r, -2.0, lo=400, hi=5000) * (0.5 + 0.5 * osc.periodic_lfo(n, 4, phase0=0.25))
    m = mix(L.norm(sh) * pulse * 0.6, L.norm(rumble) * 0.35, L.norm(roar) * 0.12)
    return steady(lambda z: filt.hp(z, 60.0), m)


@asset("sfx_lightning_telegraph_loop", "loop", loop=True, mono=True, group="world", family="telegraph", priority=40,
       max_voices=4, import_class="loop", spatial=SPATIAL_NEAR)
def lightning_telegraph_loop(ctx):
    r = ctx.rng
    n = L.ns(2.0)
    hum = L.hum(n, 50.0, True, 3000.0)
    static = L.crackle(2.0, r, 40, 10, 0.02, 2500, 12000, circular=True)
    sizzle = osc.colored(n, r, 0.0, lo=4000, hi=11000) * (0.4 + 0.6 * (0.5 + 0.5 * osc.periodic_lfo(n, 16)) ** 3)
    m = mix(L.norm(hum) * 0.45, static * 0.45, L.norm(sizzle) * 0.1)
    return steady(lambda z: filt.hp(z, 80.0), m)


@asset("sfx_bg_explosion", "distant", takes=4, mono=True, group="world", family="bg", priority=180, cooldown_ms=300,
       max_voices=4, spatial=SPATIAL_FAR)
def bg_explosion(ctx):
    r = ctx.rng
    fire = _fireball(r.uniform(2.5, 4.0), r, 30)
    thud = L.thump(r.uniform(42, 55), r, dur=0.35, drop=1.4, hp=26)
    m = filt.air_absorb(mix(thud * 0.7, L.norm(fire)), r.uniform(1500, 3500))
    m = mix(L.dense(L.norm(m), 1.6), L.tail(m, "canyon", -6.0, lp_hz=1200).mean(axis=1))
    return L.finish(filt.hp(m, 26.0))


@asset("sfx_bg_thunder", "distant", takes=4, mono=True, group="world", family="bg", priority=180, cooldown_ms=500,
       max_voices=3, spatial=SPATIAL_FAR)
def bg_thunder(ctx):
    r = ctx.rng
    crack, rumble = _thunder(r.uniform(4.0, 6.0), r, 0.1)
    m = filt.air_absorb(mix(crack * 0.1, rumble), r.uniform(2500, 5000))
    m = mix(L.dense(L.norm(m), 1.5), L.tail(m, "canyon", -6.0, lp_hz=1000).mean(axis=1))
    return L.finish(filt.hp(m, 28.0))


@asset("sfx_bg_machine_loop", "ambience", loop=True, mono=True, group="world", family="bg", priority=200, max_voices=2,
       import_class="loop", spatial=dict(min_distance=10, max_distance=400, rolloff="log", doppler=0.0))
def bg_machine_loop(ctx):
    r = ctx.rng
    dur = 6.0
    n = L.ns(dur)
    t = np.arange(n) / L.SR
    drone = np.zeros(n)
    for h, a in ((1, 1.0), (2, 0.6), (3, 0.35), (5, 0.2), (7, 0.1)):
        drone += a * np.sin(2 * np.pi * L.loop_freq(48.0 * h, n) * t + r.uniform(0, 6))
    drone *= 0.8 + 0.2 * osc.periodic_lfo(n, 12)
    clank = np.zeros(n)
    for i in range(6):
        h = modal.hit(r.uniform(180, 420), "tube", L.ns(0.8), r, decay=0.5, damp=0.8, contact_ms=0.6)
        place(clank, L.norm(h), L.ns(i * dur / 6 + r.uniform(0, 0.15)), r.uniform(0.4, 1.0), circular=True)
    hiss = osc.colored(n, r, -1.0, lo=1500, hi=8000) * (0.6 + 0.4 * osc.periodic_lfo(n, 3))
    m = mix(drone * 0.5, clank * 0.3, L.norm(hiss) * 0.08)
    return steady(lambda z: filt.hp(z, 45.0, 4), m)


@asset("sfx_falling", "distant", takes=2, family="bg", priority=150, cooldown_ms=300, max_voices=3,
       notes="falling object fly-by (Doppler pass-by)")
def falling(ctx):
    r = ctx.rng
    dur = 3.0
    n = L.ns(dur)
    whistle = osc.sine(np.full(n, r.uniform(1500, 2200)), n) * 0.25
    roar = stft.shaped_noise(n, lambda t, f: stft.band(f, 200, 8000) * stft.resonance(f, 900, 1.2, 6), r)
    src = mix(whistle, L.norm(roar) * 0.6) * env.bp([(0, 0.2), (dur * 0.7, 1.0, 1.5), (dur, 0.6)], n)
    fly = motion.pass_by(src, 70.0, 40.0, 1.0 if ctx.take == 1 else -1.0, height=60.0, t_closest=dur * 0.6, focus=0.6)
    fly = fly * env.bp([(0, 1.0), (dur - 0.4, 1.0), (dur, 0.0, -2.0)], len(fly))[:, None]
    return L.finish(filt.hp(L.norm(fly), 120.0))


@asset("sfx_fireworks_loop", "ambience", loop=True, group="world", family="bg", priority=200, max_voices=1,
       import_class="loop", notes="victory-lap ambience")
def fireworks_loop(ctx):
    r = ctx.rng
    dur = 8.0
    n = L.ns(dur)
    out = np.zeros((n, 2))
    for i in range(10):
        t0 = r.uniform(0, dur)
        p = r.uniform(-0.8, 0.8)
        # whistle up, pop, crackle rain ; all distant (absorbed)
        wl = L.ns(r.uniform(0.6, 1.0))
        tt = np.arange(wl) / L.SR
        wh = np.sin(2 * np.pi * np.cumsum(np.linspace(900, 2400, wl)) / L.SR) * (tt / tt[-1]) * 0.15
        pop = mix(L.thump(r.uniform(60, 90), r, dur=0.25, hp=45), L.snap(r, 3.0, 400, 4000, 0.3) * 0.5)
        crackle = L.crackle(1.6, r, 40, 12, 0.05, 1500, 9000) * env.bp([(0, 0), (0.2, 1), (1.6, 0, -3)], L.ns(1.6))
        ev = mix(wh, L.at(pop, len(tt) / L.SR), L.at(crackle * 0.3, len(tt) / L.SR + 0.15))
        ev = filt.air_absorb(ev, r.uniform(300, 700))
        st = L.panned_hi(ev, p)
        place(out, st, L.ns(t0), r.uniform(0.5, 1.0), circular=True)
    crowd = osc.colored(n, r, -2.0, lo=300, hi=3000) * 0.05
    m = mix(out, np.stack([crowd, crowd], axis=1))
    return steady(lambda z: filt.hp(z, 45.0), m)
