"""World music : per-world palettes, the 64-bar stems (base / hyper / light + the master mix for QA) and the 3-bar
intros (count-in builds whose counts land on the downbeats ; the stems start exactly 3 bars after the intro)."""
import numpy as np

from ..registry import asset
from ..config import WORLDS, BAR, BEAT, STEP16, BPM
from ..dsp import filt, nonlin
from ..dsp.util import ns, mix
from . import arrange, mixbus, drums as D, instruments as I, theory as T
from .sequencer import BARS, loop_len

PALETTES = {
    1: dict(  # F minor liquid / jump-up : FM e-piano, rolling break, warm reese, KS arps
        swing=0.555, snare_tune=205.0, talk=0.55, reese_cut=650.0, reese_res=0.35, reese_drive=2.0, stab_bright=6000.0,
        pluck_bright=0.55, lead_cut=4200.0, atmos_cut=1800.0, neuro=False,
        progression=WORLDS[1]["chords"],
        mix=dict(kick=-1.0, snare=-1.0, hats=-12.0, crash=-14.0, sub=-11.5, reese=-11.0, keys=-9.0, pad=-12.0,
                 atmos=-20.0, top=-8.0, ghost=2.0, ride=-11.0, neuro=-10.0, arps=-8.0, stabs=-8.0, lead=-16.0,
                 octave=-18.0, choir=-14.0, bells=-17.0, swell=-19.0)),
    2: dict(  # G minor (Phrygian b2) jump-up / neuro : talking reese, brass stabs, half-time switch-up
        swing=0.54, snare_tune=218.0, talk=0.9, talk_drop=0.3, reese_cut=820.0, reese_res=0.45, reese_drive=3.0,
        stab_bright=5000.0, stab_kind="brass", pluck_bright=0.62, lead_cut=4800.0, atmos_cut=1500.0, neuro=True,
        halftime_switch=True, fold=2.0, formant_rate=5.0,
        switch_riff=[(0, 0, 6), (12, 6, 2), (0, 10, 6), (1, 16, 6), (0, 22, 2), (-2, 26, 6)],
        progression=WORLDS[2]["chords"],
        mix=dict(kick=-1.0, snare=-1.0, hats=-12.0, crash=-14.0, sub=-11.5, reese=-10.0, keys=-11.0, pad=-13.0,
                 atmos=-21.0, top=-8.0, ghost=2.0, ride=-11.0, neuro=-8.0, arps=-9.0, stabs=-7.0, lead=-16.0,
                 octave=-18.0, choir=-14.0, bells=-17.0, swell=-19.0)),
    3: dict(  # E minor dark neurofunk : FM neuro bass, formant choir, cosmic shimmer, half-time doom Em-F-Em-C-B
        swing=0.53, snare_tune=188.0, talk=0.7, reese_cut=520.0, reese_res=0.5, reese_drive=3.5, bass_mode="neuro",
        neuro=True, halftime_switch=True, fold=2.8, formant_rate=3.0, stab_bright=3200.0, pluck_bright=0.42,
        lead_cut=3400.0, atmos_cut=1200.0, cosmic=True, choir_gain=1.5, bars_per_chord=1,
        progression=[(0, "m"), (0, "m"), (1, "maj"), (1, "maj"), (0, "m"), (0, "m"), (8, "maj"), (7, "maj")],
        switch_riff=[(0, 0, 8), (1, 8, 8), (0, 16, 8), (-1, 24, 8)],
        mix=dict(kick=-1.0, snare=-1.0, hats=-12.5, crash=-14.0, sub=-11.5, reese=-11.0, keys=-12.0, pad=-12.0,
                 atmos=-19.0, top=-9.0, ghost=2.0, ride=-12.0, neuro=-9.0, arps=-10.0, stabs=-9.0, lead=-16.0,
                 octave=-18.0, choir=-11.0, bells=-16.0, swell=-18.0)),
}


def render_world(world, rng):
    ch, duck, info = arrange.compose(world, PALETTES[world], rng)
    stems = mixbus.build_stems(ch, duck)
    out, mixed, minfo = mixbus.master(stems, -14.0, -16.0)
    w = "music_w%d" % world
    return {w + "_base": out["base"], w + "_hyper": out["hyper"], w + "_light": out["light"], w + "_mix": mixed}, minfo


def _world_asset(world):
    w = "music_w%d" % world
    n = loop_len()

    def fn(ctx):
        out, info = render_world(world, ctx.rng)
        return out
    asset(w, "music_full", outputs={w + "_base": "music_base", w + "_hyper": "music_layer", w + "_light": "music_layer",
                                    w + "_mix": "music_full"},
          master=False, loop=True, group="music", cost=100.0, import_class="music_stem",
          meta=dict(stems=[w + "_base", w + "_hyper", w + "_light"], mix=w + "_mix", no_install=[w + "_mix"],
                    bpm=BPM, bars=BARS, loop_start_samples=0, loop_end_samples=n,
                    sections=dict(dropA=[0, 16], switch=[16, 24], breakdown=[24, 32], build=[32, 40], dropB=[40, 56],
                                  roller=[56, 64]),
                    waive={"seam_flux": "the loop seam is the roller -> drop A downbeat : a designed spectral change"}))(fn)


# ------------------------------------------------------------------ intro (3 bars, counts on the downbeats)

def render_intro(world, rng):
    pal = PALETTES[world]
    cfg = WORLDS[world]
    root = cfg["root"]
    n = ns(3 * BAR + 1.2)
    out = np.zeros((n, 2))
    prog = pal["progression"]
    # bar 1 : chord 2, bar 2 : chord 4, bar 3 : chord 4 rising -> the drop lands on chord 1
    seq = [prog[1], prog[3], prog[3]]
    prev = None
    kick_root = T.hz(arrange._sub_root(root))
    for b, (off, kind) in enumerate(seq):
        t0 = b * BAR
        vo = T.voicing(root + 12 + off, kind, prev, 53, 79, 5)
        prev = vo
        # the count hit : kick + chord stab + short crash on the downbeat
        k = D.kick(8, b, float(kick_root), world)
        out[ns(t0):ns(t0) + len(k)] += k[:, None] * 0.9
        st = I.stab([m + 12 for m in vo], BEAT * 0.9, rng, 5000.0 + 1500 * b)
        stw = mixbus.mono_low(np.stack([st, st], axis=1)) * 0.5
        out[ns(t0):ns(t0) + len(stw)] += stw[:min(len(stw), n - ns(t0))]
        pad = I.warm_pad(vo, BAR, rng, cutoff=900 + 900 * b, attack=0.15)
        side = filt.decorrelate(pad, 31 + b, 20.0)
        p2 = np.stack([pad + side * 0.6, pad - side * 0.6], axis=1) * 0.35
        e = min(len(p2), n - ns(t0))
        out[ns(t0):ns(t0) + e] += p2[:e]
        sub_n = ns(BAR * (0.95 if b < 2 else 0.75))
        sub = np.sin(2 * np.pi * float(T.hz(arrange._sub_root(root + off))) * np.arange(sub_n) / 48000.0)
        sub *= np.minimum(1, np.arange(sub_n) / ns(0.01)) * np.minimum(1, (sub_n - np.arange(sub_n)) / ns(0.03)) * 0.2
        out[ns(t0):ns(t0) + sub_n] += sub[:, None]
    # snare roll : 8ths -> 16ths -> 32nds over the 3 bars, silence on the last beat
    t = 0.0
    while t < 3 * BAR - BEAT:
        frac = t / (3 * BAR)
        step = 2 * STEP16 if frac < 1 / 3 else (STEP16 if frac < 2 / 3 else STEP16 / 2)
        s = D.snare(D.vq(0.3 + 0.6 * frac), int(t * 1000) % 4, world, pal["snare_tune"])
        i0 = ns(t)
        e = min(len(s), n - i0)
        out[i0:i0 + e] += (s[:e] * (0.25 + 0.45 * frac))[:, None]
        t += step
    rs = I.riser(3 * BAR - BEAT, rng, 150, 6000)
    rs = filt.hp(rs, 300.0, 4)
    side = filt.decorrelate(rs, 41, 20.0)
    out[:len(rs)] += np.stack([rs + side * 0.7, rs - side * 0.7], axis=1) * 0.25
    sw = I.swell(BEAT * 2, rng)
    i0 = ns(3 * BAR) - len(sw)
    out[i0:i0 + len(sw)] += (filt.hp(sw, 400.0) * 0.35)[:, None]
    # a little room on everything (send), tails cross into the drop
    from ..dsp import space
    wet = space.send(out.mean(axis=1), "hall", -16.0)
    out = mix(out, wet)[:n]
    fade_n = ns(1.0)
    out[-fade_n:] *= np.linspace(1, 0, fade_n)[:, None] ** 2
    return out


def _intro_asset(world):
    def fn(ctx):
        return render_intro(world, ctx.rng)
    asset("music_w%d_intro" % world, "music_intro", group="music", cost=5.0, import_class="music_stinger",
          meta=dict(bpm=BPM, bars=3, sync_point_s=3 * BAR, counts_s=[0.0, BAR, 2 * BAR], go_s=3 * BAR,
                    notes="play the stems exactly at sync_point_s (PlayScheduled) ; the tail after it overlaps the drop"))(fn)


for _w in (1, 2, 3):
    _world_asset(_w)
    _intro_asset(_w)
