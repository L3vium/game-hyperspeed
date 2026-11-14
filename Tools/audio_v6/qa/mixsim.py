"""Mix-sim demo reels : what a run of the game should sound like with the planned bus gains and ducking.
Intro (3 bars) -> stems automated base -> boost -> hyper -> light -> back, with scripted SFX (steps, jumps, lands,
dash, rings, pad, hyper, light, rope, wind layers by speed). Measures the game-mix loudness and the SFX-over-music
margins. Writes _build/demo/world_N.wav and demo.json for the report."""
import json
import os

import numpy as np

from ..config import SR, BAR, BEAT, DEMO_DIR, WAV_DIR, BUS_GAIN_DB
from ..dsp import loudness, dyn
from ..dsp.util import undb, to_stereo
from ..render import read_wav


def _load(name):
    p = os.path.join(WAV_DIR, name + ".wav")
    if not os.path.exists(p):
        return None
    return to_stereo(read_wav(p)[0])


def _ramp(n, points, rate_up=2.5, rate_down=0.8):
    """Gain automation like the game's MoveTowards : targets [(t, value)], linear slew limited per second."""
    tgt = np.zeros(n)
    pts = sorted(points)
    for i, (t, v) in enumerate(pts):
        i0 = int(t * SR)
        i1 = int(pts[i + 1][0] * SR) if i + 1 < len(pts) else n
        tgt[i0:i1] = v
    out = np.empty(n)
    cur = tgt[0]
    hop = 480
    up, down = rate_up * hop / SR, rate_down * hop / SR
    for i in range(0, n, hop):
        g = tgt[i]
        cur = min(g, cur + up) if g > cur else max(g, cur - down)
        out[i:i + hop] = cur
    return out


def build(world=1, seconds=80.0):
    w = "music_w%d" % world
    intro = _load(w + "_intro")
    base, hyper, light = _load(w + "_base"), _load(w + "_hyper"), _load(w + "_light")
    if base is None:
        return None
    sync = 3 * BAR if intro is not None else 0.0
    n = int((sync + seconds) * SR)
    music = np.zeros((n, 2))
    sfx = np.zeros((n, 2))
    events = []
    if intro is not None:
        k = min(len(intro), n)
        music[:k] += intro[:k]
    s0 = int(sync * SR)
    L = n - s0

    def loop(x):
        reps = int(np.ceil(L / len(x)))
        return np.tile(x, (reps, 1))[:L]
    # speed script (seconds after the GO) : base -> boost (pad) -> hyper -> light -> hyper out -> base
    T_PAD, T_HYPER, T_LIGHT, T_OUT, T_ROPE = 14.0, 14.3, 38.0, 58.0, 66.0
    gb = _ramp(L, [(0, 1.0)])
    gh = _ramp(L, [(0, 0.0), (T_HYPER, 1.0), (T_OUT, 0.0)])
    gl = _ramp(L, [(0, 0.0), (T_LIGHT, 1.0), (T_OUT, 0.0)])
    music[s0:] += loop(base) * gb[:, None]
    if hyper is not None:
        music[s0:] += loop(hyper) * gh[:, None]
    if light is not None:
        music[s0:] += loop(light) * gl[:, None]
    music *= undb(BUS_GAIN_DB["music"])

    def put(name, t, gain_db=0.0, cat="sfx"):
        x = _load(name)
        if x is None:
            return
        i = int((sync + t) * SR)
        if i >= n:
            return
        k = min(len(x), n - i)
        sfx[i:i + k] += x[:k] * undb(gain_db + BUS_GAIN_DB["sfx"])
        events.append((name, sync + t, k / SR, cat))

    def put_loop(name, t0, t1, gain_db=0.0):
        x = _load(name)
        if x is None:
            return
        i0, i1 = int((sync + t0) * SR), min(n, int((sync + t1) * SR))
        reps = int(np.ceil((i1 - i0) / len(x))) + 1
        y = np.tile(x, (reps, 1))[:i1 - i0]
        fade = min(len(y) // 4, int(0.25 * SR))
        e = np.ones(len(y))
        e[:fade] = np.linspace(0, 1, fade)
        e[-fade:] = np.linspace(1, 0, fade)
        sfx[i0:i1] += y * e[:, None] * undb(gain_db + BUS_GAIN_DB["loops"])
    rng = np.random.default_rng(world)
    # the count-in : the game plays sfx_count on the intro downbeats, sfx_go on the drop
    for b in range(3):
        put("sfx_count", -sync + b * BAR, -2.0, "ui")
    put("sfx_go", 0.0, 0.0, "race")
    # running : steps at sprint cadence, surfaces change
    cadence = 1.0 / 7.0
    t = 0.3
    k = 0
    surfaces = ["grass", "grass", "stone", "rock", "energy", "sand"]
    while t < seconds - 1:
        airborne = any(a <= t < b for a, b in ((5.0, 5.7), (9.0, 9.9), (22.0, 22.8), (30.0, 31.2), (T_ROPE, T_ROPE + 4.5)))
        if not airborne:
            sf = surfaces[int(t / 12) % len(surfaces)]
            tier = "sprint" if t > T_HYPER else "run"
            put("sfx_step_%s_%s_%d" % (sf, tier, 1 + k % 6), t, -7.0, "step")  # the game plays steps at 0.45
            k += 1
        t += cadence * (0.8 if t > T_HYPER else 1.0)
    for tj, tl, take in ((5.0, 5.7, 1), (9.0, 9.9, 2), (22.0, 22.8, 3), (30.0, 31.2, 4)):
        put("sfx_jump_%d" % take, tj, 0.0, "core")
        if take == 4:
            put("sfx_doublejump_1", tj + 0.35, 0.0, "core")
            put("sfx_flip_2", tj + 0.4, 0.0, "core")
        put("sfx_land_%d" % take if take < 4 else "sfx_land_heavy_1", tl, 0.0, "core")
    for i, td in enumerate((7.5, 19.0, 26.0, 44.0, 51.0)):
        put("sfx_dash_%d" % (1 + i % 4), td, 0.0, "core")
    for i, tr in enumerate((11.0, 11.6, 12.2, 33.0, 33.6, 34.2)):
        put("sfx_ring_w%d_%d" % (world, 1 + i % 3), tr, 0.0, "core")
    put("sfx_pad_1", T_PAD, 0.0, "hero")
    put("sfx_hyper_w%d" % world, T_HYPER, 0.0, "hero")
    put("sfx_light_w%d" % world, T_LIGHT, 0.0, "hero")
    put_loop("sfx_light_loop_w%d" % world, T_LIGHT + 1.0, T_OUT, -6.0)
    put("sfx_hyper_out_w%d" % world, T_OUT, 0.0, "core")
    put("sfx_boost_small_1", 47.0, 0.0, "core")
    put("sfx_grind_start_1", T_ROPE, 0.0, "core")
    put_loop("sfx_grind_loop", T_ROPE + 0.2, T_ROPE + 4.3, 0.0)
    put("sfx_rope_exit_1", T_ROPE + 4.4, 0.0, "core")
    put("sfx_land_perfect_w%d" % world, T_ROPE + 5.2, 0.0, "core")
    # wind layers by speed
    put_loop("sfx_wind_low", 0.0, seconds, -4.0)
    put_loop("sfx_wind_mid", T_HYPER - 0.5, T_OUT + 1.0, -5.0)
    put_loop("sfx_wind_high", T_LIGHT - 0.5, T_OUT, -6.0)
    # planned ducking : hero events pull the music bus 3 dB for half a second
    hero_t = [t for (nm, t, d, c) in events if c == "hero"]
    duck = dyn.duck_curve(n, hero_t, depth_db=-3.0, attack=0.01, hold=0.3, release=0.4)
    game = music * duck[:, None] + sfx
    out, gr = dyn.limiter(game, -1.0, release_ms=80.0)
    # measurements
    li = loudness.integrated(out)
    lm = loudness.integrated(music)
    ls = loudness.integrated(sfx)
    margins = {}
    mom_music = loudness.momentary(music * duck[:, None], SR, 0.01)
    for nm, t, d, c in events:
        i0 = int(t * SR)
        seg = sfx[i0:i0 + int(min(d, 0.4) * SR) + 1]
        if len(seg) < 10:
            continue
        m_sfx = loudness.momentary_max(seg)
        j = min(len(mom_music) - 1, max(0, int(t / 0.01)))
        margins.setdefault(c, []).append(m_sfx - mom_music[j])
    os.makedirs(DEMO_DIR, exist_ok=True)
    path = os.path.join(DEMO_DIR, "world_%d.wav" % world)
    from ..render import write_wav
    write_wav(path, out, 1234 + world)
    summ = ["game mix : %.1f LUFS-I, TP %.1f dBTP, safety limiter GR %.1f dB (the game has no master limiter : keep it < 1)" % (
        li, loudness.true_peak(out), gr),
        "music bus alone : %.1f LUFS-I (bus %+.0f dB), sfx bus alone : %.1f LUFS-I" % (lm, BUS_GAIN_DB["music"], ls),
        "SFX over music (LUFS-M of the event - music momentary at that time), median per class :"]
    for c, v in sorted(margins.items()):
        summ.append("  %-6s %+5.1f dB (min %+5.1f, n=%d)" % (c, float(np.median(v)), float(np.min(v)), len(v)))
    summ.append("timeline (s after GO) : pad+hyper %.1f, light %.1f, hyper out %.1f, rope %.1f" % (T_PAD, T_LIGHT, T_OUT, T_ROPE))
    return dict(name="World %d" % world, file="world_%d.wav" % world, summary="\n".join(summ),
                lufs_game=li, limiter_gr=gr, margins={c: float(np.median(v)) for c, v in margins.items()})


def build_all():
    out = []
    for w in (1, 2, 3):
        r = build(w)
        if r:
            out.append(r)
            print("demo world %d : %s" % (w, r["summary"].splitlines()[0]))
    if out:
        with open(os.path.join(DEMO_DIR, "demo.json"), "w", encoding="utf-8") as fh:
            json.dump(out, fh, indent=1)
    return out
