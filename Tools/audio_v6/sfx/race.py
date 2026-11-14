"""Race : count (neutral tock), go (non-tonal), finish and ranks (in key, per world ; S = fanfare chord + cymbal
swell), wrong way (soft FM falling minor third), rewind, respawn, death, lose, rival win, vs, perfect start, pass,
stall, tick."""
import numpy as np

from ..registry import asset, alias
from ..dsp import osc, env, filt, modal, motion, stft
from ..dsp.util import mix, place
from . import layers as L
from ..music import instruments as I, drums as D


@asset("sfx_count", "ui", family="race", priority=20, max_voices=1, notes="played on the intro downbeats")
def count(ctx):
    r = ctx.rng
    tock = modal.modes(820.0, [1.0, 2.76, 5.40], L.ns(0.25), r, decay=0.12, amps=[1.0, 0.35, 0.1], contact_ms=0.4)
    knock = filt.bp(r.standard_normal(L.ns(0.02)), 600, 3000) * env.decay(L.ns(0.02), 0.004, attack=0.0003)
    body = mix(L.norm(tock) * 0.7, L.norm(knock) * 0.3)
    st = mix(L.widen(filt.hp(body, 200.0), 0.15, 701), L.tail(body, "room", -18.0))
    return L.finish(st)


@asset("sfx_go", "core", family="race", priority=10, max_voices=1)
def go(ctx):
    r = ctx.rng
    crack = filt.hp(L.nwave(6.0, 0.05), 150.0)
    burst = L.air(0.9, r, 600, 5000, q=1.2, peak_db=10, lo=150, hi=14000, curve=-1.5,
                  env_pts=[(0, 0.6), (0.006, 1.0), (0.9, 0.0, -4)])
    thump = L.thump(90, r, dur=0.14, drop=1.8, hp=50, drive=1.8)
    body = mix(L.widen(thump * 0.45, 0.0), L.widen(L.dense(burst, 2.0) * 0.6, 0.8, 711), L.tail(burst, "canyon", -18.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.2, 710), filt.hp(body, 50.0), "core", gain=0.6))


def _fanfare(world, r, notes_kind, dur, bright=4000.0):
    notes = L.world_chord(world, 4, notes_kind)
    x = I.brass(notes, dur * 0.6, r, bright)
    y = I.supersaw_chord([m + 12 for m in notes], dur * 0.7, r, cutoff=6000.0, attack=0.02, release=dur * 0.3)
    return mix(L.norm(x) * 0.6, L.norm(y) * 0.4)


def _cymbal_swell(r, dur):
    c = D.crash(9, dur)
    return c * env.bp([(0, 0.4), (0.02, 1.0), (dur, 0.0, -3.0)], len(c))


@asset("sfx_finish", "hero", worlds=(1, 2, 3), family="finish", priority=5, max_voices=1)
def finish(ctx):
    r = ctx.rng
    crack = filt.hp(L.nwave(8.0, 0.05), 150.0)
    sub = L.sub_drop(L.hz(L.world_note(ctx.world, 0, 2)) * 2, L.hz(L.world_note(ctx.world, 0, 1)), 0.4)
    fan = _fanfare(ctx.world, r, "rel_major", 2.4)
    cym = _cymbal_swell(r, 2.5)
    gl = L.glints(1.8, r, [m + 12 for m in L.world_chord(ctx.world, 5, "rel_major")], count=14, decay=0.45, t_spread=1.0)
    body = mix(L.widen(sub * 0.45, 0.0), L.widen(L.dense(fan, 1.6) * 0.8, 0.6, 721), L.widen(filt.hp(cym, 400) * 0.4, 0.8, 722),
               L.widen(gl * 0.15, 0.7, 723), L.tail(fan, "hall", -10.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.2, 720), filt.hp(body, 28.0), "hero", gain=0.5))


def _rank(ctx, level):
    r = ctx.rng
    w = ctx.world
    hit = mix(L.thump(85, r, dur=0.15, drop=1.8, hp=50, drive=1.8), L.snap(r, 2.0, 1500, 9000, 0.6) * 0.5)
    notes = L.world_chord(w, 4 + (level >= 2), "rel_major" if level >= 2 else "tonic9")[:3 + level]
    stack = np.zeros(L.ns(2.2))
    for i, m in enumerate(notes):
        place(stack, L.bell(L.hz(m), 1.8, r, ratio=2.0 + (level >= 2), index=1.6, decay=1.2), L.ns(0.04 * i), 0.8)
    parts = [L.widen(L.norm(stack) * (0.5 + 0.1 * level), 0.5, 731 + level), L.tail(stack, "hall", -12.0)]
    if level == 3:
        parts += [L.widen(L.dense(_fanfare(w, r, "rel_major", 2.0), 1.5) * 0.6, 0.6, 735),
                  L.widen(filt.hp(_cymbal_swell(r, 2.2), 400) * 0.35, 0.8, 736)]
    body = filt.hp(mix(*parts), 60.0)
    cat = "hero" if level == 3 else "core"
    return L.finish(L.fit_transient(L.widen(filt.hp(hit, 50.0), 0.1, 730), body, cat, gain=0.5, peak_ms=0))


for _lvl, _l in enumerate("cbas"):
    def _fn(ctx, _lvl=_lvl):
        return _rank(ctx, _lvl)
    asset("sfx_rank_%s" % _l, "hero" if _l == "s" else "core", worlds=(1, 2, 3), family="rank", priority=10, max_voices=1,
          meta=dict(waive={"time_to_peak": "rank reveal : the chord swells after the hit"}))(_fn)
alias("sfx_rank", "sfx_rank_a_w1")
for _l in "cbas":
    alias("sfx_rank_%s" % _l, "sfx_rank_%s_w1" % _l)
alias("sfx_finish", "sfx_finish_w1")


@asset("sfx_wrong_way", "ui", family="race", priority=30, cooldown_ms=800, max_voices=1)
def wrong_way(ctx):
    r = ctx.rng
    a = L.bell(L.hz(L.world_note(1, 4, 5)), 0.3, r, ratio=1.0, index=1.2, decay=0.35, detune=0.2)
    b = L.bell(L.hz(L.world_note(1, 2, 5)), 0.45, r, ratio=1.0, index=1.0, decay=0.45, detune=0.2)
    body = mix(a * 0.6, L.at(b, 0.16) * 0.6)
    st = mix(L.widen(filt.hp(body, 200.0), 0.3, 741), L.tail(body, "room", -18.0))
    return L.finish(st)


@asset("sfx_rewind", "core", family="race", priority=30, max_voices=1,
       meta=dict(waive={"time_to_peak": "tape spin-up : energy builds"}))
def rewind(ctx):
    r = ctx.rng
    n = L.ns(1.0)
    t = np.arange(n) / L.SR
    sh = L.shepard(n, 1, r, base=110.0, octaves=6, sigma=1.0, direction=-1.0)
    flutter = L.air(1.0, r, 3000, 800, q=1.5, peak_db=10, lo=300, hi=10000, env_pts=[(0, 0.5), (0.01, 1), (1.0, 0, -3)])
    flutter = flutter * (0.7 + 0.3 * np.sin(2 * np.pi * np.cumsum(np.linspace(30, 8, n)) / L.SR))
    click = L.snap(r, 1.5, 2000, 9000, 0.5)
    sh = L.norm(sh) * env.bp([(0, 0.4), (0.15, 1.0), (0.7, 0.8), (1.0, 0.0, -4)], n)
    body = filt.hp(mix(L.widen(sh * 0.5, 0.5, 751), L.widen(flutter * 0.4, 0.7, 752)), 150.0)
    return L.finish(L.fit_transient(L.widen(click, 0.1, 750), body, "core", gain=0.4, peak_ms=0))


@asset("sfx_respawn", "core", family="race", priority=30, max_voices=1,
       meta=dict(waive={"time_to_peak": "respawn chime : the arpeggio blooms after the pop"}))
def respawn(ctx):
    r = ctx.rng
    pop = L.snap(r, 1.5, 2000, 10000, 0.6)
    notes = L.world_chord(1, 5, "five")
    arp = np.zeros(L.ns(0.9))
    for i, m in enumerate(notes):
        place(arp, L.bell(L.hz(m), 0.6, r, ratio=2.0, index=1.2, decay=0.4), L.ns(0.03 * i), 0.7)
    air = L.air(0.5, r, 4000, 1500, q=1.0, peak_db=8, lo=500, hi=12000, env_pts=[(0, 0), (0.005, 1), (0.5, 0, -4)])
    body = filt.hp(mix(L.widen(L.norm(arp) * 0.55, 0.4, 761), L.widen(air * 0.3, 0.7, 762), L.tail(arp, "hall", -14.0)), 150.0)
    return L.finish(L.fit_transient(L.widen(pop, 0.1, 760), body, "core", gain=0.5))


@asset("sfx_death", "impact", family="race", priority=20, max_voices=1)
def death(ctx):
    r = ctx.rng
    crack = L.snap(r, 2.5, 1200, 9000, 0.6)
    thud = L.thump(70, r, dur=0.2, drop=2.0, hp=40, drive=2.0)
    n = L.ns(0.9)
    fall = osc.saw(np.geomspace(900, 70, n), n) * env.bp([(0, 0), (0.01, 1), (0.9, 0, -3)], n)
    fall = filt.svf(fall, np.geomspace(3000, 300, n), 1.0, "lp")
    debris = L.debris(0.6, r, 20, 600, 3000, decay=0.03)
    body = filt.hp(mix(L.widen(thud * 0.6, 0.0), L.widen(L.norm(fall) * 0.25, 0.4, 771), L.widen(debris * 0.25, 0.7, 772),
                       L.tail(mix(fall, debris), "room", -10.0)), 40.0, 3)
    return L.finish(L.fit_transient(L.widen(crack, 0.1, 770), body, "impact", gain=0.5))


def _sting(r, notes, falling=True, dur=1.6):
    n = L.ns(dur)
    t = np.arange(n) / L.SR
    bend = 2 ** ((-2.0 if falling else 0.0) / 12.0 * np.clip(t / dur, 0, 1))
    x = np.zeros(n)
    for m in notes:
        for d in (-6, 6):
            x += osc.saw(L.hz(m) * 2 ** (d / 1200) * bend, n, phase0=r.uniform())
    x = filt.svf(x, np.geomspace(3500, 500, n), 0.9, "lp") * env.bp([(0, 0), (0.01, 1), (dur, 0, -3)], n)
    return L.norm(x)


@asset("sfx_lose", "core", family="race", priority=20, max_voices=1)
def lose_sfx(ctx):
    r = ctx.rng
    hit = mix(L.thump(80, r, dur=0.15, hp=50), L.snap(r, 2.0, 1000, 6000, 0.4) * 0.4)
    st = _sting(r, L.world_chord(1, 4, "triad"))
    body = filt.hp(mix(L.widen(st * 0.5, 0.4, 781), L.tail(st, "hall", -12.0)), 60.0)
    return L.finish(L.fit_transient(L.widen(filt.hp(hit, 50.0), 0.1, 780), body, "core", gain=0.5))


@asset("sfx_rival_win", "core", family="race", priority=20, max_voices=1)
def rival_win(ctx):
    r = ctx.rng
    hit = mix(L.thump(75, r, dur=0.18, hp=45), L.snap(r, 2.0, 900, 6000, 0.4) * 0.4)
    m = L.world_note(1, 0, 4)
    st = _sting(r, [m, m + 3, m + 6], True, 1.4)  # diminished : the rival took it
    body = filt.hp(mix(L.widen(st * 0.5, 0.4, 791), L.tail(st, "hall", -12.0)), 60.0)
    return L.finish(L.fit_transient(L.widen(filt.hp(hit, 45.0), 0.1, 790), body, "core", gain=0.5))


@asset("sfx_vs", "hero", family="race", priority=10, max_voices=1)
def vs(ctx):
    r = ctx.rng
    crack = filt.hp(L.nwave(7.0, 0.05), 150.0)
    sub = L.sub_drop(80, 40, 0.4)
    clang = L.norm(modal.hit(180.0, "tube", L.ns(1.8), r, decay=1.4, damp=0.6, contact_ms=0.5))
    swoosh = L.air(0.8, r, 4000, 600, q=1.5, peak_db=10, lo=200, hi=12000, env_pts=[(0, 0.6), (0.005, 1), (0.8, 0, -4)])
    body = mix(L.widen(sub * 0.5, 0.0), L.widen(L.dense(mix(clang * 0.6, swoosh * 0.6), 2.0), 0.6, 801),
               L.tail(mix(clang, swoosh), "hall", -10.0))
    return L.finish(L.fit_transient(L.widen(crack, 0.2, 800), filt.hp(body, 30.0), "hero", gain=0.5))


@asset("sfx_perfect_start", "core", family="race", priority=15, max_voices=1)
def perfect_start(ctx):
    r = ctx.rng
    crack = filt.hp(L.nwave(4.0, 0.05), 200.0)
    burst = L.air(0.6, r, 1500, 7000, q=1.4, peak_db=10, lo=300, hi=14000, curve=-1.5,
                  env_pts=[(0, 0.5), (0.005, 1.0), (0.6, 0, -4)])
    notes = L.world_chord(1, 5, "rel_major")[:3]
    ch = np.zeros(L.ns(1.0))
    for i, m in enumerate(notes):
        place(ch, L.bell(L.hz(m), 0.9, r, 2.0, 1.4, 0.7), L.ns(0.02 * i), 0.7)
    body = filt.hp(mix(L.widen(L.dense(burst, 1.8) * 0.5, 0.7, 811), L.widen(L.norm(ch) * 0.45, 0.5, 812),
                       L.tail(ch, "hall", -12.0)), 150.0)
    return L.finish(L.fit_transient(L.widen(crack, 0.2, 810), body, "core", gain=0.5))


@asset("sfx_pass", "core", takes=2, family="race", priority=60, cooldown_ms=300, max_voices=2,
       meta=dict(waive={"time_to_peak": "a pass-by : peaks as the rival goes by"}))
def pass_by(ctx):
    r = ctx.rng
    dur = 1.2
    n = L.ns(dur)
    src = stft.shaped_noise(n, lambda t, f: stft.band(f, 150, 10000) * stft.resonance(f, 1200, 1.0, 6), r)
    fly = motion.pass_by(L.norm(src), 45.0, 3.0, 1.0 if ctx.take == 1 else -1.0, t_closest=0.45)
    fly = fly * env.bp([(0, 1.0), (dur - 0.3, 1.0), (dur, 0.0, -2.0)], len(fly))[:, None]
    tick = L.snap(r, 2.0, 1000, 6000, 0.2)
    return L.finish(L.fit_transient(L.widen(tick, 0.1, 820), filt.hp(L.dense(L.norm(fly), 1.5), 120.0), "core",
                                    gain=0.15, peak_ms=0))


@asset("sfx_stall", "core", family="race", priority=50, max_voices=1)
def stall(ctx):
    r = ctx.rng
    n = L.ns(0.7)
    t = np.arange(n) / L.SR
    sput = osc.pulse(np.geomspace(140, 45, n), n, 0.3) * (0.6 + 0.4 * np.sign(np.sin(2 * np.pi * 14 * t)))
    sput = filt.svf(sput, np.geomspace(2500, 400, n), 1.2, "lp") * env.bp([(0, 0), (0.005, 1), (0.7, 0, -3)], n)
    click = L.snap(r, 2.0, 1000, 6000, 0.4)
    body = filt.hp(mix(L.widen(L.dense(L.norm(sput), 1.8) * 0.55, 0.2, 831), L.tail(sput, "room", -10.0)), 60.0)
    return L.finish(L.fit_transient(L.widen(click, 0.1, 830), body, "core", gain=0.5))


@asset("sfx_tick", "micro", family="race", priority=60, cooldown_ms=50, max_voices=2)
def tick(ctx):
    r = ctx.rng
    ping = modal.modes(2400.0, [1.0, 2.32], L.ns(0.25), r, decay=0.12, amps=[1.0, 0.3], contact_ms=0.3)
    body = mix(L.widen(filt.hp(L.norm(ping) * 0.5, 300.0), 0.1, 841), L.tail(ping, "room", -14.0))
    return L.finish(L.fit_transient(L.widen(L.snap(r, 1.0, 3000, 12000, 0.8), 0.1, 840), body, "micro", gain=0.2))
