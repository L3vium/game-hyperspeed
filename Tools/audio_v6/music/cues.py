"""Menu and race cues built on the same engine and THE leitmotif : music_title (64 bars liquid, -16 LUFS),
music_select (32 bars, -17), music_win (2-bar fanfare) + music_win_loop (triumphant motif), music_lose (2-bar
stinger) + music_lose_loop, music_ending (credits, ~2 min, the leitmotif in full)."""
import copy

import numpy as np

from ..registry import asset
from ..config import BAR, BEAT, STEP16, BPM, WORLDS
from ..dsp import filt, space
from ..dsp.util import ns, mix
from . import arrange, mixbus, drums as D, instruments as I, theory as T
from .worlds import PALETTES
from .sequencer import loop_len

MAJOR = (0, 2, 4, 5, 7, 9, 11)


def _render_piece(world, pal, rng, sections, target, fade_end=False, key_world=None):
    ch, duck, info = arrange.compose(world, pal, rng, sections, key_world)
    stems = mixbus.build_stems(ch, duck)
    out, mixed, minfo = mixbus.master(stems, target, target - 2.0, adjust_base=False)
    if fade_end:
        k = ns(2 * BAR)
        mixed = mixed.copy()
        mixed[-k:] *= np.linspace(1, 0, k)[:, None] ** 2
    return mixed


def _soft(pal, **kw):
    p = copy.deepcopy(pal)
    p.update(kw)
    return p


TITLE_SECTIONS = [("intro", 0, 8), ("dropA", 8, 24), ("breakdown", 24, 32), ("build", 32, 40), ("dropB", 40, 56),
                  ("outro", 56, 64)]
SELECT_SECTIONS = [("intro", 0, 8), ("roller", 8, 24), ("outro", 24, 32)]
ENDING_SECTIONS = [("intro", 0, 8), ("breakdown", 8, 16), ("build", 16, 24), ("dropA", 24, 40), ("breakdown", 40, 48),
                   ("build", 48, 56), ("dropB", 56, 72), ("outro", 72, 80)]


@asset("music_title", "music_title", loop=True, group="music", cost=90.0, import_class="music_stream",
       meta=dict(bpm=BPM, bars=64, loop_start_samples=0, loop_end_samples=loop_len(64), waive={"seam_flux": "the loop seam is a phrase boundary (bar 1 starts a phrase) : a designed spectral change"}))
def title(ctx):
    pal = _soft(PALETTES[1], lead_sections=("dropA", "dropB", "breakdown", "outro"))
    pal["mix"] = dict(pal["mix"], lead=-11.0, octave=-15.0, top=-12.0, kick=-2.0)
    return _render_piece(1, pal, ctx.rng, TITLE_SECTIONS, -16.0)


@asset("music_select", "music_select", loop=True, group="music", cost=50.0, import_class="music_stream",
       meta=dict(bpm=BPM, bars=32, loop_start_samples=0, loop_end_samples=loop_len(32), waive={"seam_flux": "the loop seam is a phrase boundary (bar 1 starts a phrase) : a designed spectral change"}))
def select(ctx):
    pal = _soft(PALETTES[2], lead_sections=("intro", "outro"), halftime_switch=False, stab_kind="stab")
    pal["mix"] = dict(pal["mix"], lead=-14.0, top=-12.0, neuro=-20.0, reese=-14.0)
    return _render_piece(2, pal, ctx.rng, SELECT_SECTIONS, -17.0)


WIN_PAL = _soft(PALETTES[1], transpose=3, scale=MAJOR, progression=[(0, "maj9"), (7, "add9"), (9, "m7"), (5, "maj7")],
                motif_a=T.MOTIF_A, motif_b=T.MOTIF_WIN, lead_sections=("dropA", "dropB"), lead_cut=5200.0)
WIN_PAL["mix"] = dict(WIN_PAL["mix"], lead=-10.0, octave=-14.0)


@asset("music_win_loop", "music_title", loop=True, group="music", cost=40.0, import_class="music_stream",
       meta=dict(bpm=BPM, bars=16, loop_start_samples=0, loop_end_samples=loop_len(16), waive={"seam_flux": "the loop seam is a phrase boundary (bar 1 starts a phrase) : a designed spectral change"}))
def win_loop(ctx):
    return _render_piece(1, WIN_PAL, ctx.rng, [("dropA", 0, 8), ("dropB", 8, 16)], -16.0)


LOSE_PAL = _soft(PALETTES[1], lead_sections=("breakdown", "outro"), lead_cut=2600.0,
                 progression=[(0, "m9"), (8, "maj7"), (5, "m7"), (7, "7b9")])
LOSE_PAL["mix"] = dict(LOSE_PAL["mix"], lead=-12.0, keys=-8.0)


@asset("music_lose_loop", "music_select", loop=True, group="music", cost=40.0, import_class="music_stream",
       meta=dict(bpm=BPM, bars=16, loop_start_samples=0, loop_end_samples=loop_len(16), waive={"seam_flux": "the loop seam is a phrase boundary (bar 1 starts a phrase) : a designed spectral change"}))
def lose_loop(ctx):
    return _render_piece(1, LOSE_PAL, ctx.rng, [("breakdown", 0, 8), ("outro", 8, 16)], -17.0)


@asset("music_ending", "music_title", group="music", cost=110.0, import_class="music_stream",
       meta=dict(bpm=BPM, bars=80))
def ending(ctx):
    pal = _soft(PALETTES[1], lead_sections=("breakdown", "dropA", "dropB", "outro", "intro"))
    pal["mix"] = dict(pal["mix"], lead=-11.0, octave=-15.0)
    return _render_piece(1, pal, ctx.rng, ENDING_SECTIONS, -16.0, fade_end=True)


# ------------------------------------------------------------------ 2-bar stingers

def _stinger(rng, motif, scale, root, chords, bright=True):
    n = ns(2 * BAR + 2.5)
    out = np.zeros((n, 2))
    # drums : a hit on 1, a crash, a fill into bar 2
    k = D.kick(8, 0, float(T.hz(arrange._sub_root(root, 46.0))), 1)
    out[:len(k)] += k[:, None] * 0.8
    cr = D.crash(1)
    side = filt.decorrelate(cr, 51, 20.0)
    out[:len(cr)] += np.stack([cr + side * 0.6, cr - side * 0.6], axis=1) * 0.35
    for st, v in ((12, 0.5), (13, 0.6), (14, 0.8), (15, 1.0)):
        s = D.snare(D.vq(v), st % 4, 1, 205.0)
        i0 = ns(st * STEP16)
        out[i0:i0 + len(s)] += s[:, None] * 0.45
    prev = None
    lead = np.zeros(n)
    for m, st, ln in T.motif_notes(motif, scale, root + 24):
        x = I.lead(m, ln * STEP16 * 0.95, rng, prev, cutoff=5000.0 if bright else 2600.0)
        i0 = ns(st * STEP16)
        lead[i0:i0 + len(x)] += x[:n - i0]
        prev = m
    bed = np.zeros(n)
    for b, (off, kind) in enumerate(chords):
        vo = T.voicing(root + 12 + off, kind, None, 55, 79, 5)
        pad = I.brass(vo, BAR * (0.9 if b == 0 else 1.6), rng) if bright else I.warm_pad(vo, BAR * 1.6, rng, 1400, 0.05)
        i0 = ns(b * BAR)
        bed[i0:i0 + len(pad)] += pad[:n - i0]
    sub = np.zeros(n)
    for b, (off, kind) in enumerate(chords):
        f = float(T.hz(arrange._sub_root(root + off)))
        m = ns(BAR * (1.0 if b == 0 else 1.8))
        x = np.sin(2 * np.pi * f * np.arange(m) / 48000.0) * np.minimum(1, np.arange(m) / 200) * np.exp(-np.arange(m) / (m * 0.6))
        sub[ns(b * BAR):ns(b * BAR) + m] += x[:n - ns(b * BAR)]
    lw = mixbus.mono_low(np.stack([lead, lead], axis=1))
    bw = filt.decorrelate(filt.hp(bed, 150.0), 52, 20.0)
    body = mix(lw * 0.5, np.stack([bed + bw * 0.6, bed - bw * 0.6], axis=1) * 0.5, sub[:, None] * 0.4)
    wet = space.send(mix(lead, bed * 0.5), "hall", -12.0)
    out = mix(out, body, wet)[:n]
    k = ns(1.2)
    out[-k:] *= np.linspace(1, 0, k)[:, None] ** 2
    return filt.hp(out, 28.0, 4)


@asset("music_win", "stinger", group="music", cost=5.0, import_class="music_stinger", meta=dict(bpm=BPM, bars=2))
def win(ctx):
    root = WORLDS[1]["root"] + 3
    return _stinger(ctx.rng, T.MOTIF_WIN, MAJOR, root, [(0, "maj9"), (7, "add9")], True)


@asset("music_lose", "stinger", group="music", cost=5.0, import_class="music_stinger", meta=dict(bpm=BPM, bars=2))
def lose(ctx):
    root = WORLDS[1]["root"]
    falling = [(7, 0, 4), (6, 4, 4), (4, 8, 4), (3, 12, 4), (2, 16, 6), (1, 22, 4), (0, 26, 6)]
    return _stinger(ctx.rng, falling, WORLDS[1]["scale"], root, [(8, "maj7"), (7, "7b9")], False)
