"""Arrangement of a looped piece over a section map. The world loop is 64 bars : Drop A 16 -> switch-up 8 -> light
breakdown 8 -> build 8 -> Drop B 16 -> roller 8 (back into bar 1). Cues use other maps (intro / outro / ...).
Writes every part into circular tracks and returns the mixer channels.

Stems : base = kick, snare, hats, crash, fills, sub, reese, keys / pads, atmos ; hyper = top loop, ghosts, ride,
neuro / reese top, arps, stabs ; light = lead motif, supersaw / choir octave, FM bells, swells."""
import numpy as np

from ..config import BAR, BEAT, STEP16, WORLDS, SR
from ..dsp import dyn, osc
from . import drums as D, instruments as I, theory as T
from .sequencer import Grid, Track, loop_len, SECTIONS
from .mixbus import Channel

DRUMS_ON = ("dropA", "dropB", "roller", "switch")


def _sub_root(midi, centre=62.0):
    """Bring a pitch class into the sub octave (closest to `centre` Hz : subs live in ~40-78 Hz)."""
    best = None
    for m in range(midi % 12 + 12, midi % 12 + 48, 12):
        d = abs(np.log2(T.hz(m) / centre))
        if best is None or d < best[0]:
            best = (d, m)
    return best[1]


def compose(world, pal, rng, sections=None, key_world=None):
    sections = sections or SECTIONS
    bars = sections[-1][2]
    cfg = WORLDS[key_world or world]
    root = cfg["root"] + pal.get("transpose", 0)
    scale = pal.get("scale", cfg["scale"])
    n = loop_len(bars)
    g = Grid(pal["swing"], 3.0, rng)
    trk = {k: Track(n) for k in ("kick", "snare", "fill", "hats", "crash", "sub", "reese", "keys", "pad", "atmos",
                                 "top", "ghost", "ride", "neuro", "arps", "stabs", "lead", "lead2", "octave", "choir",
                                 "bells", "swell")}
    kicks, snares = [], []
    prog = pal["progression"]
    bpc = pal.get("bars_per_chord", 2)
    voic = T.voice_lead(prog, root + 12, 53, 79, 5)
    voic_hi = T.voice_lead(prog, root + 24, 65, 91, 4)
    kick_root = T.hz(_sub_root(root, 46.0))
    cache = {}
    build_last = {z - 1 for name, a, z in sections if name == "build"}

    def section(bar):
        b = bar % bars
        for name, a, z in sections:
            if a <= b < z:
                return name, b - a, z - a
        return sections[0][0], 0, 1

    def chord_of(bar):
        i = (bar // bpc) % len(prog)
        return i, prog[i], voic[i], voic_hi[i]

    def cached(key, fn):
        if key not in cache:
            cache[key] = fn()
        return cache[key]

    # ------------------------------------------------------------------ drums
    def k(bar, step, vel):
        t = g.t(bar, step)
        trk["kick"].add(D.kick(D.vq(vel), int(rng.integers(0, 4)), float(kick_root), world), t)
        kicks.append(t)

    def s(bar, step, vel, track="snare"):
        t = g.t(bar, step)
        trk[track].add(D.snare(D.vq(vel), int(rng.integers(0, 4)), world, pal["snare_tune"]), t)
        if track == "snare":
            snares.append(t)

    def h(bar, step, vel, open_=False):
        trk["hats"].add(D.hat(D.vq(vel), int(rng.integers(0, 4)), open_, world), g.t(bar, step))

    half_switch = pal.get("halftime_switch", False)
    for bar in range(bars):
        sec, i, L = section(bar)
        last_beat_out = bar in build_last
        fill_bar = (i % 8 == 7 or i == L - 1) and sec in DRUMS_ON
        if sec in ("dropA", "dropB", "roller") or (sec == "switch" and not half_switch and pal.get("switch_style") == "drop"):
            k(bar, 0, 1.0)
            k(bar, 10, 0.92)
            if bar % 2 == 1:
                k(bar, 7, 0.55)
            if sec == "dropB" and bar % 4 == 3:
                k(bar, 14, 0.6)
            s(bar, 4, 1.0)
            if not fill_bar:
                s(bar, 12, 1.0)
            for st in range(0, 16, 2):
                h(bar, st, 0.8 if st % 4 == 2 else 0.5)
            if sec != "dropA" or bar % 2 == 1:
                for st in (3, 11) if sec != "roller" else (1, 3, 5, 9, 11, 13, 15):
                    h(bar, st, 0.3)
            if bar % 2 == 1:
                h(bar, 14, 0.6, True)
        elif sec == "switch" and half_switch:
            # half-time switch-up : kick on 1 (and the "and" of 3 every other bar), snare on 3
            k(bar, 0, 1.0)
            if bar % 2 == 1:
                k(bar, 11, 0.8)
            s(bar, 8, 1.0)
            for st in range(0, 16, 2):
                h(bar, st, 0.7 if st % 8 == 4 else 0.4)
            h(bar, 6, 0.55, True)
        elif sec == "switch":
            for st, v in ((0, 1.0), (6, 0.7), (10, 0.9)):
                k(bar, st, v)
            s(bar, 4, 1.0)
            if not fill_bar:
                s(bar, 12, 1.0)
            for st in range(16):
                h(bar, st, 0.75 if st % 4 == 2 else (0.45 if st % 2 == 0 else 0.25))
        elif sec in ("breakdown", "intro"):
            if i >= max(0, L - 4):
                for st in (0, 4, 8, 12):
                    h(bar, st, 0.3 + 0.05 * (i - L + 4))
        elif sec == "build":
            if i >= L // 2:
                for st in (0, 4, 8, 12):
                    if not (last_beat_out and st >= 12):
                        k(bar, st, 0.7 + 0.3 * (i - L // 2) / max(1, L // 2))
            frac = i / max(1, L)
            div = 4 if frac < 0.5 else (2 if frac < 0.75 else 1)
            for st in range(0, 16, div):
                if last_beat_out and st >= 12:
                    break
                vel = 0.35 + 0.6 * ((i * 16 + st) / (16.0 * L))
                s(bar, st, vel, "fill")
            for st in range(0, 16, 2):
                if not (last_beat_out and st >= 12):
                    h(bar, st, 0.3 + 0.4 * frac)
        if fill_bar:
            for st, v in ((12, 0.5), (13, 0.35), (14, 0.7), (15, 0.9)):
                s(bar, st, v, "fill")
        if i == 0 and sec in DRUMS_ON:
            trk["crash"].add(D.crash(world), g.t(bar, 0, False))
        if i == 0 and sec in ("breakdown",) and bar != 0:
            trk["crash"].add(D.crash(world + 7) * 0.6, g.t(bar, 0, False))

    # ------------------------------------------------------------------ hyper drums : top loop, ghosts, ride
    for bar in range(bars):
        sec, i, L = section(bar)
        if sec in DRUMS_ON and not (sec == "switch" and half_switch):
            pat = [("k", 0, 0.9), ("h", 2, 0.5), ("s", 4, 0.95), ("h", 6, 0.45), ("g", 7, 0.5), ("h", 8, 0.5),
                   ("g", 9, 0.4), ("k", 10, 0.8), ("h", 11, 0.35), ("s", 12, 0.95), ("h", 14, 0.5), ("g", 15, 0.45)]
            if bar % 4 == 3:
                pat += [("g", 13, 0.4), ("k", 14, 0.6)]
            for kind, st, v in pat:
                trk["top"].add(D.break_hit(kind, D.vq(v), int(rng.integers(0, 3)), world), g.t(bar, st))
            for st in ((7, 9, 15) if bar % 2 == 0 else (6, 9, 14, 15)):
                trk["ghost"].add(D.snare(D.vq(0.25), int(rng.integers(0, 4)), world + 20, pal["snare_tune"] * 1.05,
                                         0.8, 1.0), g.t(bar, st), 0.16)
            for st in (0, 4, 8, 12):
                bell = (st == 0 and bar % 2 == 0)
                trk["ride"].add(D.ride(D.vq(0.7 if st == 0 else 0.5), int(rng.integers(0, 3)), bell, world), g.t(bar, st))
        elif sec == "switch":
            for kind, st, v in (("k", 0, 0.9), ("h", 4, 0.5), ("g", 6, 0.4), ("s", 8, 0.95), ("h", 12, 0.5), ("g", 14, 0.45)):
                trk["top"].add(D.break_hit(kind, D.vq(v), int(rng.integers(0, 3)), world), g.t(bar, st))
            for st in (0, 4, 8, 12):
                trk["ride"].add(D.ride(D.vq(0.55), int(rng.integers(0, 3)), st == 0, world), g.t(bar, st))
        elif sec in ("breakdown", "outro"):
            for st in range(0, 16, 2):
                trk["ride"].add(D.ride(D.vq(0.3), int(rng.integers(0, 3)), False, world), g.t(bar, st), 0.6)

    # ------------------------------------------------------------------ bass : sub + reese / neuro
    sub_events = []
    for bar in range(0, bars, 2):
        sec, i, L = section(bar)
        if sec in ("intro", "outro") and not pal.get("bass_in_intro"):
            continue
        if sec == "switch" and half_switch:
            riff = pal.get("switch_riff", T.RIFF_LONG)
        else:
            riff = {"dropA": T.RIFF_ROLL, "dropB": T.RIFF_ROLL, "roller": T.RIFF_ROLL, "switch": T.RIFF_JUMP,
                    "breakdown": T.RIFF_LONG, "intro": T.RIFF_LONG, "outro": T.RIFF_LONG,
                    "build": T.RIFF_LONG if i < L // 2 else T.RIFF_BUILD}[sec]
        riff = pal.get("riffs", {}).get(sec, riff)
        for semi, st, ln in riff:
            nb = bar + st // 16
            off = chord_of(nb)[1][0]
            croot = _sub_root(root + off)
            t0 = g.t(bar, st, False)
            dur = ln * STEP16 * 0.95
            end_st = st + ln
            if (bar + 1) in build_last and end_st > 28:
                dur = max(0.0, (28 - st) * STEP16 * 0.95)  # drop-out on the last beat of a build
                if dur <= 0:
                    continue
            vel = 0.75 if sec in ("breakdown", "intro", "outro") else 1.0
            sub_events.append((t0, dur, croot + semi, vel))
            if sec in ("breakdown", "intro", "outro") or (sec == "build" and i < L // 2):
                continue
            mode = pal.get("bass_mode", "reese")
            if mode == "neuro" and sec in ("dropA", "dropB", "roller"):
                key = ("neuroB", croot + 12 + semi, round(dur, 3), int(rng.integers(0, 2)))
                x = cached(key, lambda: I.neuro(croot + 12 + semi, dur, rng, fold=pal.get("fold", 2.2),
                                                 formant_rate=pal.get("formant_rate", 4.0)))
                trk["reese"].add(x, t0, 1.0)
            else:
                talk = pal["talk"] if sec == "switch" else pal.get("talk_drop", 0.0)
                cut = pal["reese_cut"] * (1.4 if sec == "dropB" else (0.7 if sec == "roller" else 1.0))
                key = ("reese", croot + 12 + semi, round(dur, 3), sec, int(rng.integers(0, 3)))
                x = cached(key, lambda: I.reese(croot + 12 + semi, dur, rng, cutoff=cut, res=pal["reese_res"],
                                                 lfo_hz=0.5 + 0.3 * rng.uniform(), depth=0.6, drive=pal["reese_drive"],
                                                 talk=talk))
                trk["reese"].add(x, t0, 0.9 if semi >= 12 else 1.0)
            if sec == "switch":
                if pal.get("neuro"):
                    nx = cached(("neuro", croot + 24 + semi, round(dur, 3)),
                                lambda: I.neuro(croot + 24 + semi, dur, rng, fold=pal.get("fold", 2.2),
                                                formant_rate=pal.get("formant_rate", 4.0)))
                    trk["neuro"].add(nx, t0)
                else:
                    top = cached(("rtop", croot + 24 + semi, round(dur, 3)),
                                 lambda: I.reese(croot + 24 + semi, dur, rng, cutoff=2200, res=0.5, lfo_hz=3.0, depth=1.0,
                                                 drive=3.0, talk=0.8))
                    trk["neuro"].add(top, t0)
    sub_events.sort()
    # keep notes from overlapping (sub_line expects a monophonic line)
    clean = []
    for ev in sub_events:
        if clean and ev[0] < clean[-1][0] + clean[-1][1]:
            p = clean[-1]
            clean[-1] = (p[0], max(0.01, ev[0] - p[0] - 0.002), p[2], p[3])
        clean.append(ev)
    if clean:
        trk["sub"].buf += I.sub_line(clean, n)

    # ------------------------------------------------------------------ keys, pads, stabs, arps
    stab_fn = (lambda vh_: I.stab(vh_, STEP16 * 1.5, rng, pal["stab_bright"])) if pal.get("stab_kind") != "brass" else \
        (lambda vh_: I.brass(vh_, STEP16 * 1.6, rng))
    for c0 in range(0, bars, bpc):
        sec, i, L = section(c0)
        ci, (off, kind), vo, vh = chord_of(c0)
        span = bpc * BAR
        if sec in ("dropA", "dropB"):
            x = cached(("pad", ci, "soft"), lambda: I.warm_pad(vo, span, rng, cutoff=1200, attack=0.3))
            trk["pad"].add(x, g.t(c0, 0, False), 0.45)
        elif sec in ("breakdown", "intro", "outro"):
            x = cached(("ep_long", ci), lambda: I.epiano(vo, span - 0.1, 0.65, rng))
            trk["keys"].add(x, g.t(c0, 0, False), 1.0)
            x = cached(("pad", ci, "open"), lambda: I.warm_pad(vo, span, rng, cutoff=2200, attack=0.5))
            trk["pad"].add(x, g.t(c0, 0, False), 0.8)
        elif sec == "build":
            x = cached(("padb", ci, i), lambda: I.warm_pad(vo, span, rng, cutoff=900 + 3000 * i / max(1, L), attack=0.4))
            trk["pad"].add(x, g.t(c0, 0, False), 0.7)
    for bar in range(0, bars, 2):
        sec, i, L = section(bar)
        if sec in ("dropA", "dropB"):
            for st, ln, v in ((0, 5, 0.8), (6, 3, 0.55), (10, 6, 0.7), (16, 5, 0.75), (22, 3, 0.5), (26, 5, 0.65)):
                ci, _, vo, vh = chord_of(bar + st // 16)
                x = cached(("ep", ci, ln, round(v, 2)), lambda: I.epiano(vo, ln * STEP16, v, rng))
                trk["keys"].add(x, g.t(bar, st), 0.8)
        elif sec == "switch":
            for st in (2, 10, 18, 26) if bar % 4 == 0 else (2, 10, 14, 18, 26, 30):
                ci, _, vo, vh = chord_of(bar + st // 16)
                x = cached(("stab", ci), lambda: stab_fn(vh))
                trk["stabs"].add(x, g.t(bar, st), 0.8)
        if sec in ("dropA", "dropB") and (i // 2) % 4 == 3:
            for st in (26, 30):
                ci, _, vo, vh = chord_of(bar + 1)
                x = cached(("stab", ci), lambda: stab_fn(vh))
                trk["stabs"].add(x, g.t(bar, st), 0.7)
        # arps : KS plucks over the voicing, 16ths in drops / roller, 8ths in the breakdown
        if sec in ("dropA", "dropB", "roller", "breakdown", "intro", "outro"):
            slow = sec in ("breakdown", "intro", "outro")
            pattern = [0, 1, 2, 3, 2, 1, 3, 2] if not slow else [0, 2, 1, 3]
            stepw = 1 if not slow else 2
            for st in range(0, 32, stepw * 2 if sec != "roller" else stepw):
                ci, _, vo, vh = chord_of(bar + st // 16)
                tones = sorted(vh)
                m = tones[pattern[(st // max(1, stepw)) % len(pattern)] % len(tones)] + 12
                key = ("ks", m, int(rng.integers(0, 3)))
                x = cached(key, lambda: I.pluck(m, STEP16 * 1.6, rng, bright=pal["pluck_bright"], decay=0.7))
                trk["arps"].add(x, g.t(bar, st), 0.55 + 0.25 * ((st % 4) == 0))

    # ------------------------------------------------------------------ light : lead motif, octave pad, choir, bells, swells
    lead_secs = pal.get("lead_sections", ("dropA", "dropB", "breakdown"))
    for bar in range(0, bars, 2):
        sec, i, L = section(bar)
        if sec in lead_secs:
            phrase = pal.get("motif_b", T.MOTIF_B) if (bar // 2) % 2 == 1 else pal.get("motif_a", T.MOTIF_A)
            octave_up = 12 if (sec == "dropB" and i >= L // 2) else 0
            notes = T.motif_notes(phrase, scale, root + 24 + octave_up)
            prev = None
            slow = sec in ("breakdown", "intro", "outro")
            for m, st, ln in notes:
                dur = ln * STEP16 * (1.6 if slow else 0.95)
                key = ("lead", m, ln, slow, prev)
                x = cached(key, lambda: I.lead(m, dur, rng, prev, cutoff=pal["lead_cut"]))
                trk["lead"].add(x, g.t(bar, st, False), 0.9)
                if sec == "dropB":
                    d0 = [d for d, s_, l_ in phrase if s_ == st][0]
                    m2 = T.degree(scale, root + 24 + octave_up, d0 - 2)
                    x2 = cached(("lead2", m2, ln), lambda: I.lead(m2, dur, rng, None, cutoff=pal["lead_cut"] * 0.8))
                    trk["lead2"].add(x2, g.t(bar, st, False), 0.5)
                prev = m
    for c0 in range(0, bars, bpc):
        sec, i, L = section(c0)
        ci, (off, kind), vo, vh = chord_of(c0)
        span = bpc * BAR
        if sec in ("dropA", "dropB"):
            x = cached(("oct", ci), lambda: I.supersaw_chord([m + 12 for m in vh], span, rng, cutoff=4500, attack=0.08))
            trk["octave"].add(x, g.t(c0, 0, False), 0.6)
        if sec in ("breakdown", "build", "switch", "intro", "outro"):
            vowel = "a" if sec != "switch" else "o"
            x = cached(("choir", ci, vowel), lambda: I.choir(vh, span, rng, vowel))
            trk["choir"].add(x, g.t(c0, 0, False), (0.8 if sec != "switch" else 0.5) * pal.get("choir_gain", 1.0))
    for bar in range(0, bars, 2):
        sec, i, L = section(bar)
        if sec in ("switch", "roller", "breakdown", "intro", "outro"):
            for j, st in enumerate((0, 6, 12, 16, 22, 28)):
                ci, _, vo, vh = chord_of(bar + st // 16)
                tones = sorted(vh)
                m = tones[j % len(tones)] + 12
                x = cached(("bell", m), lambda: I.fm_bell(m, 0.4, rng, 3.5, 1.6, 1.2))
                trk["bells"].add(x, g.t(bar, st), 0.5 if sec not in ("breakdown", "intro", "outro") else 0.7)
    for name, a, z in sections:
        if a == 0:
            continue  # nothing crosses the loop seam
        if name in ("switch", "breakdown", "dropB", "roller", "dropA"):
            sw = cached(("swell", 2), lambda: I.swell(2 * BEAT * 2, rng))
            trk["swell"].add(sw, a * BAR - len(sw) / SR)
        if name == "build":
            rs_len = (z - a) / 2.0 * BAR - BEAT
            rs = cached(("riser", z - a), lambda: I.riser(rs_len, rng, 150, 5000))
            trk["swell"].add(rs, (a + (z - a) / 2.0) * BAR, 0.6)

    # atmos : periodic bed, louder in the quiet sections
    at = I.atmos(n, rng, [root + 12, root + 19], cutoff=pal["atmos_cut"])
    tt = np.arange(n) / SR / BAR
    quiet = np.zeros(n, dtype=bool)
    for name, a, z in sections:
        if name in ("breakdown", "build", "intro", "outro"):
            quiet |= (tt >= a) & (tt < z)
    trk["atmos"].buf += at * np.where(quiet, 1.0, 0.45)

    # ------------------------------------------------------------------ duck curve (analytic, kick + snare)
    dk = dyn.duck_curve_circular(n, kicks, depth_db=-6.0, attack=0.002, hold=0.01, release=0.11) if kicks else np.ones(n)
    ds = dyn.duck_curve_circular(n, snares, depth_db=-3.0, attack=0.002, hold=0.01, release=0.09) if snares else np.ones(n)
    duck = np.minimum(dk, ds)

    # ------------------------------------------------------------------ channels
    # tremolo autopan on the keys (mono-compatible : L + R constant)
    lfo = osc.periodic_lfo(n, bars * 2)
    keys_st = np.stack([trk["keys"].buf * (1 + 0.2 * lfo), trk["keys"].buf * (1 - 0.2 * lfo)], axis=1)
    mx = pal["mix"]
    cosmic = pal.get("cosmic", False)
    big = "cosmic" if cosmic else "hall"
    ch = [
        Channel("kick", "base", trk["kick"].buf, drum=True, gain_db=mx["kick"]),
        Channel("snare", "base", trk["snare"].buf, drum=True, hp=100, gain_db=mx["snare"], sends={"plate": -16}),
        Channel("fill", "base", trk["fill"].buf, drum=True, hp=120, gain_db=mx["snare"] - 3, sends={"plate": -14}),
        Channel("hats", "base", trk["hats"].buf, drum=True, hp=500, pan=0.25, width=0.2, gain_db=mx["hats"], seed=11),
        Channel("crash", "base", trk["crash"].buf, drum=True, hp=400, width=0.6, gain_db=mx["crash"], seed=12),
        Channel("sub", "base", trk["sub"].buf, lp=140, gain_db=mx["sub"], duck=8.0),
        Channel("reese", "base", trk["reese"].buf, hp=110, width=0.35, gain_db=mx["reese"], duck=4.0, seed=13),
        Channel("keys", "base", keys_st, hp=150, gain_db=mx["keys"], duck=3.0, sends={"hall": -14}),
        Channel("pad", "base", trk["pad"].buf, hp=180, width=0.7, gain_db=mx["pad"], duck=4.0, sends={"hall": -12}, seed=14),
        Channel("atmos", "base", trk["atmos"].buf, hp=200, width=0.8, gain_db=mx["atmos"], duck=2.0, seed=15),
        Channel("top", "hyper", trk["top"].buf, drum=True, hp=400, pan=-0.1, width=0.25, gain_db=mx["top"], seed=16),
        Channel("ghost", "hyper", trk["ghost"].buf, drum=True, hp=200, pan=0.1, gain_db=mx["ghost"]),
        Channel("ride", "hyper", trk["ride"].buf, drum=True, hp=500, pan=-0.35, gain_db=mx["ride"]),
        Channel("neuro", "hyper", trk["neuro"].buf, hp=250, width=0.4, gain_db=mx["neuro"], duck=4.0, seed=17),
        Channel("arps", "hyper", trk["arps"].buf, hp=250, pan=0.3, width=0.3, gain_db=mx["arps"], duck=3.0,
                sends={"pingpong": -10, "hall": -18}, seed=18),
        Channel("stabs", "hyper", trk["stabs"].buf, hp=200, width=0.6, gain_db=mx["stabs"], duck=3.0,
                sends={"plate": -14}, seed=19),
        Channel("lead", "light", trk["lead"].buf, hp=200, gain_db=mx["lead"], duck=2.0,
                sends={big: -12, "pingpong": -14}),
        Channel("lead2", "light", trk["lead2"].buf, hp=200, pan=-0.2, gain_db=mx["lead"] - 4, duck=2.0, sends={big: -12}),
        Channel("octave", "light", trk["octave"].buf, hp=250, width=0.8, gain_db=mx["octave"], duck=4.0,
                sends={big: -14}, seed=20),
        Channel("choir", "light", trk["choir"].buf, hp=200, width=0.7, gain_db=mx["choir"], duck=3.0,
                sends={big: -10}, seed=21),
        Channel("bells", "light", trk["bells"].buf, hp=300, pan=0.2, width=0.4, gain_db=mx["bells"], duck=2.0,
                sends={big: -12, "pingpong": -12}, seed=22),
        Channel("swell", "light", trk["swell"].buf, hp=300, width=0.9, gain_db=mx["swell"], seed=23),
    ]
    ch = [c for c in ch if np.any(c.audio)]
    return ch, duck, dict(kicks=kicks, snares=snares, bars=bars)
