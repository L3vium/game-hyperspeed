"""UI kit : one key (F minor, the title key), glassy FM pings + band-limited clicks ~3-4 kHz, high-passed at 200 Hz,
<= 150 ms, dry. Transitions : reverse-reverb suck + pop, whoomp + air."""
import numpy as np

from ..registry import asset, alias
from ..dsp import env, filt, modal, space
from ..dsp.util import mix, place
from . import layers as L

KEY = 1  # F minor


def _ping(midi, dur=0.12, index=1.2, ratio=3.0):
    """Glassy FM ping (short)."""
    return L.bell(L.hz(midi), dur, None, ratio=ratio, index=index, decay=dur * 0.9, detune=0.4)


def _click(r, f=3500.0):
    """Band-limited click centred on f."""
    c = L.snap(r, 1.0, f * 0.6, f * 1.6, 0.5)
    return filt.bp(c, f * 0.7, f * 1.5)


def _ui(r, parts, category="ui", gain=0.5):
    tr = filt.hp(_click(r), 200.0)
    body = filt.hp(mix(*parts), 200.0)
    # dry and centred : decorrelating a pure ping would pan its partials at random (lopsided image)
    st = L.fit_transient(L.widen(tr, 0.0, 601), L.widen(body, 0.0, 602), category, gain=gain)
    return L.finish(st, 0.01)


@asset("ui_move", "micro", takes=2, group="ui", family="ui", priority=100, cooldown_ms=30, max_voices=2,
       notes="take 1 = up, take 2 = down")
def ui_move(ctx):
    m = L.world_note(KEY, 7 if ctx.take == 1 else 4, 5)
    return _ui(ctx.rng, [_ping(m, 0.14, 1.0) * 0.6], "micro", 0.4)


@asset("ui_select", "ui", group="ui", family="ui", priority=90, cooldown_ms=60, max_voices=2)
def ui_select(ctx):
    out = np.zeros(L.ns(0.15))
    place(out, _ping(L.world_note(KEY, 0, 5), 0.09), 0)
    place(out, _ping(L.world_note(KEY, 4, 5), 0.11), L.ns(0.035))
    return _ui(ctx.rng, [out * 0.6])


@asset("ui_back", "ui", group="ui", family="ui", priority=90, cooldown_ms=60, max_voices=2)
def ui_back(ctx):
    out = np.zeros(L.ns(0.15))
    place(out, _ping(L.world_note(KEY, 4, 5), 0.09), 0)
    place(out, _ping(L.world_note(KEY, 0, 5), 0.11), L.ns(0.035))
    return _ui(ctx.rng, [out * 0.6])


@asset("ui_locked", "ui", group="ui", family="ui", priority=90, cooldown_ms=100, max_voices=1)
def ui_locked(ctx):
    r = ctx.rng
    thunk = modal.modes(300.0, [1.0, 1.59, 2.14], L.ns(0.12), r, decay=0.08, amps=[1.0, 0.4, 0.2], contact_ms=1.5)
    m = L.world_note(KEY, 4, 5)
    dis = mix(_ping(m, 0.12, 0.8), _ping(m + 1, 0.12, 0.8))
    return _ui(r, [L.norm(thunk) * 0.6, dis * 0.25])


@asset("ui_slider", "micro", group="ui", family="ui", priority=120, cooldown_ms=25, max_voices=2)
def ui_slider(ctx):
    return _ui(ctx.rng, [_ping(L.world_note(KEY, 7, 6), 0.05, 0.6) * 0.3], "micro", 0.7)


@asset("ui_hint", "ui", group="ui", family="ui", priority=100, cooldown_ms=200, max_voices=1)
def ui_hint(ctx):
    out = np.zeros(L.ns(0.15))
    place(out, _ping(L.world_note(KEY, 2, 5), 0.1, 0.9, 2.0), 0)
    place(out, _ping(L.world_note(KEY, 4, 5), 0.12, 0.9, 2.0), L.ns(0.03))
    return _ui(ctx.rng, [out * 0.6], gain=0.25)


@asset("ui_whoosh", "ui", group="ui", family="ui", priority=100, cooldown_ms=100, max_voices=1,
       meta=dict(waive={"lead_silence": "a swell : starts soft by design"}))
def ui_whoosh(ctx):
    r = ctx.rng
    w = L.air(0.25, r, 1200, 4500, q=1.4, peak_db=10, lo=300, hi=12000, curve=-1.0,
              env_pts=[(0, 0.2), (0.01, 0.6), (0.08, 1.0), (0.25, 0.0, -4)])
    return L.finish(filt.hp(L.widen(w, 0.6, 610), 200.0))


@asset("ui_transition_in", "ui", group="ui", family="ui_transition", priority=60, max_voices=1,
       meta=dict(waive={"lead_silence": "reverse-reverb suck : starts from silence by design"}))
def ui_transition_in(ctx):
    r = ctx.rng
    notes = L.world_chord(KEY, 5, "tonic9")
    hit = mix(*[_ping(m, 0.4, 1.0) for m in notes[:3]])
    wet = space.send(hit, "hall", 0.0)[:L.ns(0.7)]
    rev = wet[::-1] * env.bp([(0, 0), (0.7, 1.0, 2.0)], L.ns(0.7))[:, None]
    suck = L.air(0.7, r, 600, 6000, q=1.2, peak_db=8, lo=300, hi=12000, env_pts=[(0, 0), (0.7, 1.0, 3.0)])
    pop = filt.hp(L.snap(r, 1.5, 1500, 9000, 0.7), 200.0)
    body = mix(L.norm(rev) * 0.6, L.widen(suck * 0.4, 0.6, 611))
    pop_t = mix(L.widen(pop * 0.8, 0.0, 612), L.tail(pop, "tight", -12.0))
    out = mix(body, L.at(pop_t, 0.7))
    return L.finish(filt.hp(out, 200.0), 0.01)


@asset("ui_transition_out", "ui", group="ui", family="ui_transition", priority=60, max_voices=1)
def ui_transition_out(ctx):
    r = ctx.rng
    whoomp = filt.bp(r.standard_normal(L.ns(0.3)), 200, 900) * env.bp([(0, 0), (0.004, 1), (0.3, 0, -4)], L.ns(0.3))
    airy = L.air(0.6, r, 5000, 900, q=1.0, peak_db=8, lo=300, hi=13000, env_pts=[(0, 0), (0.01, 1), (0.6, 0, -4)])
    pop = filt.hp(L.snap(r, 2.0, 1200, 8000, 0.5), 200.0)
    body = filt.hp(mix(L.widen(L.norm(whoomp) * 0.6, 0.2, 613), L.widen(airy * 0.45, 0.7, 614)), 200.0)
    return L.finish(L.fit_transient(L.widen(pop, 0.1, 615), body, "ui", gain=0.5))


alias("ui_move_up", "ui_move_1")
alias("ui_move_down", "ui_move_2")
alias("ui_move", "ui_move_1")
