"""Movement : steps (surfaces x tiers), jump, double jump, lands, dash, flips, wall, glide, drift, electric rope.
Every one-shot is built as transient layer + body ; fit_transient() sets the transient level inside the window the
category allows (transient = the peak, peak-to-loudness within budget)."""
import numpy as np

from ..registry import asset, alias
from ..dsp import osc, env, filt, modal, motion, stft
from ..dsp.util import mix, place
from ..dsp.loops import steady
from . import layers as L

SURFACES = ("grass", "stone", "sand", "snow", "energy", "rock")
TIERS = ("run", "sprint")


# ============================================================================ steps

def _contact(surface, r, s):
    """One foot contact on a surface : (transient, body), mono, both start at sample 0."""
    if surface == "grass":
        tr = filt.bp(r.standard_normal(L.ns(0.012)), 300, 3500) * env.decay(L.ns(0.012), 0.003, attack=0.0004)
        blades = L.rustle(0.09, r, rate=1800 * s, lo=1800, hi=7500, grain_ms=(0.4, 1.6),
                          env_pts=[(0, 0), (0.002, 1.0), (0.09, 0.0, -3.0)])
        soil = modal.hit(r.uniform(170, 230), "membrane", L.ns(0.06), r, decay=0.05, contact_ms=2.5)
        return L.norm(tr) * 0.6 * s, mix(blades * 0.5 * s, L.norm(soil) * 0.45 * s)
    if surface == "stone":
        tr = L.snap(r, r.uniform(2.0, 3.0), 1500, 8000, 0.2)
        ring = modal.hit(r.uniform(1000, 1600), "plate", L.ns(0.08), r, decay=0.035, damp=0.8, contact_ms=0.4)
        grit = L.debris(0.05, r, count=int(5 + 4 * s), f_lo=2800, f_hi=7000, decay=0.006, contact_ms=0.3)
        thud = filt.bp(r.standard_normal(L.ns(0.05)), 200, 1800) * env.decay(L.ns(0.05), 0.01, attack=0.0008)
        return tr * 0.5 * s, mix(L.norm(ring) * 0.25 * s, grit * 0.12 * s, L.norm(thud) * 0.6 * s)
    if surface == "sand":
        tr = filt.bp(r.standard_normal(L.ns(0.01)), 500, 4000) * env.decay(L.ns(0.01), 0.003, attack=0.0008)
        crunch = L.rustle(0.12, r, rate=4200 * s, lo=700, hi=5200, grain_ms=(0.3, 1.2),
                          env_pts=[(0, 0), (0.004, 1.0), (0.03, 0.6), (0.12, 0.0, -2.0)])
        push = filt.lp(r.standard_normal(L.ns(0.05)), 900) * env.decay(L.ns(0.05), 0.012, attack=0.0015)
        return L.norm(tr) * 0.4 * s, mix(crunch * 0.8 * s, L.norm(push) * 0.6 * s)
    if surface == "snow":
        tr = filt.bp(r.standard_normal(L.ns(0.01)), 600, 4500) * env.decay(L.ns(0.01), 0.0025, attack=0.0006)
        crunch = L.crackle(0.12, r, cluster_rate=70 * s, size=10, spread=0.012, lo=500, hi=4200)
        crunch = crunch * env.bp([(0, 0), (0.003, 1.0), (0.05, 0.7), (0.12, 0.0, -2.0)], len(crunch))
        squeak = filt.svf(L.rustle(0.08, r, 2500, 600, 3500), np.linspace(1100, 1500, L.ns(0.08)), 6.0, "bpn")
        push = filt.lp(r.standard_normal(L.ns(0.05)), 700) * env.decay(L.ns(0.05), 0.012, attack=0.0015)
        return L.norm(tr) * 0.4 * s, mix(crunch * 0.7 * s, L.norm(squeak) * 0.22 * s, L.norm(push) * 0.5 * s)
    if surface == "energy":
        tr = L.snap(r, 2.0, 3000, 12000, 0.2)
        ping = modal.hit(r.uniform(1900, 2500), "glass", L.ns(0.12), r, decay=0.07, damp=1.2, contact_ms=0.3)
        fizz = L.crackle(0.05, r, cluster_rate=120, size=5, spread=0.006, lo=4000, hi=13000)
        fizz = fizz * env.decay(len(fizz), 0.012)
        thud = filt.bp(r.standard_normal(L.ns(0.04)), 250, 2000) * env.decay(L.ns(0.04), 0.008, attack=0.0008)
        return tr * 0.45 * s, mix(L.norm(ping) * 0.2 * s, fizz * 0.1 * s, L.norm(thud) * 0.6 * s)
    if surface == "rock":
        tr = L.snap(r, r.uniform(2.5, 3.5), 900, 6500, 0.2)
        ring = modal.hit(r.uniform(520, 820), "plate", L.ns(0.09), r, decay=0.03, damp=0.7, contact_ms=0.7)
        gravel = L.debris(0.09, r, count=int(10 + 6 * s), f_lo=1500, f_hi=6000, decay=0.008, contact_ms=0.15)
        thud = filt.bp(r.standard_normal(L.ns(0.05)), 150, 1400) * env.decay(L.ns(0.05), 0.01, attack=0.001)
        return tr * 0.5 * s, mix(L.norm(ring) * 0.25 * s, gravel * 0.16 * s, L.norm(thud) * 0.6 * s)
    raise ValueError(surface)


def step(ctx, surface, tier):
    r = ctx.rng
    sprint = tier == "sprint"
    hard = 1.0 if sprint else 0.78
    gap = r.uniform(0.014, 0.020) if sprint else r.uniform(0.022, 0.030)
    h_tr, h_body = _contact(surface, r, hard)
    t_tr, t_body = _contact(surface, r, hard * r.uniform(0.35, 0.5))
    soft_surface = surface in ("grass", "sand", "snow")
    w = L.thump(r.uniform(85, 115) * (1.08 if sprint else 1.0), r, dur=0.05 if sprint else 0.06, drop=1.5, hp=60.0)
    scuff = L.rustle(r.uniform(0.09, 0.14), r, rate=900 if sprint else 600, lo=2000, hi=6000,
                     env_pts=[(0, 0), (0.004, 1.0), (0.03, 0.5), (0.14, 0.0, -2.0)])
    body = mix(h_body, L.at(t_body, gap), w * (0.32 if soft_surface else 0.25) * hard,
               L.at(scuff, gap * 0.6) * (0.22 if sprint else 0.14))
    body = filt.hp(L.dense(body, 3.0), 60.0, 2)
    seed = ctx.seed % 997
    trans = mix(h_tr, L.at(t_tr * 0.6, gap))
    st = L.fit_transient(L.widen(filt.hp(trans, 60.0), 0.08, seed),
                         mix(L.widen(body, 0.12, seed + 1), L.tail(body, "tight", -20.0)), "micro", margin_db=2.5)
    return L.finish(st, 0.01)


def _register_steps():
    for s in SURFACES:
        for t in TIERS:
            def fn(ctx, s=s, t=t):
                return step(ctx, s, t)
            asset("sfx_step_%s_%s" % (s, t), "micro", takes=6, family="step_%s" % s, group="sfx", priority=200,
                  cooldown_ms=40, max_voices=3, import_class="sfx_short", notes="heel then toe, %s, %s" % (s, t))(fn)
    for i in range(1, 5):
        alias("sfx_step_%d" % i, "sfx_step_grass_run_%d" % i)
    alias("sfx_step", "sfx_step_grass_run_1")


_register_steps()


# ============================================================================ jump / double jump

@asset("sfx_jump", "core", takes=4, family="jump", priority=60, cooldown_ms=60, max_voices=2)
def jump(ctx):
    r = ctx.rng
    k = 1.0 + 0.06 * (ctx.take - 2.5) + r.uniform(-0.03, 0.03)
    push = L.snap(r, 3.0, 1800, 8000, 0.15)
    scuff = L.rustle(0.04, r, 1200, 2000, 6500, env_pts=[(0, 0), (0.002, 1), (0.04, 0, -2)])
    weight = L.thump(120 * k, r, dur=0.045, drop=1.6, hp=70)
    lift = L.air(0.32, r, 650 * k, 2900 * k, q=1.3, peak_db=12, lo=300, hi=9000, curve=-2.0,
                 env_pts=[(0, 0), (0.006, 1.0, 1.0), (0.04, 0.85), (0.32, 0.0, -2.5)])
    cloth = L.rustle(0.16, r, 260, 900, 4200, grain_ms=(1.0, 4.0), env_pts=[(0, 0), (0.02, 1), (0.16, 0, -2)])
    core = filt.hp(mix(push * 0.3, scuff * 0.2, weight * 0.6), 70.0)
    body = filt.hp(L.dense(mix(lift * 0.5, cloth * 0.18), 2.0), 120.0)
    st = L.fit_transient(L.widen(core, 0.1, 11), mix(L.widen(body, 0.55, 12 + ctx.take), L.tail(body, "tight", -24.0)), "core")
    return L.finish(st)


@asset("sfx_doublejump", "core", takes=3, family="jump", priority=60, cooldown_ms=60, max_voices=2)
def doublejump(ctx):
    r = ctx.rng
    k = 1.0 + 0.05 * (ctx.take - 2) + r.uniform(-0.02, 0.02)
    snap = L.snap(r, 2.0, 2500, 10000, 0.4)
    puff = mix(L.thump(150 * k, r, dur=0.06, drop=1.4, hp=80) * 0.8,
               filt.bp(r.standard_normal(L.ns(0.08)), 180, 1100) * env.decay(L.ns(0.08), 0.018, attack=0.001) * 0.35)
    n = L.ns(0.42)
    spin = L.air(0.42, r, 900 * k, 4800 * k, q=2.0, peak_db=15, lo=400, hi=12000, curve=-1.5,
                 env_pts=[(0, 0), (0.006, 1.0, 1.0), (0.06, 0.8), (0.42, 0.0, -2.5)])
    spin = spin * (0.75 + 0.25 * np.sin(2 * np.pi * np.cumsum(np.linspace(22, 9, n)) / L.SR))
    sheen = L.air(0.18, r, 7000, 9000, q=0.8, peak_db=6, lo=4000, hi=15000, env_pts=[(0, 0), (0.01, 1), (0.18, 0, -3)])
    core = filt.hp(mix(snap * 0.45, puff), 80.0)
    body = filt.hp(L.dense(mix(spin * 0.45, sheen * 0.12), 2.0), 150.0)
    st = L.fit_transient(L.widen(core, 0.12, 21), mix(L.widen(body, 0.7, 22 + ctx.take), L.tail(body, "room", -22.0)), "core")
    return L.finish(st)


# ============================================================================ landings

def _land(r, weight_f, power, debris_n, cloth=0.15):
    slap1 = L.snap(r, 2.5, 900, 6000, 0.3)
    slap2 = L.snap(r, 2.0, 1200, 7000, 0.3)
    weight = L.thump(weight_f, r, dur=0.09 + 0.05 * power, drop=1.6, hp=60, drive=1.5)
    shove = filt.bp(r.standard_normal(L.ns(0.2)), 150, 1300) * env.bp(
        [(0, 0), (0.002, 1.0), (0.03, 0.6, -1.0), (0.2, 0.0, -2.0)], L.ns(0.2))
    dust = L.air(0.35, r, 1800, 700, q=0.8, peak_db=6, lo=300, hi=9000, env_pts=[(0, 0), (0.01, 1.0), (0.35, 0.0, -2.0)])
    grit = L.debris(0.15 + 0.1 * power, r, count=debris_n, f_lo=2000, f_hi=6500, decay=0.01, t0=0.003, contact_ms=0.3)
    scuff = L.rustle(0.1, r, 900, 2000, 6000, env_pts=[(0, 0), (0.003, 1), (0.1, 0, -2)])
    cl = L.rustle(0.18, r, 200, 800, 3500, grain_ms=(1.0, 4.0), env_pts=[(0, 0), (0.03, 1), (0.18, 0, -2)])
    core = mix(slap1 * 0.5, L.at(slap2, r.uniform(0.005, 0.009)) * 0.3, weight * 0.8)
    body = L.dense(mix(L.norm(shove) * 0.55 * (0.6 + power), dust * 0.5), 2.5)
    detail = mix(grit * 0.18, scuff * 0.2, L.at(cl, 0.025) * cloth * 1.3)
    return core, body, detail


@asset("sfx_land", "core", takes=4, family="land", priority=60, cooldown_ms=60, max_voices=2)
def land(ctx):
    r = ctx.rng
    core, body, detail = _land(r, r.uniform(88, 112), 0.4 + 0.1 * ctx.take, 8 + 2 * ctx.take)
    rest = mix(L.widen(filt.hp(body, 60), 0.3, 30), L.widen(filt.hp(detail, 150), 0.5, 32 + ctx.take),
               L.tail(mix(core, body), "room", -20.0))
    return L.finish(L.fit_transient(L.widen(filt.hp(core, 60), 0.05, 31), rest, "core"))


@asset("sfx_land_heavy", "impact", takes=2, family="land", priority=50, cooldown_ms=120, max_voices=2)
def land_heavy(ctx):
    r = ctx.rng
    core, body, detail = _land(r, r.uniform(68, 78), 1.2, 26)
    crack = L.snap(r, 3.5, 700, 7000, 0.4)
    whomp = filt.bp(r.standard_normal(L.ns(0.18)), 90, 600) * env.decay(L.ns(0.18), 0.045, attack=0.002)
    rubble = L.debris(0.5, r, count=30, f_lo=600, f_hi=3500, decay=0.02, t0=0.02, alpha=1.5, contact_ms=0.4)
    core = filt.hp(mix(core, crack * 0.5), 40.0, 3)
    rest = mix(L.widen(filt.hp(mix(body, L.norm(whomp) * 0.5), 40.0, 3), 0.08, 40),
               L.widen(filt.hp(mix(detail, rubble * 0.3), 120.0), 0.6, 42 + ctx.take),
               L.tail(mix(core, detail), "canyon", -24.0, lp_hz=5000))
    return L.finish(L.fit_transient(L.widen(core, 0.05, 41), rest, "impact"))


@asset("sfx_land_good", "core", takes=2, family="land", priority=55, cooldown_ms=100, max_voices=2)
def land_good(ctx):
    r = ctx.rng
    core, body, detail = _land(r, r.uniform(92, 104), 0.7, 14)
    release = L.air(0.4, r, 2400, 900, q=1.2, peak_db=10, lo=400, hi=12000,
                    env_pts=[(0, 0), (0.01, 1.0), (0.4, 0.0, -2.5)])
    pop = filt.bp(r.standard_normal(L.ns(0.03)), 1500, 6000) * env.decay(L.ns(0.03), 0.004, attack=0.0003)
    rest = mix(L.widen(filt.hp(body, 60), 0.1, 50), L.widen(filt.hp(mix(detail, release * 0.3), 150), 0.6, 52 + ctx.take),
               L.tail(release, "room", -20.0))
    return L.finish(L.fit_transient(L.widen(filt.hp(mix(core, L.norm(pop) * 0.3), 60), 0.08, 51), rest, "core"))


@asset("sfx_land_perfect", "core", worlds=(1, 2, 3), family="land_perfect", priority=40, cooldown_ms=150, max_voices=1)
def land_perfect(ctx):
    r = ctx.rng
    core, body, detail = _land(r, 100.0, 0.9, 16)
    notes = L.world_chord(ctx.world, 5, "tonic9")
    arp = np.zeros(L.ns(1.6))
    for i, m in enumerate([notes[0], notes[2], notes[4]]):
        place(arp, L.bell(L.hz(m), 1.4, r, ratio=3.0, index=1.6, decay=0.9), L.ns(0.008 + 0.045 * i), 0.7 - 0.12 * i)
    gl = L.glints(1.2, r, [n + 12 for n in notes], count=8, decay=0.25, t_spread=0.5)
    release = L.air(0.5, r, 3000, 1100, q=1.2, peak_db=10, lo=500, hi=13000, env_pts=[(0, 0), (0.01, 1.0), (0.5, 0.0, -2.5)])
    tonal = mix(L.norm(arp) * 0.3, gl * 0.1)
    rest = mix(L.widen(filt.hp(body, 60), 0.1, 60), L.widen(filt.hp(mix(detail, release * 0.3), 150), 0.6, 62),
               L.widen(tonal, 0.5, 63), L.tail(tonal, "hall", -14.0), L.tail(core, "tight", -26.0))
    return L.finish(L.fit_transient(L.widen(filt.hp(core, 60), 0.05, 61), rest, "core"))


for _a, _s in (("sfx_jump", "sfx_jump_1"), ("sfx_land", "sfx_land_1"), ("sfx_land_perfect", "sfx_land_perfect_w1")):
    alias(_a, _s)


# ============================================================================ dash (signature)

@asset("sfx_dash", "core", takes=4, family="dash", priority=40, cooldown_ms=80, max_voices=2)
def dash(ctx):
    r = ctx.rng
    # per take : sweep start / end, sweep time, resonance and comb depth all move (4 distinct rips)
    f_hi = [6000, 6800, 5400, 7400][ctx.take - 1] * r.uniform(0.97, 1.03)
    f_lo = [800, 950, 700, 880][ctx.take - 1]
    sweep_t = [0.18, 0.15, 0.21, 0.165][ctx.take - 1]
    q = [4.2, 5.5, 3.4, 4.8][ctx.take - 1]
    comb_end = [0.005, 0.0038, 0.006, 0.0045][ctx.take - 1]
    n = L.ns(0.95)
    snap = L.snap(r, r.uniform(2.0, 4.0), 2000, 12000, 0.5)
    # resonant band-pass air rip f_hi -> f_lo in ~180 ms
    src = osc.colored(n, r, -1.5)
    t = np.arange(n) / L.SR
    fc = np.where(t < sweep_t, f_hi * (f_lo / f_hi) ** (t / sweep_t),
                  f_lo * (1 - 0.25 * np.clip((t - sweep_t) / 0.4, 0, 1)))
    rip = filt.svf(src, fc, q, "bpn")
    rip = rip * env.bp([(0, 0), (0.003, 1.0, 1.0), (0.1, 0.75), (0.45, 0.22, -1.5), (0.95, 0.0, -4.0)], n)
    # into a comb flanger 0.5 -> 5 ms
    dl = np.where(t < sweep_t, 0.0005, 0.0005 + (comb_end - 0.0005) * np.clip((t - sweep_t) / 0.35, 0, 1) ** 0.7)
    rip = filt.comb_ff(rip, dl, 0.7)
    thoom = L.thump(r.uniform(90, 120), r, dur=0.14, drop=1.8, tau_p=0.015, hp=50, drive=1.8)
    turb = L.air(0.95, r, 1400, 500, q=0.9, peak_db=8, lo=250, hi=7000,
                 env_pts=[(0, 0), (0.05, 0.6, 1.0), (0.2, 1.0), (0.95, 0.0, -4.5)])
    # widening turbulent tail : the side grows over time
    tw = L.widen(turb, 1.0, 71 + ctx.take)
    m, s = (tw[:, 0] + tw[:, 1]) / 2, (tw[:, 0] - tw[:, 1]) / 2
    grow = np.clip(np.arange(len(m)) / L.ns(0.4), 0.0, 1.0) * 0.9
    tail_st = np.stack([m + s * grow, m - s * grow], axis=1) * 0.3
    body = mix(L.widen(L.norm(rip) * 0.8, 0.25, 70), L.widen(thoom * 0.4, 0.0, 69), tail_st, L.tail(rip, "room", -22.0))
    st = L.fit_transient(L.widen(snap, 0.2, 68), filt.hp(body, 50.0), "core", gain=0.5)
    return L.finish(st)


alias("sfx_dash", "sfx_dash_1")


# ============================================================================ flips (orbit)

def _flip(ctx, turns):
    r = ctx.rng
    dur = 0.36 + 0.14 * turns
    src = L.air(dur, r, 1500, 3800, q=1.6, peak_db=10, lo=500, hi=11000,
                env_pts=[(0, 0.2), (0.01, 0.6), (dur * 0.5, 1.0), (dur, 0.0, -2.0)])
    spun = motion.orbit(src, radius=0.9, turns=turns, height=0.2, start_angle=-np.pi / 2)
    whip = L.snap(r, 3.0, 1000, 6000, 0.3)
    cloth = L.rustle(dur, r, 300, 700, 3500, grain_ms=(1, 4), env_pts=[(0, 0), (0.02, 1), (dur, 0, -1.5)])
    body = mix(L.widen(cloth * 0.12, 0.3, 79), L.norm(spun) * 0.6, L.tail(L.norm(spun).mean(axis=1), "room", -20.0))
    st = L.fit_transient(L.widen(whip, 0.2, 80 + turns), filt.hp(body, 120.0), "core", gain=0.5, peak_ms=0)
    return L.finish(st)


for _t in (1, 2, 3):
    def _fn(ctx, _t=_t):
        return _flip(ctx, _t)
    asset("sfx_flip_%d" % _t, "core", family="flip", priority=90, cooldown_ms=100, max_voices=2,
          meta=dict(waive={"time_to_peak": "spin whoosh : onset immediate, energy peaks mid-spin by design"}))(_fn)


# ============================================================================ wall

@asset("sfx_wall_attach", "core", takes=2, family="wall", priority=70, cooldown_ms=100, max_voices=2)
def wall_attach(ctx):
    r = ctx.rng
    grip = L.snap(r, 2.5, 1200, 7000, 0.4)
    weight = L.thump(r.uniform(140, 165), r, dur=0.05, drop=1.5, hp=90)
    sq_n = L.ns(0.05)
    squeak = np.sin(2 * np.pi * np.cumsum(np.linspace(1800, 2300, sq_n) * (1 + 0.01 * np.sin(np.arange(sq_n) / 8))) / L.SR)
    squeak = filt.bp(squeak, 1500, 4000) * env.bp([(0, 0), (0.005, 1), (0.05, 0, -2)], sq_n)
    scuff = L.rustle(0.25, r, 1100, 1800, 6500, env_pts=[(0, 0), (0.003, 1), (0.25, 0, -2)])
    shove = filt.bp(r.standard_normal(L.ns(0.2)), 200, 1500) * env.bp([(0, 0), (0.002, 1), (0.04, 0.5, -1), (0.2, 0, -2)], L.ns(0.2))
    slide = L.air(0.35, r, 2500, 1200, q=1.0, peak_db=6, lo=400, hi=9000, env_pts=[(0, 0), (0.01, 1), (0.35, 0, -2)])
    core = filt.hp(mix(grip * 0.6, weight * 0.7), 90.0)
    body = filt.hp(L.dense(mix(L.at(squeak, 0.006) * 0.12, L.norm(shove) * 0.5, slide * 0.35), 2.5), 90.0)
    rest = mix(L.widen(body, 0.2, 90), L.widen(scuff * 0.3, 0.5, 91 + ctx.take), L.tail(mix(core, body), "room", -18.0))
    return L.finish(L.fit_transient(L.widen(core, 0.05, 92), rest, "core"))


@asset("sfx_wallrun_loop", "loop", loop=True, group="loop", family="wall", priority=70, max_voices=1,
       import_class="loop", meta=dict(cadence_hz=6.0))
def wallrun_loop(ctx):
    r = ctx.rng
    dur = 2.0
    n = L.ns(dur)
    out = np.zeros(n)
    for i in range(12):
        t = i / 6.0 + r.uniform(-0.004, 0.004)
        h_tr, h_body = _contact("stone", r, 0.75 if i % 2 else 0.85)
        t_tr, t_body = _contact("stone", r, 0.4)
        c = mix(h_tr * 0.6, h_body, L.at(mix(t_tr * 0.6, t_body), 0.016))
        place(out, c, L.ns(t), r.uniform(0.8, 1.0), circular=True)
    out = steady(lambda z: filt.hp(z, 90.0), out)

    def g(t, f):
        return stft.band(f, 300, 6000) * (0.6 + 0.4 * np.sin(2 * np.pi * t / dur * 3) ** 2)
    air = stft.shaped_noise(n, g, r, circular=True)
    air = air / np.max(np.abs(air))
    m = mix(L.norm(out) * 0.8, air * 0.12)
    return L.widen_loop(m, 0.35, 95)


# ============================================================================ glide

@asset("sfx_glide_open", "core", family="glide", priority=70, cooldown_ms=200, max_voices=1)
def glide_open(ctx):
    r = ctx.rng
    snap = L.snap(r, 3.0, 600, 5000, 0.4)
    flaps = np.zeros(L.ns(0.5))
    for i, (t, a) in enumerate(((0.0, 1.0), (0.055, 0.7), (0.1, 0.5), (0.15, 0.32))):
        f = L.rustle(0.07, r, 1500, 300, 2200, grain_ms=(1.5, 5.0), env_pts=[(0, 0), (0.004, 1), (0.07, 0, -2)])
        f = filt.svf(f, 900 - 120 * i, 1.5, "lp")
        place(flaps, L.norm(f), L.ns(t), a)
    whoomp = filt.bp(r.standard_normal(L.ns(0.3)), 100, 500) * env.bp([(0, 0), (0.006, 1), (0.3, 0, -2)], L.ns(0.3))
    lift = L.air(0.7, r, 500, 1600, q=1.0, peak_db=8, lo=200, hi=8000, env_pts=[(0, 0), (0.02, 1.0), (0.7, 0, -2)])
    core = filt.hp(snap * 0.6, 90.0)
    rest = mix(L.widen(filt.hp(L.norm(whoomp) * 0.5, 90.0), 0.15, 101),
               L.widen(filt.hp(mix(flaps * 0.4, lift * 0.45), 120.0), 0.55, 102), L.tail(mix(flaps, lift), "room", -18.0))
    return L.finish(L.fit_transient(L.widen(core, 0.1, 100), rest, "core"))


@asset("sfx_glide_loop", "loop", loop=True, group="loop", family="glide", priority=80, max_voices=1, import_class="loop")
def glide_loop(ctx):
    r = ctx.rng
    dur = 4.0
    n = L.ns(dur)
    flutter_rate = 11.0 + 3.0 * osc.smooth_random(n, 0.6, r)
    cyc = np.sum(flutter_rate) / L.SR
    flutter = 0.65 + 0.35 * np.sin(2 * np.pi * np.cumsum(flutter_rate * np.round(cyc) / cyc) / L.SR)

    def g(t, f):
        gust = 0.8 + 0.2 * np.sin(2 * np.pi * t / dur) * np.sin(2 * np.pi * t * 2 / dur + 1)
        return stft.band(f, 180, 3500, 12, 12) * stft.tilt(f, -2.0) * gust
    cloth = stft.shaped_noise(n, g, r, circular=True)
    cloth = cloth / np.max(np.abs(cloth)) * flutter
    wind = osc.colored(n, r, -2.0, lo=120, hi=6000)
    wind = wind / np.max(np.abs(wind))
    m = mix(cloth * 0.6, wind * 0.15)
    return L.widen_loop(m, 0.6, 105, length_ms=20.0)


# ============================================================================ drift

@asset("sfx_drift_start", "core", takes=2, family="drift", priority=80, cooldown_ms=200, max_voices=1)
def drift_start(ctx):
    r = ctx.rng
    n = L.ns(0.45)
    t = np.arange(n) / L.SR
    src = osc.colored(n, r, 0.0)
    f = 2200 * (1 + 0.06 * np.sin(2 * np.pi * 23 * t)) * (1 - 0.1 * t)
    squeal = filt.svf(src, f, 9.0, "bpn") + 0.5 * filt.svf(src, f * 1.52, 7.0, "bpn")
    squeal = squeal * env.bp([(0, 0), (0.008, 1), (0.25, 0.7), (0.45, 0, -2)], n)
    grit = L.crackle(0.45, r, 60, 8, 0.01, 1500, 7000) * env.bp([(0, 0), (0.005, 1), (0.45, 0, -2)], n)
    snap = L.snap(r, 3, 900, 6000, 0.4)
    w = L.thump(110, r, dur=0.06, hp=80)
    core = filt.hp(mix(snap * 0.5, w * 0.6), 80.0)
    rest = mix(L.widen(filt.hp(mix(L.norm(squeal) * 0.35, grit * 0.25), 150.0), 0.4, 112 + ctx.take),
               L.tail(squeal, "room", -22.0))
    return L.finish(L.fit_transient(L.widen(core, 0.1, 111), rest, "core"))


@asset("sfx_drift_loop", "loop", loop=True, group="loop", family="drift", priority=80, max_voices=1, import_class="loop")
def drift_loop(ctx):
    r = ctx.rng
    dur = 3.0
    n = L.ns(dur)
    src = osc.colored(n, r, -1.0)
    wob = osc.periodic_lfo(n, 69) * 0.05 + osc.periodic_lfo(n, 2, phase0=0.3) * 0.04
    f = 2100 * (1 + wob)
    squeal = steady(lambda z: filt.svf(z, np.tile(f, 3), 10.0, "bpn") + 0.4 * filt.svf(z, np.tile(f * 1.5, 3), 8.0, "bpn"), src)
    grit = L.crackle(dur, r, 80, 8, 0.01, 1500, 7000, circular=True)
    rumble = osc.colored(n, r, -4.0, lo=120, hi=900)
    m = mix(L.norm(squeal) * 0.5, grit * 0.3, L.norm(rumble) * 0.25)
    return L.widen_loop(m, 0.4, 115, length_ms=15.0)


@asset("sfx_drift_tier", "core", takes=3, worlds=(1, 2, 3), family="drift_tier", priority=60, cooldown_ms=150, max_voices=1)
def drift_tier(ctx):
    r = ctx.rng
    lvl = ctx.take
    deg = [(4, 7), (7, 9), (9, 11)][lvl - 1]
    a = L.bell(L.hz(L.world_note(ctx.world, deg[0], 5)), 0.6, r, ratio=2.0, index=1.4, decay=0.45)
    b = L.bell(L.hz(L.world_note(ctx.world, deg[1], 5)), 0.8, r, ratio=2.0, index=1.4, decay=0.6)
    tick = L.snap(r, 1.5, 3000, 12000, 0.5)
    spark = L.crackle(0.25, r, 90, 6, 0.008, 3000, 12000) * env.decay(L.ns(0.25), 0.06)
    tonal = mix(a * 0.6, L.at(b, 0.07) * 0.45)
    core = mix(tick * 0.3, L.norm(tonal)[:L.ns(0.015)] * 0.0)
    rest = mix(L.widen(L.norm(tonal) * 0.5, 0.3, 120 + lvl), L.widen(spark * 0.18, 0.7, 121), L.tail(tonal, "plate", -16.0))
    return L.finish(filt.hp(L.fit_transient(L.widen(core, 0.2, 119), rest, "core"), 200.0))


alias("sfx_drift_tier", "sfx_drift_tier_w1_1")


# ============================================================================ electric rope

def _rope_bed(n, r, circular=True):
    """Hum (rectified 50 Hz -> 100 Hz harmonics), arc crackle clusters, pulley roll, sizzle (mono parts)."""
    hum = L.hum(n, 50.0, True, 1600.0, periodic=circular)
    roll_rate = 36.0
    roll = np.zeros(n)
    k = int(n / L.SR * roll_rate)
    for i in range(k):
        c = filt.bp(r.standard_normal(L.ns(0.006)), 400, 2500) * np.hanning(L.ns(0.006))
        place(roll, c, int(i * n / k), r.uniform(0.5, 1.0), circular=circular)
    rumble = osc.colored(n, r, -4.0, lo=150, hi=700)
    arcs = L.crackle(n / L.SR, r, 14, 7, 0.03, 2500, 12000, circular=circular)
    sizzle = osc.colored(n, r, 0.0, lo=4000, hi=10000)
    sizzle = sizzle * (0.6 + 0.4 * osc.periodic_lfo(n, 13))
    return hum, roll, rumble, arcs, sizzle


@asset("sfx_grind_loop", "loop", loop=True, group="loop", family="rope", priority=50, max_voices=1, import_class="loop")
def grind_loop(ctx):
    r = ctx.rng
    n = L.ns(3.0)
    hum, roll, rumble, arcs, sizzle = _rope_bed(n, r)
    centre = mix(L.norm(hum) * 0.5, L.norm(roll) * 0.22, L.norm(rumble) * 0.18)
    spread = mix(arcs * 0.35, L.norm(sizzle) * 0.06)
    centre = steady(lambda z: filt.hp(z, 70.0), centre)
    side = steady(lambda z: filt.decorrelate(z, 131, 16.0), spread)
    return np.stack([centre + spread + side * 0.8, centre + spread - side * 0.8], axis=1)


@asset("sfx_grind_start", "core", takes=2, family="rope", priority=50, cooldown_ms=150, max_voices=1)
def grind_start(ctx):
    r = ctx.rng
    n = L.ns(0.9)
    arc = L.crackle(0.06, r, 400, 10, 0.004, 2000, 13000)
    snap = L.snap(r, 2.0, 2500, 12000, 0.6)
    latch = modal.hit(r.uniform(200, 240), "bar", L.ns(0.4), r, decay=0.25, damp=1.2, contact_ms=0.4)
    thunk = L.thump(125, r, dur=0.07, drop=1.5, hp=70)
    hum, roll, rumble, arcs, sizzle = _rope_bed(n, r, circular=False)
    swell = env.bp([(0, 0), (0.25, 1.0, 1.5), (0.6, 0.8), (0.9, 0.0, -2)], n)
    bed = mix(L.norm(hum) * 0.4, arcs * 0.3) * swell
    core = filt.hp(mix(snap * 0.5, thunk * 0.6), 70.0)
    rest = mix(L.widen(filt.hp(mix(arc * 0.45, L.norm(latch) * 0.3), 70.0), 0.25, 140 + ctx.take),
               L.widen(filt.hp(bed, 70.0), 0.4, 141), L.tail(mix(arc, latch), "room", -20.0))
    return L.finish(L.fit_transient(L.widen(core, 0.1, 139), rest, "core"))


@asset("sfx_rope_exit", "core", takes=2, family="rope", priority=55, cooldown_ms=150, max_voices=1)
def rope_exit(ctx):
    r = ctx.rng
    zap = L.crackle(0.08, r, 300, 10, 0.005, 2500, 12000)
    t = np.arange(L.ns(0.18)) / L.SR
    zip_ = np.sin(2 * np.pi * np.cumsum(3000 * (600 / 3000) ** (t / 0.18)) / L.SR) * \
        env.bp([(0, 0), (0.003, 1), (0.18, 0, -2)], len(t))
    whoosh = L.air(0.6, r, 700, 3200, q=1.4, peak_db=12, lo=300, hi=11000, curve=-1.5,
                   env_pts=[(0, 0), (0.008, 1.0), (0.06, 0.85), (0.6, 0.0, -2.5)])
    thump = L.thump(115, r, dur=0.05, hp=70)
    snap = L.snap(r, 2.0, 2500, 11000, 0.6)
    core = filt.hp(mix(snap * 0.5, thump * 0.5), 70.0)
    rest = mix(L.widen(filt.hp(mix(zap * 0.4, L.norm(zip_) * 0.18), 150.0), 0.25, 150 + ctx.take),
               L.widen(filt.hp(whoosh * 0.4, 150.0), 0.6, 151), L.tail(zap, "room", -22.0))
    return L.finish(L.fit_transient(L.widen(core, 0.1, 149), rest, "core"))


@asset("sfx_rope_aura_loop", "ambience", loop=True, mono=True, group="world", family="rope", priority=150, max_voices=4,
       import_class="loop", spatial=dict(min_distance=3, max_distance=40, rolloff="log", doppler=0.0))
def rope_aura_loop(ctx):
    r = ctx.rng
    n = L.ns(4.0)
    hum = L.hum(n, 50.0, True, 2500.0)
    corona = osc.colored(n, r, -1.0, lo=2800, hi=9000)
    corona = corona * (0.5 + 0.5 * (0.5 + 0.5 * osc.periodic_lfo(n, 3)) * (0.5 + 0.5 * osc.periodic_lfo(n, 7, phase0=0.2)))
    ticks = L.crackle(4.0, r, 3, 4, 0.02, 3000, 11000, circular=True)
    m = mix(L.norm(hum) * 0.5, L.norm(corona) * 0.12, ticks * 0.25)
    return steady(lambda z: filt.hp(z, 80.0), m)


# ============================================================================ dive, fall, launch (legacy ids the game plays)

@asset("sfx_dive", "core", takes=2, family="dive", priority=50, cooldown_ms=150, max_voices=1)
def dive(ctx):
    r = ctx.rng
    snap = L.snap(r, 2.5, 1200, 8000, 0.4)
    down = L.air(0.6, r, 4200, 600, q=1.8, peak_db=12, lo=200, hi=12000, curve=-1.5,
                 env_pts=[(0, 0.5), (0.005, 1.0), (0.15, 0.8), (0.6, 0.0, -4)])
    whoom = L.thump(120, r, dur=0.2, drop=1.4, hp=70)
    body = mix(L.widen(L.dense(down, 1.8) * 0.6, 0.6, 161 + ctx.take), L.widen(whoom * 0.35, 0.0), L.tail(down, "room", -16.0))
    return L.finish(L.fit_transient(L.widen(snap, 0.1, 160), filt.hp(body, 70.0), "core", gain=0.5))


@asset("sfx_dive_loop", "loop", loop=True, group="loop", family="dive", priority=60, max_voices=1, import_class="loop")
def dive_loop(ctx):
    r = ctx.rng
    n = L.ns(3.0)

    def g(t, f):
        flick = 1 + 0.12 * np.sin(2 * np.pi * t * 5 / 3.0) * np.sin(2 * np.pi * t * 2 / 3.0 + 1.0)
        return stft.band(f, 250, 12000, 12, 12) * stft.tilt(f, -1.5, 2000) * (stft.resonance(f, 1800, 1.0, 6)) * flick
    rush = stft.shaped_noise(n, g, r, circular=True)
    m = steady(lambda z: filt.hp(z, 150.0), L.norm(rush))
    return L.widen_loop(m, 0.7, 165)


@asset("sfx_fall", "core", family="dive", priority=40, cooldown_ms=500, max_voices=1,
       meta=dict(waive={"time_to_peak": "falling away : the rush builds then recedes"}))
def fall(ctx):
    r = ctx.rng
    dur = 1.4
    rush = L.air(dur, r, 2600, 300, q=1.4, peak_db=10, lo=150, hi=11000, curve=-1.0,
                 env_pts=[(0, 0.6), (0.01, 0.8), (0.25, 1.0), (dur, 0.0, -4)])
    n = L.ns(dur)
    tone = osc.sine(np.geomspace(900, 160, n), n) * env.bp([(0, 0), (0.02, 1), (dur, 0, -3)], n)
    snap = L.snap(r, 3.0, 800, 6000, 0.3)
    body = mix(L.widen(L.dense(rush, 1.6) * 0.6, 0.7, 171), L.widen(tone * 0.12, 0.2, 172), L.tail(rush, "canyon", -16.0))
    return L.finish(L.fit_transient(L.widen(snap, 0.1, 170), filt.hp(body, 120.0), "core", gain=0.4, peak_ms=0))


@asset("sfx_launch", "core", takes=2, family="pad", priority=30, cooldown_ms=150, max_voices=2)
def launch(ctx):
    r = ctx.rng
    n = L.ns(0.6)
    t = np.arange(n) / L.SR
    boing = np.sin(2 * np.pi * np.cumsum(180 + 260 * (1 - np.exp(-t / 0.08)) + 25 * np.sin(2 * np.pi * 18 * t) * np.exp(-t / 0.2)) / L.SR)
    boing = boing * env.bp([(0, 0), (0.003, 1), (0.6, 0, -4)], n)
    up = L.air(0.6, r, 700, 4500, q=1.4, peak_db=12, lo=250, hi=12000, curve=-1.5, env_pts=[(0, 0.4), (0.006, 1), (0.6, 0, -4)])
    crack = filt.hp(L.nwave(4.0, 0.05), 200.0)
    thump = L.thump(100, r, dur=0.12, drop=1.7, hp=55)
    body = mix(L.widen(thump * 0.4, 0.0), L.widen(L.norm(boing) * 0.3, 0.2, 181), L.widen(L.dense(up, 1.8) * 0.55, 0.7, 182 + ctx.take),
               L.tail(up, "room", -16.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.15, 180), filt.hp(body, 55.0), "core", gain=0.5))
