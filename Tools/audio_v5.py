"""Hyper Speed sound design v5 (2026-11-11 rework) : stereo, layered, loudness matched, with variations, and a new
soundtrack : drum & bass per world in three synchronised stems (base, hyper, light) the game mixes by speed state.

    python Tools/audio_v5.py            everything
    python Tools/audio_v5.py sfx        effects only
    python Tools/audio_v5.py music      music only

Built on the primitives of generate_audio.py (same folder). Deterministic : re-run to regenerate.
"""
import os
import sys
import numpy as np
from scipy import signal
from scipy.io import wavfile

sys.path.insert(0, os.path.dirname(__file__))
import generate_audio as g  # noqa : E402
from generate_audio import (SR, t_axis, noise, sine_sweep, saw, square, env_exp, env_adsr, lowpass, highpass, bandpass,  # noqa : E402
                            sweep_filter, drive, mix, pad, at, fade, loopable, whoosh, chime, reverb, sub, sparkle, riser,
                            click, body, crackle, note_freq)

OUT = os.environ.get("HS_AUDIO_OUT", g.OUT)
rng = np.random.default_rng(2026_11_11)


# ============================================================================= helpers

def stereo(x, width=0.35, delay_ms=9.0, tilt=0.15):
    """Mono to a wide stereo pair : a short Haas delay and a slightly brighter / darker side."""
    d = int(SR * delay_ms / 1000.0)
    left = x.copy()
    right = np.concatenate([np.zeros(d), x])[:len(x)]
    bright = highpass(x, 2500) * tilt
    left = left + bright
    right = right - bright
    mid = (left + right) * 0.5
    side = (left - right) * 0.5 * (width / 0.5)
    return mid + side, mid - side


def pan(x, p):
    """Constant power pan, p in -1..1."""
    a = (p + 1) * np.pi / 4
    return x * np.cos(a), x * np.sin(a)


def write_st(name, left, right, rms_db=-14.0, peak=0.97):
    left = np.asarray(left, dtype=np.float64)
    right = np.asarray(right, dtype=np.float64)
    n = max(len(left), len(right))
    left = np.pad(left, (0, n - len(left)))
    right = np.pad(right, (0, n - len(right)))
    rms = np.sqrt(np.mean((left * left + right * right) * 0.5)) + 1e-12
    k = (10 ** (rms_db / 20.0)) / rms
    data = np.stack([np.tanh(left * k / peak) * peak, np.tanh(right * k / peak) * peak], axis=1)
    os.makedirs(OUT, exist_ok=True)
    wavfile.write(os.path.join(OUT, name + ".wav"), SR, (data * 32767).astype(np.int16))
    print("wrote", name, round(n / SR, 2), "s", rms_db, "dB")


def sfx(name, x, rms_db=-13.0, width=0.35, verb=None):
    """One effect : optional reverb (seconds, decay, wet), widened, written loudness matched."""
    if verb is not None:
        x = reverb(x, verb[0], verb[1], verb[2])
    x = fade(x)
    left, right = stereo(x, width)
    write_st(name, left, right, rms_db)


def pitch(x, ratio):
    """Resample : ratio > 1 higher and shorter."""
    n = max(8, int(len(x) / ratio))
    return signal.resample(x, n)


def boom(depth=55.0, length=1.2, crack_amount=1.4):
    """A sonic boom : a hard crack, a falling sub and a rolling air tail."""
    crack = highpass(noise(0.05), 1800) * env_exp(0.05, 0.008) * crack_amount
    return mix(crack, body(depth * 2.4, depth * 0.45, length, length * 0.3, 4) * 1.4,
               at(lowpass(noise(length), 900) * env_exp(length, length * 0.3) * 0.9, 0.01),
               at(bandpass(noise(0.6), 1500, 6000) * env_exp(0.6, 0.12) * 0.5, 0.0))


def zap(length=0.3, density=260):
    return mix(click(0.01, 3000), crackle(length, density) * 1.4, sine_sweep(2600, 500, length * 0.8, 0.4) * env_exp(length * 0.8, length * 0.2) * 0.35)


def metal(length=0.9, base=210.0):
    partials = [base, base * 2.76, base * 5.4, base * 8.93, base * 13.3]
    return chime(partials, length, length * 0.25)


# ============================================================================= effects

def make_sfx():
    # ---------------------------------------------------------------- feet : four variations, by weight
    for i in range(4):
        r = 0.9 + 0.08 * i
        foot = mix(lowpass(noise(0.06), 2000 * r) * env_exp(0.06, 0.011) * 0.75, sub(160 * r, 70 * r, 0.08, 0.022) * 0.9,
                   click(0.008, 3500 + 400 * i) * 0.3, bandpass(noise(0.05), 3000, 7000) * env_exp(0.05, 0.006) * 0.15)
        sfx("sfx_step_%d" % (i + 1), foot, -17, 0.2)
    # ---------------------------------------------------------------- jumps and flips
    for i in range(3):
        r = 1.0 + 0.07 * i
        j = mix(click(0.015, 3000) * 0.7, body(175 * r, 80 * r, 0.15, 0.035, 2.5) * 0.9, whoosh(0.34, 700 * r, 5600 * r) * 1.1,
                at(sine_sweep(400 * r, 820 * r, 0.16, 0.6) * env_exp(0.16, 0.05) * 0.18, 0.01))
        sfx("sfx_jump_%d" % (i + 1), j, -13, 0.3, (0.6, 0.18, 0.12))
    sfx("sfx_jump", mix(click(0.015, 3000) * 0.7, body(175, 80, 0.15, 0.035, 2.5) * 0.9, whoosh(0.34, 700, 5600) * 1.1), -13, 0.3, (0.6, 0.18, 0.12))
    dj = mix(click(0.012, 4000) * 0.6, whoosh(0.4, 1400, 9000) * 1.3, at(sine_sweep(620, 1650, 0.22, 0.6) * env_exp(0.22, 0.07) * 0.28, 0.01),
             sparkle(0.35, 14, 4000, 11000) * 0.5, body(230, 110, 0.12, 0.03, 2) * 0.5)
    sfx("sfx_doublejump", dj, -12.5, 0.45, (0.9, 0.25, 0.2))
    for i in range(3):
        # A flip : air ripped in a quick spiral, faster for more turns.
        rate = 8 + i * 5
        tt = t_axis(0.55)
        spiral = whoosh(0.55, 600 + 300 * i, 7000) * (0.65 + 0.35 * np.sin(2 * np.pi * rate * tt))
        sfx("sfx_flip_%d" % (i + 1), mix(spiral * 1.2, sparkle(0.4, 6, 5000, 11000) * 0.25), -14, 0.6, (0.6, 0.2, 0.15))
    # ---------------------------------------------------------------- landings
    for i in range(3):
        r = 0.92 + 0.06 * i
        land = mix(body(135 * r, 45, 0.22, 0.06, 2.5) * 1.1, lowpass(noise(0.14), 1600) * env_exp(0.14, 0.025) * 0.7,
                   lowpass(noise(0.05), 2200) * env_exp(0.05, 0.01) * 0.5)
        sfx("sfx_land_%d" % (i + 1), land, -14, 0.25)
    heavy = mix(body(110, 28, 0.6, 0.14, 4) * 1.4, lowpass(noise(0.5), 1300) * env_exp(0.5, 0.09), click(0.03, 1500) * 0.9,
                at(lowpass(noise(0.9), 500) * env_exp(0.9, 0.25) * 0.5, 0.03), at(crackle(0.5, 80, 1500, 5000) * 0.5, 0.02))
    sfx("sfx_land_heavy", heavy, -10.5, 0.4, (1.4, 0.4, 0.22))
    # Dive landing : a crater and a kick.
    dl = mix(heavy * 0.8, at(boom(48, 1.0, 1.0) * 0.8, 0.02), at(whoosh(0.7, 300, 9000) * 0.9, 0.05), at(sparkle(0.6, 20, 3000, 10000) * 0.4, 0.05))
    sfx("sfx_land_perfect", dl, -10, 0.55, (1.6, 0.5, 0.28))
    # ---------------------------------------------------------------- dash : a ripped air sheet with a sub punch
    tt = t_axis(0.5)
    swirl = whoosh(0.5, 9000, 450) * (1 + 0.4 * np.sin(2 * np.pi * 18 * tt))
    for i in range(3):
        r = 1.0 + 0.05 * (i - 1)
        dash = mix(click(0.02, 2000) * 1.3, body(125 * r, 38, 0.35, 0.08, 3.5) * 1.3, pitch(swirl, r) * 1.4,
                   at(sparkle(0.3, 10, 5000, 12000) * 0.35, 0.02), highpass(noise(0.08), 3000) * env_exp(0.08, 0.015) * 0.8)
        sfx("sfx_dash_%d" % (i + 1), dash, -10.5, 0.55, (0.9, 0.24, 0.18))
    sfx("sfx_dash", mix(click(0.02, 2000) * 1.3, body(125, 38, 0.35, 0.08, 3.5) * 1.3, swirl * 1.4), -10.5, 0.55, (0.9, 0.24, 0.18))
    # ---------------------------------------------------------------- pads : a supersonic bang with a sub drop and a rising tail
    padboom = mix(boom(52, 1.4, 1.8) * 1.3, at(whoosh(1.0, 300, 11000) * 1.2, 0.0), at(sine_sweep(220, 1600, 0.6, 0.5) * env_exp(0.6, 0.22) * 0.3, 0.03),
                  at(sparkle(0.8, 24, 3500, 12000) * 0.5, 0.05), at(sub(90, 30, 1.2, 0.5) * 1.2, 0.0))
    sfx("sfx_pad", padboom, -9, 0.6, (1.8, 0.55, 0.3))
    sfx("sfx_boost", padboom * 0.9, -10, 0.6, (1.6, 0.5, 0.28))
    small = mix(click(0.015, 2500), body(140, 60, 0.25, 0.06, 3) * 0.9, whoosh(0.45, 600, 8000) * 1.1)
    sfx("sfx_boost_small", small, -12.5, 0.45, (0.8, 0.22, 0.18))
    ring = mix(chime([1046, 1568, 2093, 3136], 1.0, 0.35) * 0.7, whoosh(0.6, 800, 10000) * 0.9, sparkle(0.8, 30, 4000, 13000) * 0.6,
               body(160, 80, 0.2, 0.05) * 0.5)
    sfx("sfx_ring", ring, -11.5, 0.7, (1.6, 0.5, 0.3))
    # ---------------------------------------------------------------- impact frame and freeze frame
    imp = mix(highpass(noise(0.03), 2500) * env_exp(0.03, 0.006) * 2.0, body(90, 30, 0.7, 0.18, 5) * 1.5, at(riser(0.25, 9000, 400)[::-1] * 0.6, 0.0),
              at(lowpass(noise(0.8), 600) * env_exp(0.8, 0.3) * 0.6, 0.02))
    sfx("sfx_impact", imp, -9.5, 0.6, (1.2, 0.4, 0.25))
    freeze = mix(body(70, 40, 0.25, 0.08, 6) * 1.5, click(0.02, 1200) * 0.8)
    sfx("sfx_freeze", freeze, -12, 0.2)
    # ---------------------------------------------------------------- breaks, by material
    glass = mix(click(0.02, 3000) * 1.4, sparkle(0.6, 60, 3000, 13000) * 1.0, highpass(noise(0.5), 4000) * env_exp(0.5, 0.09) * 0.7,
                body(150, 50, 0.22, 0.05) * 0.7)
    sfx("sfx_break_glass", glass, -11, 0.7, (1.2, 0.35, 0.28))
    sfx("sfx_shatter", glass, -11.5, 0.7, (1.2, 0.35, 0.28))
    wood = mix(body(180, 70, 0.3, 0.06, 3) * 1.1, bandpass(noise(0.25), 400, 3000) * env_exp(0.25, 0.04) * 1.2,
               at(bandpass(noise(0.2), 600, 2500) * env_exp(0.2, 0.03) * 0.7, 0.05), click(0.02, 1500))
    sfx("sfx_break_crate", wood, -11, 0.5, (0.9, 0.25, 0.2))
    ice = mix(glass * 0.7, chime([2200, 3300, 4700, 6100], 0.9, 0.25) * 0.4, at(crackle(0.4, 120, 3000, 9000) * 0.7, 0.02))
    sfx("sfx_break_ice", ice, -11, 0.7, (1.4, 0.4, 0.3))
    door = mix(metal(1.0, 140) * 0.8, body(100, 40, 0.5, 0.12, 3) * 1.2, highpass(noise(0.1), 2000) * env_exp(0.1, 0.02))
    sfx("sfx_break_door", door, -10.5, 0.55, (1.2, 0.4, 0.25))
    rock = mix(body(100, 32, 0.55, 0.12, 3.5) * 1.3, lowpass(noise(0.5), 1400) * env_exp(0.5, 0.08) * 1.0, click(0.03, 1200) * 0.8,
               at(lowpass(noise(0.6), 3000) * env_exp(0.6, 0.06) * 0.4, 0.05))
    sfx("sfx_break_rock", rock, -10.5, 0.55, (1.2, 0.35, 0.22))
    sfx("sfx_rock", rock, -11.5, 0.5, (1.0, 0.3, 0.2))
    crack = highpass(noise(0.06), 2500) * env_exp(0.06, 0.012)
    hit = drive(mix(sub(115, 32, 0.55, 0.13) * 1.5, crack * 1.8, lowpass(noise(0.35), 1800) * env_exp(0.35, 0.05)), 3)
    sfx("sfx_punch_hit", hit, -10, 0.5, (1.2, 0.3, 0.3))
    sfx("sfx_crash", drive(mix(sub(130, 35, 0.5, 0.11) * 1.3, lowpass(noise(0.45), 1500) * env_exp(0.45, 0.08), crack, metal(0.4, 300) * 0.3), 2.2), -10.5, 0.5, (1.0, 0.3, 0.2))
    clang = mix(metal(1.3, 180) * 0.9, body(90, 32, 0.7, 0.16, 3) * 1.3, crack * 1.3)
    sfx("sfx_hammer_hit", clang, -10, 0.5, (1.4, 0.45, 0.3))
    # ---------------------------------------------------------------- the world crashing
    expl = mix(body(68, 20, 1.8, 0.5, 4.5) * 1.5, lowpass(noise(1.8), 900) * env_exp(1.8, 0.4) * 1.2, crack * 1.6,
               at(lowpass(noise(1.4), 2600) * env_exp(1.4, 0.22) * 0.5, 0.05), at(sparkle(1.0, 30, 1500, 6000) * 0.35, 0.1),
               at(crackle(1.5, 60, 800, 4000) * 0.6, 0.15))
    sfx("sfx_explosion", expl, -9.5, 0.7, (2.4, 0.7, 0.32))
    vcrash = mix(expl * 0.8, at(metal(1.2, 120) * 0.7, 0.02), at(metal(0.9, 260) * 0.5, 0.12), at(sparkle(0.7, 40, 2500, 9000) * 0.5, 0.05))
    sfx("sfx_vehicle_crash", vcrash, -9.5, 0.7, (2.0, 0.6, 0.3))
    whistle = mix(sine_sweep(2400, 500, 2.4, 1.3) * env_adsr(2.4, 0.5, 0.2, 0.8, 0.3) * 0.25, bandpass(noise(2.4), 600, 3000) * np.linspace(0.1, 1, int(SR * 2.4)) * 0.6)
    sfx("sfx_falling", whistle, -15, 0.5)
    thunder = mix(highpass(noise(0.12), 2000) * env_exp(0.12, 0.02) * 1.6, at(lowpass(noise(2.8), 400) * env_exp(2.8, 0.8) * 1.3, 0.05),
                  at(lowpass(noise(1.0), 1500) * env_exp(1.0, 0.15) * 0.6, 0.02), at(crackle(0.6, 300) * 0.6, 0.0))
    sfx("sfx_thunder", thunder, -11, 0.8, (2.4, 0.8, 0.35))
    warn = mix(sine_sweep(300, 900, 1.2, 0.8) * env_adsr(1.2, 0.3, 0.2, 0.8, 0.3) * 0.3, lowpass(noise(1.2), 600) * np.linspace(0.2, 1, int(SR * 1.2)) * 0.8)
    sfx("sfx_meteor_warn", warn, -15, 0.6)
    # ---------------------------------------------------------------- rope : electric, surreal
    sfx("sfx_grind_start", mix(zap(0.4, 320) * 1.3, at(boom(70, 0.8, 0.8) * 0.6, 0.0), sparkle(0.5, 30, 3000, 12000) * 0.5), -10.5, 0.7, (1.2, 0.35, 0.25))
    gl = t_axis(3.0)
    buzz = mix(np.sign(np.sin(2 * np.pi * 100 * gl)) * 0.07 * (1 + 0.35 * np.sin(2 * np.pi * 7 * gl)), lowpass(saw(50, 3.0), 900) * 0.25,
               crackle(3.0, 180) * 1.3, bandpass(noise(3.0), 2500, 7000) * 0.18, np.sin(2 * np.pi * np.cumsum(1200 + 300 * np.sin(2 * np.pi * 0.7 * gl)) / SR) * 0.04)
    write_st("sfx_grind_loop", *stereo(loopable(buzz, 0.3), 0.6), rms_db=-15)
    sfx("sfx_rope_exit", mix(zap(0.3, 200), boom(60, 1.0, 1.2), whoosh(0.8, 500, 10000) * 1.1), -10, 0.7, (1.6, 0.5, 0.3))
    # ---------------------------------------------------------------- speed states
    hyper = mix(boom(46, 1.6, 2.0) * 1.3, at(riser(0.35, 500, 12000) * 0.5, 0.0), at(whoosh(1.2, 10000, 300) * 1.2, 0.05),
                at(chime([392, 587, 784, 1175], 1.6, 0.5) * 0.35, 0.05), at(sparkle(1.2, 40, 3000, 13000) * 0.6, 0.05))
    sfx("sfx_hyper", hyper, -9.5, 0.8, (2.2, 0.7, 0.32))
    sfx("sfx_hyper_out", mix(whoosh(0.7, 6000, 500) * 1.1, sine_sweep(900, 250, 0.6, 0.6) * env_exp(0.6, 0.2) * 0.3), -13, 0.5, (1.0, 0.3, 0.2))
    light = mix(riser(0.5, 400, 15000) * 1.0, at(boom(36, 2.2, 2.2) * 1.5, 0.45), at(whoosh(1.4, 12000, 250) * 1.2, 0.45),
                at(sparkle(1.6, 60, 3000, 14000) * 0.9, 0.45), at(chime([523, 784, 1047, 1568, 2093], 2.2, 0.8) * 0.4, 0.47))
    sfx("sfx_light", light, -9, 0.9, (2.8, 0.9, 0.35))
    lt = t_axis(3.0)
    hum = mix(np.sin(2 * np.pi * 110 * lt) * 0.3, np.sin(2 * np.pi * 165 * lt) * 0.15, np.sin(2 * np.pi * 220 * lt * (1 + 0.002 * np.sin(2 * np.pi * 5 * lt))) * 0.2,
              bandpass(noise(3.0), 3000, 9000) * 0.2 * (0.6 + 0.4 * np.sin(2 * np.pi * 11 * lt)), crackle(3.0, 60) * 0.7)
    write_st("sfx_light_loop", *stereo(loopable(hum, 0.3), 0.7), rms_db=-16)
    # Wind : three layers the game blends by speed.
    for name, lo, hi, gain, db in (("sfx_wind_low", 60, 400, 1.0, -16), ("sfx_wind_mid", 500, 2500, 0.8, -17), ("sfx_wind_high", 2500, 9000, 0.6, -19)):
        wt = t_axis(6.0)
        gust = 0.7 + 0.3 * np.sin(2 * np.pi * 0.37 * wt + lo) * np.sin(2 * np.pi * 0.11 * wt + 1.0)
        w = bandpass(noise(6.0), lo, hi) * gust * gain
        write_st(name, *stereo(loopable(w, 0.6), 0.8, 13), rms_db=db)
    wt = t_axis(6.0)
    gust = 0.75 + 0.25 * np.sin(2 * np.pi * 0.37 * wt) * np.sin(2 * np.pi * 0.11 * wt + 1.0)
    wind = mix(lowpass(noise(6.0), 260) * gust, bandpass(noise(6.0), 700, 2600) * 0.5 * gust, highpass(noise(6.0), 5000) * 0.12)
    write_st("sfx_wind_loop", *stereo(loopable(wind, 0.6), 0.8, 13), rms_db=-15)
    # ---------------------------------------------------------------- race
    count = mix(click(0.01, 3000) * 0.6, np.sin(2 * np.pi * 880 * t_axis(0.3)) * env_exp(0.3, 0.09) * 0.8, body(220, 110, 0.14, 0.04) * 0.7,
                at(lowpass(noise(0.3), 800) * env_exp(0.3, 0.06) * 0.4, 0.0))
    sfx("sfx_count", count, -12, 0.4, (0.9, 0.3, 0.25))
    go = mix(click(0.015, 2500), chime([1047, 1319, 1568, 2093], 1.1, 0.35) * 0.8, boom(60, 0.8, 1.2) * 1.0, whoosh(0.7, 500, 10000) * 1.0)
    sfx("sfx_go", go, -10, 0.7, (1.6, 0.5, 0.3))
    perfect = mix(go * 0.6, at(chime([1568, 2093, 2637, 3136], 1.2, 0.4) * 0.6, 0.05), at(sparkle(0.9, 40, 4000, 13000) * 0.7, 0.05))
    sfx("sfx_perfect_start", perfect, -10, 0.8, (1.8, 0.6, 0.3))
    passing = mix(sine_sweep(900, 300, 0.9, 1.0) * env_adsr(0.9, 0.3, 0.2, 0.6, 0.3) * 0.3, whoosh(0.9, 300, 6000) * 1.0)
    sfx("sfx_pass", passing, -12.5, 0.8, (1.0, 0.3, 0.2))
    # The rival crossed first : a falling brass sting.
    sting = mix(*[at(saw(note_freq(n), 0.9) * env_adsr(0.9, 0.02, 0.2, 0.6, 0.3) * 0.25, 0.0) for n in (55, 58, 62)])
    sting = sweep_filter(sting, 3000, 400, "low")
    sfx("sfx_rival_win", mix(sting, at(body(80, 40, 0.8, 0.25, 3) * 0.8, 0.0)), -11, 0.6, (1.6, 0.5, 0.3))
    sfx("sfx_lose", mix(sting * 0.8, at(sine_sweep(400, 120, 1.0, 0.7) * env_exp(1.0, 0.4) * 0.3, 0.1)), -11, 0.6, (1.8, 0.6, 0.3))
    fin = mix(chime([784, 1047, 1319, 1568, 2093], 1.8, 0.7) * 0.9, boom(55, 1.2, 1.2) * 0.9, at(sparkle(1.4, 60, 3000, 13000) * 0.8, 0.05),
              at(whoosh(1.0, 300, 11000) * 0.8, 0.0))
    sfx("sfx_finish", fin, -9.5, 0.9, (2.6, 0.8, 0.35))
    for i, letter in enumerate(("c", "b", "a", "s")):
        notes = [60 + i * 2, 64 + i * 2, 67 + i * 2] + ([72 + i * 2, 76 + i * 2] if i >= 2 else [])
        rank = mix(body(90, 30, 0.7, 0.2, 4) * 1.3, highpass(noise(0.04), 2500) * env_exp(0.04, 0.008) * 1.6,
                   at(chime([note_freq(n) for n in notes], 1.4, 0.5) * (0.5 + 0.15 * i), 0.02), at(sparkle(1.0, 10 + 15 * i, 3000, 12000) * 0.6, 0.03))
        sfx("sfx_rank_" + letter, rank, -10, 0.8, (2.0, 0.6, 0.3))
    sfx("sfx_rank", mix(body(90, 30, 0.7, 0.2, 4) * 1.3, chime([523, 659, 784, 1047], 1.4, 0.5) * 0.6), -10, 0.8, (2.0, 0.6, 0.3))
    tick = mix(click(0.006, 4000) * 0.8, np.sin(2 * np.pi * 2400 * t_axis(0.04)) * env_exp(0.04, 0.01) * 0.6)
    sfx("sfx_tick", tick, -18, 0.2)
    beep = lambda f: square(f, 0.14, 0.5) * env_adsr(0.14, 0.004, 0.03, 0.7, 0.04)
    alarm = lowpass(mix(beep(620), at(beep(465), 0.18), at(beep(620), 0.36)), 3500)
    sfx("sfx_wrong_way", mix(alarm, at(body(120, 60, 0.3, 0.1) * 0.5, 0.0)), -13.5, 0.5, (0.8, 0.25, 0.2))
    # ---------------------------------------------------------------- death and the way back
    death = mix(body(140, 28, 0.7, 0.16, 4.5) * 1.4, crack * 1.6, at(sine_sweep(1000, 70, 0.8, 0.6) * env_exp(0.8, 0.3) * 0.35, 0.03),
                at(lowpass(noise(0.9), 1200) * env_exp(0.9, 0.22) * 0.6, 0.02), at(crackle(0.5, 200) * 0.5, 0.0))
    sfx("sfx_death", death, -10, 0.6, (1.6, 0.5, 0.3))
    travel = mix(riser(1.0, 300, 9000) * 0.9, whoosh(1.1, 400, 7000) * 0.8, at(sparkle(0.8, 20, 3000, 10000) * 0.3, 0.2))
    sfx("sfx_rewind", travel, -13, 0.8, (1.2, 0.4, 0.25))
    resp = mix(whoosh(0.5, 400, 8000)[::-1] * 0.9, at(chime([880, 1320, 1760, 2640], 0.8, 0.25) * 0.5, 0.4), at(body(200, 100, 0.14, 0.04) * 0.6, 0.42),
               at(sparkle(0.6, 30, 4000, 12000) * 0.5, 0.42))
    sfx("sfx_respawn", resp, -12, 0.7, (1.2, 0.35, 0.28))
    fall = mix(sine_sweep(1400, 200, 0.8, 1.2) * env_exp(0.8, 0.4) * 0.3, whoosh(0.8, 3000, 300) * 0.9)
    sfx("sfx_fall", fall, -13, 0.5)
    # ---------------------------------------------------------------- rivals
    rt = t_axis(3.0)
    roar = mix(lowpass(noise(3.0), 420) * (0.8 + 0.2 * np.sin(2 * np.pi * 9 * rt)), bandpass(noise(3.0), 900, 4000) * 0.35, crackle(3.0, 90, 600, 3000) * 0.9,
               np.sin(2 * np.pi * 48 * rt) * 0.3)
    write_st("sfx_engine_rocket", *stereo(loopable(drive(roar * 1.5, 2.0), 0.3), 0.5), rms_db=-15)
    flaps = np.zeros(int(SR * 3.0))
    for k in range(int(3.0 * 7)):
        o = int(SR * (k / 7.0 + rng.uniform(0, 0.04)))
        f = bandpass(noise(0.07), 300, 1800) * env_exp(0.07, 0.02) * rng.uniform(0.6, 1.0)
        flaps[o:o + len(f)] += f[:len(flaps) - o]
    caw = np.zeros(int(SR * 3.0))
    for k in range(3):
        o = int(SR * rng.uniform(0.2, 2.6))
        c = drive(sine_sweep(1300, 900, 0.18, 1.0) * env_adsr(0.18, 0.01, 0.05, 0.7, 0.08), 2.5) * 0.25
        caw[o:o + len(c)] += c[:len(caw) - o]
    write_st("sfx_engine_birds", *stereo(loopable(mix(flaps * 1.2, caw, bandpass(noise(3.0), 800, 3000) * 0.05), 0.2), 0.8), rms_db=-17)
    trex = mix(drive(sine_sweep(160, 70, 1.6, 0.8) * env_adsr(1.6, 0.15, 0.3, 0.8, 0.4) * 0.8, 3), bandpass(noise(1.6), 200, 1800) * env_adsr(1.6, 0.1, 0.3, 0.7, 0.5) * 0.9,
               np.sin(2 * np.pi * np.cumsum(90 + 30 * np.sin(2 * np.pi * 23 * t_axis(1.6))) / SR) * env_adsr(1.6, 0.1, 0.3, 0.8, 0.4) * 0.5)
    sfx("sfx_trex_roar", trex, -10, 0.6, (2.0, 0.6, 0.3))
    horn = mix(saw(220, 1.2) * 0.3, saw(277, 1.2) * 0.25, saw(330, 1.2) * 0.2)
    sfx("sfx_horn", lowpass(horn, 2200) * env_adsr(1.2, 0.03, 0.1, 0.9, 0.2), -13, 0.5, (1.2, 0.4, 0.25))
    # ---------------------------------------------------------------- UI
    sfx("ui_move", mix(click(0.012, 3500) * 0.8, np.sin(2 * np.pi * 1900 * t_axis(0.05)) * env_exp(0.05, 0.012) * 0.7), -17, 0.4)
    sfx("ui_select", mix(chime([1319, 1760, 2637], 0.3, 0.08) * 0.5, body(240, 100, 0.14, 0.04) * 0.8, whoosh(0.2, 2500, 8000) * 0.4, click(0.01) * 0.6), -13.5, 0.5, (0.6, 0.2, 0.18))
    sfx("ui_back", mix(chime([1760, 1319], 0.25, 0.07) * 0.4, body(200, 90, 0.12, 0.04) * 0.6, whoosh(0.18, 6000, 1500) * 0.4), -15, 0.4)
    sfx("ui_hint", mix(np.sin(2 * np.pi * 1320 * t_axis(0.2)) * env_exp(0.2, 0.06) * 0.6, np.sin(2 * np.pi * 1760 * t_axis(0.2)) * env_exp(0.2, 0.05) * 0.4, click(0.008) * 0.4), -15.5, 0.5, (0.6, 0.2, 0.2))
    sfx("ui_slider", mix(click(0.008, 5000) * 0.7, np.sin(2 * np.pi * 2600 * t_axis(0.03)) * env_exp(0.03, 0.008) * 0.5), -19, 0.3)
    sfx("ui_whoosh", whoosh(0.5, 500, 9000) * 1.2, -14, 0.6, (0.8, 0.25, 0.2))
    # Transitions : the whole picture sucked into the centre, then blown out of it.
    suck = mix(riser(0.55, 300, 12000) * 1.1, sine_sweep(150, 900, 0.55, 2.0) * env_adsr(0.55, 0.4, 0.05, 0.9, 0.05) * 0.3, at(click(0.03, 1500) * 1.2, 0.52))
    sfx("ui_transition_in", suck, -12, 0.6, (0.8, 0.25, 0.2))
    blow = mix(boom(60, 0.9, 1.4) * 1.1, whoosh(0.7, 10000, 400) * 1.2, at(sparkle(0.5, 20, 3000, 11000) * 0.4, 0.02))
    sfx("ui_transition_out", blow, -12, 0.7, (1.2, 0.4, 0.25))


# ============================================================================= music : drum & bass in stems

BPM = 174.0
BEAT = 60.0 / BPM
STEP = BEAT / 4.0
BARS = 32
LENGTH = BARS * 4 * BEAT


def place(track, x, time, gain=1.0):
    o = int(round(time * SR))
    if o >= len(track):
        return
    n = min(len(x), len(track) - o)
    track[o:o + n] += x[:n] * gain


def kick():
    return mix(drive(sine_sweep(170, 46, 0.32, 0.25) * env_exp(0.32, 0.11), 2.2) * 1.1, click(0.006, 3000) * 0.6)


def snare(bright=1.0):
    return mix(sine_sweep(240, 180, 0.18, 1.0) * env_exp(0.18, 0.045) * 0.7,
               bandpass(noise(0.22), 1200, 9000 * bright) * env_exp(0.22, 0.06) * 1.1, click(0.006, 4000) * 0.4)


def hat(open_=False):
    d = 0.14 if open_ else 0.035
    return highpass(noise(d), 7500) * env_exp(d, d * 0.3) * (0.5 if open_ else 0.4)


def ride():
    return mix(metal(0.6, 640) * 0.15, highpass(noise(0.4), 6000) * env_exp(0.4, 0.12) * 0.2)


def reese(freq, d, cutoff=700):
    a = saw(freq, d, 0.004)
    b = saw(freq, d, -0.004)
    c = saw(freq * 0.5, d)
    raw = (a + b) * 0.5 + c * 0.4
    out = sweep_filter(raw, cutoff * 0.6, cutoff * 1.6, "low", 16)
    return drive(out * 1.4, 2.0) * env_adsr(d, 0.01, 0.1, 0.9, 0.05)


def subbass(freq, d):
    return np.sin(2 * np.pi * freq * t_axis(d)) * env_adsr(d, 0.005, 0.05, 0.95, 0.04)


def supersaw(freqs, d, cutoff=3200):
    out = np.zeros(int(SR * d))
    for f in freqs:
        for det in (-0.012, -0.005, 0.0, 0.006, 0.013):
            out += saw(f, d, det)
    out = lowpass(out / (len(freqs) * 5), cutoff)
    return out * env_adsr(d, 0.02, 0.2, 0.75, 0.15)


def warm_pad(freqs, d, cutoff=1400):
    out = np.zeros(int(SR * d))
    for f in freqs:
        out += saw(f, d, 0.003) + saw(f, d, -0.003)
    return lowpass(out / (len(freqs) * 2), cutoff) * env_adsr(d, d * 0.3, 0.1, 0.9, d * 0.3)


def pluck(freq, d=0.18, cutoff=5000):
    return sweep_filter(saw(freq, d) + square(freq * 2, d, 0.3) * 0.3, cutoff, 300, "low", 8) * env_exp(d, d * 0.35)


def lead(freq, d):
    tt = t_axis(d)
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.5 * tt) * np.clip(tt / 0.2, 0, 1)
    ph = 2 * np.pi * np.cumsum(freq * vib) / SR
    raw = 2 * ((ph / (2 * np.pi)) % 1.0) - 1 + 0.5 * np.sign(np.sin(ph * 1.003))
    return lowpass(raw, 4200) * env_adsr(d, 0.01, 0.08, 0.8, 0.08) * 0.5


def chord_notes(root, kind):
    shapes = {"m": [0, 3, 7], "M": [0, 4, 7], "m7": [0, 3, 7, 10], "M7": [0, 4, 7, 11], "sus": [0, 5, 7]}
    return [root + i for i in shapes[kind]]


def sidechain(length_s):
    """A volume ducking curve that pumps on every kick of the groove."""
    env = np.ones(int(SR * length_s))
    duck = 1 - 0.65 * np.exp(-t_axis(0.25) / 0.06)
    for bar in range(BARS):
        for step in (0, 10):
            o = int(SR * (bar * 4 * BEAT + step * STEP))
            n = min(len(duck), len(env) - o)
            if n > 0:
                env[o:o + n] = np.minimum(env[o:o + n], duck[:n])
    return env


def music_world(name, root, progression, seed, dark=False):
    """Three stems of the same 32 bars : base (drums, bass, pad), hyper (breaks, arps, stabs), light (lead, choir, sparkle)."""
    lr = np.random.default_rng(seed)
    n = int(SR * LENGTH)
    base_l, base_r = np.zeros(n), np.zeros(n)
    hyp_l, hyp_r = np.zeros(n), np.zeros(n)
    lig_l, lig_r = np.zeros(n), np.zeros(n)
    k, s, s2 = kick(), snare(), snare(0.7)
    hc, ho = hat(), hat(True)
    rd = ride()
    pump = sidechain(LENGTH)
    for bar in range(BARS):
        t0 = bar * 4 * BEAT
        fill = bar % 8 == 7
        chord_root, chord_kind = progression[(bar // 2) % len(progression)]
        notes = chord_notes(root + chord_root, chord_kind)
        # ---- base : two-step, rolling sub and reese, a pad
        for step in range(16):
            tt = t0 + step * STEP
            if step in (0, 10) or (fill and step in (6, 14)):
                place(base_l, k, tt, 1.0); place(base_r, k, tt, 1.0)
            if step in (4, 12):
                l, r = pan(s, -0.05)
                place(base_l, l, tt, 0.9); place(base_r, r, tt, 0.9)
            if step % 2 == 0:
                l, r = pan(hc, 0.3)
                place(base_l, l, tt, 0.7 if step % 4 else 0.45); place(base_r, r, tt, 0.7 if step % 4 else 0.45)
        bass_f = note_freq(root + chord_root - 24)
        rb = reese(bass_f, 4 * BEAT, 500 if dark else 650) * 0.45
        sb = subbass(bass_f, 4 * BEAT) * 0.55
        place(base_l, rb + sb, t0); place(base_r, rb + sb, t0)
        if bar % 2 == 0:
            pd = warm_pad([note_freq(x) for x in notes], 8 * BEAT, 1000 if dark else 1500) * 0.32
            l, r = stereo(pd, 0.7, 14, 0.1)
            place(base_l, l, t0); place(base_r, r, t0)
        # ---- hyper : the break, ghost snares, open hats, ride, plucky arps
        for step in range(16):
            tt = t0 + step * STEP
            if step in (7, 9, 15) or (fill and step >= 12):
                l, r = pan(s2, 0.2 if step % 2 else -0.2)
                place(hyp_l, l, tt, 0.35); place(hyp_r, r, tt, 0.35)
            if step in (2, 6, 14):
                l, r = pan(ho, -0.4)
                place(hyp_l, l, tt, 0.5); place(hyp_r, r, tt, 0.5)
            if step % 4 == 0:
                l, r = pan(rd, 0.45)
                place(hyp_l, l, tt, 0.6); place(hyp_r, r, tt, 0.6)
            arp = notes[(step + bar) % len(notes)] + (12 if step % 8 >= 4 else 0)
            p = pluck(note_freq(arp + 12), STEP * 1.6, 3500 if dark else 6000) * 0.22
            l, r = pan(p, 0.5 * np.sin(step * 0.8))
            place(hyp_l, l, tt); place(hyp_r, r, tt)
        if bar % 4 == 3:
            st = supersaw([note_freq(x + 12) for x in notes], BEAT * 0.5, 4000) * 0.35
            for st_t in (2.0, 2.75):
                l, r = stereo(st, 0.8)
                place(hyp_l, l, t0 + st_t * BEAT); place(hyp_r, r, t0 + st_t * BEAT)
        # ---- light : a soaring lead, a bright supersaw bed, sparkle
        if bar % 8 < 6:
            melody = [notes[0] + 24, notes[1] + 24, notes[2] + 24, notes[1] + 24, notes[0] + 26, notes[2] + 24]
            for i in range(4):
                f = note_freq(melody[(bar + i * 2 + int(lr.integers(0, 2))) % len(melody)])
                ld = lead(f, BEAT * 0.95) * 0.3
                l, r = pan(ld, 0.0)
                place(lig_l, l, t0 + i * BEAT); place(lig_r, r, t0 + i * BEAT)
        if bar % 2 == 0:
            bed = supersaw([note_freq(x + 12) for x in notes], 8 * BEAT, 2600 if dark else 4200) * 0.22
            l, r = stereo(bed, 0.9, 17)
            place(lig_l, l, t0); place(lig_r, r, t0)
        spk = sparkle(4 * BEAT, 10, 4000, 12000) * 0.15
        l, r = stereo(spk, 1.0)
        place(lig_l, l, t0); place(lig_r, r, t0)
    # Pumping on the kick for the pads and the bass, a room on everything.
    base_l, base_r = base_l * (0.55 + 0.45 * pump), base_r * (0.55 + 0.45 * pump)
    hyp_l, hyp_r = hyp_l * (0.7 + 0.3 * pump), hyp_r * (0.7 + 0.3 * pump)
    lig_l, lig_r = lig_l * (0.6 + 0.4 * pump), lig_r * (0.6 + 0.4 * pump)
    for stem, (l, r), db in (("_base", (base_l, base_r), -15.5), ("_hyper", (hyp_l, hyp_r), -19), ("_light", (lig_l, lig_r), -19.5)):
        l = l[:n]
        r = r[:n]
        write_st(name + stem, l, r, rms_db=db)


def music_menu(name, root, progression, bpm_scale=0.5, seed=5):
    """Title / stage select : half time, wide pads, a soft beat and arps."""
    n = int(SR * LENGTH)
    left, right = np.zeros(n), np.zeros(n)
    k, s, hc = kick(), snare(0.6), hat()
    for bar in range(BARS):
        t0 = bar * 4 * BEAT
        chord_root, chord_kind = progression[(bar // 2) % len(progression)]
        notes = chord_notes(root + chord_root, chord_kind)
        for step in range(16):
            tt = t0 + step * STEP
            if step == 0 or (bar % 2 == 1 and step == 10):
                place(left, k, tt, 0.8); place(right, k, tt, 0.8)
            if step == 8:
                place(left, s, tt, 0.6); place(right, s, tt, 0.6)
            if step % 2 == 0:
                l, r = pan(hc, 0.3)
                place(left, l, tt, 0.4); place(right, r, tt, 0.4)
            if step % 2 == 0:
                p = pluck(note_freq(notes[(step // 2 + bar) % len(notes)] + 24), STEP * 2.5, 5000) * 0.18
                l, r = pan(p, 0.6 * np.sin(step))
                place(left, l, tt); place(right, r, tt)
        if bar % 2 == 0:
            pd = warm_pad([note_freq(x + 12) for x in notes], 8 * BEAT, 1800) * 0.4
            l, r = stereo(pd, 0.9, 15)
            place(left, l, t0); place(right, r, t0)
            sb = subbass(note_freq(root + chord_root - 24), 8 * BEAT) * 0.4
            place(left, sb, t0); place(right, sb, t0)
    write_st(name, left, right, rms_db=-16)


def stinger(name, root, kind, length, rising=True):
    notes = chord_notes(root, kind) + [root + 12]
    n = int(SR * length)
    left, right = np.zeros(n), np.zeros(n)
    for i, x in enumerate(notes):
        p = supersaw([note_freq(x + 12)], length - i * 0.12, 4500 if rising else 1800) * 0.35
        l, r = pan(p, (i - 1.5) * 0.3)
        place(left, l, i * 0.12 if rising else 0); place(right, r, i * 0.12 if rising else 0)
    place(left, kick(), 0.0); place(right, kick(), 0.0)
    pd = warm_pad([note_freq(x) for x in notes], length, 2000) * 0.4
    l, r = stereo(pd, 0.9)
    left += l[:n]; right += r[:n]
    write_st(name, left, right, rms_db=-14)


def make_music():
    # Each world its own key and colour : bright and hopeful, driven, dark and cosmic.
    music_world("music_w1", 57, [(0, "m"), (-4, "M"), (-9, "M"), (-2, "M")], 11)
    music_world("music_w2", 52, [(0, "m"), (1, "M"), (-2, "M"), (-4, "M7")], 22)
    music_world("music_w3", 50, [(0, "m"), (3, "M"), (-2, "sus"), (-4, "M")], 33, dark=True)
    music_menu("music_title", 57, [(0, "m"), (-4, "M"), (-9, "M"), (-2, "M")])
    music_menu("music_select", 52, [(0, "m"), (1, "M"), (-2, "M"), (-4, "M7")], seed=7)
    stinger("music_win", 60, "M", 6.0, True)
    stinger("music_lose", 57, "m", 5.0, False)
    stinger("music_intro", 57, "sus", 4.0, True)


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "all"
    if what in ("all", "sfx"):
        make_sfx()
    if what in ("all", "music"):
        make_music()
