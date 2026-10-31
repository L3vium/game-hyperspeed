"""Procedural sound design for Hyper Speed.

Synthesizes every SFX and music loop with numpy/scipy and writes WAV files into
Assets/_Game/Resources/Audio. Deterministic (fixed seed) : re-run to regenerate.

    python Tools/generate_audio.py
"""
import os
import numpy as np
from scipy import signal
from scipy.io import wavfile

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Game", "Resources", "Audio")
rng = np.random.default_rng(1337)


# ----------------------------------------------------------------------------- primitives

def t_axis(duration):
    return np.arange(int(SR * duration)) / SR


def noise(duration):
    return rng.uniform(-1.0, 1.0, int(SR * duration))


def sweep_phase(f0, f1, duration, curve=1.0):
    t = t_axis(duration)
    k = (t / duration) ** curve
    freq = f0 + (f1 - f0) * k
    return 2 * np.pi * np.cumsum(freq) / SR


def sine_sweep(f0, f1, duration, curve=1.0):
    return np.sin(sweep_phase(f0, f1, duration, curve))


def saw(freq, duration, detune=0.0):
    t = t_axis(duration)
    return 2.0 * ((t * freq * (1 + detune)) % 1.0) - 1.0


def square(freq, duration, duty=0.5):
    t = t_axis(duration)
    return np.where((t * freq) % 1.0 < duty, 1.0, -1.0)


def tri(freq, duration):
    return 2.0 * np.abs(saw(freq, duration)) - 1.0


def env_exp(duration, decay, attack=0.002):
    t = t_axis(duration)
    a = np.clip(t / max(attack, 1e-5), 0, 1)
    return a * np.exp(-t / decay)


def env_adsr(duration, a, d, s, r):
    n = int(SR * duration)
    t = np.arange(n) / SR
    e = np.ones(n) * s
    e[t < a] = t[t < a] / max(a, 1e-5)
    m = (t >= a) & (t < a + d)
    e[m] = 1 - (1 - s) * (t[m] - a) / max(d, 1e-5)
    rel = t > duration - r
    e[rel] *= np.clip((duration - t[rel]) / max(r, 1e-5), 0, 1)
    return e


def lowpass(x, cutoff, order=2):
    b, a = signal.butter(order, min(cutoff, SR * 0.45) / (SR / 2), "low")
    return signal.lfilter(b, a, x)


def highpass(x, cutoff, order=2):
    b, a = signal.butter(order, cutoff / (SR / 2), "high")
    return signal.lfilter(b, a, x)


def bandpass(x, lo, hi, order=2):
    b, a = signal.butter(order, [lo / (SR / 2), min(hi, SR * 0.45) / (SR / 2)], "band")
    return signal.lfilter(b, a, x)


def sweep_filter(x, f0, f1, kind="low", steps=48):
    """Time-varying filter by processing blocks with interpolated cutoff."""
    out = np.zeros_like(x)
    n = len(x)
    block = max(1, n // steps)
    zi = None
    for i in range(0, n, block):
        k = i / max(n - 1, 1)
        fc = f0 * (f1 / f0) ** k
        b, a = signal.butter(2, min(fc, SR * 0.45) / (SR / 2), kind)
        if zi is None:
            zi = signal.lfilter_zi(b, a) * 0
        seg, zi = signal.lfilter(b, a, x[i:i + block], zi=zi)
        out[i:i + block] = seg
    return out


def drive(x, amount):
    return np.tanh(x * amount) / np.tanh(amount)


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def pad(x, duration):
    n = int(SR * duration)
    if len(x) >= n:
        return x[:n]
    return np.concatenate([x, np.zeros(n - len(x))])


def at(x, offset):
    return np.concatenate([np.zeros(int(SR * offset)), x])


def normalize(x, peak=0.9):
    m = np.max(np.abs(x))
    return x if m < 1e-9 else x / m * peak


def fade(x, fin=0.002, fout=0.01):
    n = len(x)
    a = min(int(SR * fin), n)
    b = min(int(SR * fout), n)
    if a > 0:
        x[:a] *= np.linspace(0, 1, a)
    if b > 0:
        x[-b:] *= np.linspace(1, 0, b)
    return x


def loopable(x, crossfade=0.25):
    """Crossfade the tail into the head so the clip loops seamlessly."""
    c = int(SR * crossfade)
    head = x[:c].copy()
    tail = x[-c:].copy()
    w = np.linspace(0, 1, c)
    x = x[:-c].copy()
    x[:c] = head * w + tail * (1 - w)
    return x


def write(name, x, peak=0.9, stereo=None):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    if stereo is not None:
        data = np.stack([stereo[0], stereo[1]], axis=1)
        data = data / max(np.max(np.abs(data)), 1e-9) * peak
    else:
        data = normalize(x, peak)
    wavfile.write(path, SR, (data * 32767).astype(np.int16))
    print("wrote", name, round(len(data) / SR, 2), "s")


def note_freq(n):
    """MIDI note to Hz."""
    return 440.0 * 2 ** ((n - 69) / 12.0)


# ----------------------------------------------------------------------------- SFX

def squish(duration=0.12, f0=420, f1=140):
    """Slimy wet blip : falling sine with vibrato + bandpassed noise."""
    t = t_axis(duration)
    vib = 1 + 0.08 * np.sin(2 * np.pi * 38 * t)
    ph = 2 * np.pi * np.cumsum((f0 + (f1 - f0) * (t / duration) ** 0.6) * vib) / SR
    body = np.sin(ph) * env_exp(duration, duration * 0.35)
    wet = bandpass(noise(duration), 500, 2500) * env_exp(duration, duration * 0.2) * 0.4
    return body + wet


def whoosh(duration, f0, f1, q_lo=0.5, q_hi=1.6):
    n = noise(duration)
    out = sweep_filter(n, f0, f1, "low") - sweep_filter(n, f0 * q_lo, f1 * q_lo, "low") * 0.6
    e = np.sin(np.pi * np.clip(t_axis(duration) / duration, 0, 1)) ** 1.5
    return out * e


def thump(duration=0.3, f0=140, f1=40):
    return sine_sweep(f0, f1, duration, 0.4) * env_exp(duration, duration * 0.3)


def chime(freqs, duration, decay):
    out = np.zeros(int(SR * duration))
    for i, f in enumerate(freqs):
        out += np.sin(2 * np.pi * f * t_axis(duration)) * env_exp(duration, decay) / (1 + i * 0.4)
    return out


def arp(notes, step, duration_each, wave="square", decay=0.12):
    total = step * len(notes) + duration_each
    out = np.zeros(int(SR * total))
    for i, n in enumerate(notes):
        f = note_freq(n)
        v = (square(f, duration_each, 0.3) if wave == "square" else np.sin(2 * np.pi * f * t_axis(duration_each)))
        v = v * env_exp(duration_each, decay)
        s = int(SR * step * i)
        out[s:s + len(v)] += v
    return out


def make_sfx():
    write("sfx_jump", fade(mix(squish(0.1, 260, 180) * 0.5, sine_sweep(300, 760, 0.16, 0.7) * env_exp(0.16, 0.06) * 0.7, whoosh(0.18, 800, 3000) * 0.3)), 0.75)
    write("sfx_doublejump", fade(mix(sine_sweep(500, 1400, 0.18, 0.6) * env_exp(0.18, 0.07) * 0.6,
                                     at(chime([2093, 3136], 0.3, 0.08) * 0.35, 0.03), whoosh(0.25, 1500, 5000) * 0.5)), 0.75)
    write("sfx_land", fade(mix(thump(0.16, 130, 55) * 0.8, squish(0.1, 300, 120) * 0.5, lowpass(noise(0.08), 900) * env_exp(0.08, 0.02) * 0.4)), 0.7)
    write("sfx_land_heavy", fade(drive(mix(thump(0.35, 120, 35), lowpass(noise(0.25), 600) * env_exp(0.25, 0.06) * 0.8, squish(0.18, 260, 90) * 0.5), 1.8)), 0.85)
    write("sfx_land_good", fade(mix(thump(0.25, 140, 45) * 0.8, at(chime([880, 1320, 1760], 0.5, 0.15) * 0.5, 0.01), whoosh(0.3, 3000, 600) * 0.3)), 0.85)
    write("sfx_land_perfect", fade(mix(drive(thump(0.4, 160, 40), 2) * 0.9, at(arp([72, 76, 79, 84, 88], 0.045, 0.3, "square", 0.1) * 0.35, 0.02),
                                       at(chime([2093, 2637, 3136], 0.9, 0.3) * 0.4, 0.2), whoosh(0.5, 6000, 500) * 0.5)), 0.9)
    write("sfx_dash", fade(mix(whoosh(0.32, 6000, 500) * 1.0, thump(0.15, 90, 40) * 0.6, sine_sweep(900, 200, 0.2) * env_exp(0.2, 0.05) * 0.25)), 0.85)
    write("sfx_dive", fade(mix(sine_sweep(1400, 250, 0.4, 0.8) * env_exp(0.4, 0.2) * 0.4, whoosh(0.45, 2500, 400) * 0.8)), 0.8)
    write("sfx_punch_whoosh", fade(whoosh(0.16, 900, 4500)), 0.6)
    crack = highpass(noise(0.06), 2500) * env_exp(0.06, 0.012)
    write("sfx_punch_hit", fade(drive(mix(thump(0.4, 110, 38) * 1.2, crack * 1.2, lowpass(noise(0.3), 1500) * env_exp(0.3, 0.05) * 0.8), 2.5)), 0.95)

    pings = np.zeros(int(SR * 0.6))
    for _ in range(26):
        f = rng.uniform(2200, 7800)
        d = rng.uniform(0.05, 0.25)
        p = np.sin(2 * np.pi * f * t_axis(d)) * env_exp(d, d * 0.3)
        o = int(SR * rng.uniform(0, 0.18))
        pings[o:o + len(p)] += p * rng.uniform(0.2, 0.6)
    write("sfx_shatter", fade(mix(pings, highpass(noise(0.4), 3000) * env_exp(0.4, 0.07) * 0.9, thump(0.15, 200, 80) * 0.4)), 0.8)

    knocks = mix(*[at(bandpass(noise(0.08), 300, 1300) * env_exp(0.08, 0.02), rng.uniform(0, 0.08)) for _ in range(5)])
    write("sfx_crate", fade(drive(mix(knocks * 1.2, thump(0.2, 160, 60) * 0.7), 1.5)), 0.85)
    write("sfx_rock", fade(drive(mix(lowpass(noise(0.45), 900) * env_exp(0.45, 0.1) * 1.2, thump(0.35, 100, 35)), 2.2)), 0.85)
    t = t_axis(0.9)
    clang = sum(np.sin(2 * np.pi * f * t) * np.exp(-t / d) for f, d in [(420, 0.4), (1130, 0.25), (1870, 0.18), (2650, 0.12), (3480, 0.08)])
    write("sfx_carhit", fade(drive(mix(clang * 0.5, thump(0.3, 120, 40), highpass(noise(0.2), 1500) * env_exp(0.2, 0.04) * 0.7), 1.6)), 0.9)
    write("sfx_explosion", fade(drive(mix(lowpass(noise(1.4), 1200) * env_exp(1.4, 0.35), thump(0.9, 70, 25) * 1.4,
                                          highpass(noise(0.15), 2000) * env_exp(0.15, 0.03) * 0.6), 2.0)), 0.95)
    write("sfx_meteor_fall", fade(mix(sine_sweep(2200, 300, 1.1, 0.7) * env_adsr(1.1, 0.3, 0.2, 0.7, 0.3) * 0.25, whoosh(1.1, 400, 2500) * 0.9)), 0.7)

    t = t_axis(0.7)
    vroom = sum(saw(1, 0.7) * 0 + np.sin(sweep_phase(110 * m, 440 * m, 0.7, 0.5)) / m for m in (1, 2, 3, 4))
    write("sfx_boost", fade(mix(lowpass(vroom, 3000) * env_adsr(0.7, 0.02, 0.2, 0.6, 0.3) * 0.6, whoosh(0.7, 500, 7000) * 1.0,
                               at(chime([1568, 2349], 0.4, 0.1) * 0.25, 0.05))), 0.85)
    write("sfx_boost_small", fade(mix(sine_sweep(300, 900, 0.3, 0.6) * env_exp(0.3, 0.1) * 0.5, whoosh(0.3, 800, 5000) * 0.8)), 0.75)
    write("sfx_ring", fade(mix(chime([1318.5, 1975.5, 2637], 0.8, 0.22), at(chime([1760, 2637, 3520], 0.7, 0.2) * 0.8, 0.07))), 0.75)
    t = t_axis(0.55)
    boing = np.sin(2 * np.pi * np.cumsum(220 + 700 * (t / 0.55) ** 0.5 + 60 * np.sin(2 * np.pi * 18 * t) * np.exp(-t * 4)) / SR)
    write("sfx_launch", fade(mix(boing * env_exp(0.55, 0.25) * 0.7, whoosh(0.55, 400, 6000) * 0.8, thump(0.2, 90, 40) * 0.5)), 0.85)
    write("sfx_crash", fade(drive(mix(thump(0.35, 120, 35) * 1.2, lowpass(noise(0.3), 1600) * env_exp(0.3, 0.06),
                                      at(squish(0.35, 500, 90) * 0.8, 0.05), highpass(noise(0.08), 2500) * env_exp(0.08, 0.02) * 0.6), 2.0)), 0.95)
    write("sfx_respawn", fade(mix(arp([60, 67, 72, 76, 79, 84], 0.05, 0.4, "sine", 0.18) * 0.7, whoosh(0.6, 300, 6000) * 0.4)), 0.8)

    d = 1.6
    t = t_axis(d)
    shimmer = sum(np.sin(2 * np.pi * f * t + 3 * np.sin(2 * np.pi * 6 * t)) for f in (523, 659, 784, 1047, 1568)) * env_adsr(d, 0.4, 0.3, 0.6, 0.6)
    write("sfx_hyper", fade(drive(mix(sine_sweep(60, 30, d, 0.5) * env_exp(d, 0.6) * 1.2, sweep_filter(noise(d), 200, 12000, "low") * env_adsr(d, 0.6, 0.2, 0.5, 0.5) * 0.7,
                                      shimmer * 0.2, at(drive(thump(0.5, 200, 40), 3), 0.0) * 0.8), 1.5)), 0.95)
    write("sfx_hyper_out", fade(mix(sweep_filter(noise(0.6), 8000, 300, "low") * env_exp(0.6, 0.25), sine_sweep(800, 120, 0.6) * env_exp(0.6, 0.2) * 0.3)), 0.6)

    w = noise(6.5)
    w = lowpass(w, 700) * 0.7 + bandpass(w, 900, 2400) * 0.25 * (0.6 + 0.4 * np.sin(2 * np.pi * 0.35 * t_axis(6.5)))
    write("sfx_wind_loop", loopable(w, 0.5), 0.7)
    g = noise(2.5)
    scrape = bandpass(g, 2600, 4200, 2) * (0.7 + 0.3 * np.sin(2 * np.pi * 23 * t_axis(2.5))) + bandpass(g, 6000, 9000) * 0.3
    crackle = np.zeros_like(g)
    for _ in range(140):
        o = rng.integers(0, len(g) - 500)
        crackle[o:o + 400] += highpass(noise(400 / SR), 4000) * env_exp(400 / SR, 0.002)
    write("sfx_grind_loop", loopable(scrape + crackle * 0.5, 0.2), 0.7)
    write("sfx_grind_start", fade(mix(clang[:int(SR * 0.4)] * 0.5, bandpass(noise(0.4), 2000, 5000) * env_exp(0.4, 0.12), thump(0.15, 180, 70) * 0.5)), 0.85)
    patter = np.zeros(int(SR * 1.0))
    for i in range(14):
        s = mix(squish(0.06, 320, 140) * 0.5, bandpass(noise(0.05), 800, 3000) * env_exp(0.05, 0.01) * 0.6)
        o = int(SR * i / 14)
        patter[o:o + len(s)] += s[:len(patter) - o]
    write("sfx_wallrun_loop", loopable(patter + bandpass(noise(1.0), 1500, 3500) * 0.12, 0.05), 0.6)
    write("sfx_wall_attach", fade(mix(squish(0.12, 380, 150), bandpass(noise(0.2), 1500, 4000) * env_exp(0.2, 0.05) * 0.6)), 0.75)
    write("sfx_walljump", fade(mix(sine_sweep(350, 1100, 0.2, 0.5) * env_exp(0.2, 0.08) * 0.6, whoosh(0.3, 1200, 6000) * 0.9, squish(0.08, 400, 200) * 0.4)), 0.8)
    t = t_axis(0.35)
    screech = bandpass(noise(0.35), 1800, 3200) * (1 + 0.5 * np.sin(2 * np.pi * 31 * t))
    write("sfx_drift_start", fade(screech * env_adsr(0.35, 0.02, 0.1, 0.7, 0.15)), 0.6)
    t = t_axis(2.0)
    write("sfx_drift_loop", loopable(bandpass(noise(2.0), 1900, 3100) * (1 + 0.4 * np.sin(2 * np.pi * 27 * t)) + lowpass(noise(2.0), 400) * 0.5, 0.2), 0.55)
    write("sfx_drift_tier", fade(mix(arp([76, 83], 0.06, 0.15, "square", 0.06) * 0.6, chime([2637], 0.3, 0.08) * 0.3)), 0.6)
    write("sfx_step", fade(mix(squish(0.07, 240, 110) * 0.8, lowpass(noise(0.05), 1200) * env_exp(0.05, 0.012) * 0.5)), 0.6)
    t = t_axis(1.0)
    doppler = np.sin(2 * np.pi * np.cumsum(220 - 120 * np.tanh((t - 0.45) * 8)) / SR)
    write("sfx_pass", fade(mix(lowpass(saw(1, 1.0) * 0 + doppler, 2000) * np.exp(-((t - 0.45) ** 2) / 0.04) * 0.5,
                               whoosh(1.0, 300, 4000) * 0.9)), 0.8)
    write("sfx_count", fade(lowpass(square(880, 0.22, 0.5), 5000) * env_adsr(0.22, 0.002, 0.05, 0.6, 0.06)), 0.6)
    go = sum(lowpass(square(f, 0.7, 0.5), 6000) for f in (1760, 2217, 2637)) * env_adsr(0.7, 0.002, 0.1, 0.6, 0.3)
    write("sfx_go", fade(mix(go * 0.4, whoosh(0.7, 400, 8000) * 0.8, drive(thump(0.3, 150, 40), 2) * 0.7)), 0.85)
    stab = sum(saw(note_freq(n), 1.2, d) for n in (60, 64, 67, 72, 76) for d in (-0.006, 0.0, 0.006))
    cymbal = highpass(noise(1.6), 5000) * env_exp(1.6, 0.5)
    write("sfx_finish", fade(mix(lowpass(stab, 4000) * env_exp(1.2, 0.5) * 0.2, cymbal * 0.6, drive(thump(0.5, 120, 35), 2))), 0.9)
    lose = np.zeros(int(SR * 2.0))
    for i, n in enumerate((67, 66, 65, 64)):
        d = 0.32 if i < 3 else 0.8
        v = lowpass(saw(note_freq(n - 12), d) + saw(note_freq(n - 12), d, 0.004), 1400) * env_adsr(d, 0.02, 0.05, 0.8, 0.08)
        if i == 3:
            v *= 1 + 0.3 * np.sin(2 * np.pi * 6 * t_axis(d))
        o = int(SR * i * 0.34)
        lose[o:o + len(v)] += v
    write("sfx_lose", fade(lose), 0.8)
    write("sfx_rank", fade(mix(drive(thump(0.5, 150, 35), 2.5), cymbal[:int(SR * 1.2)] * 0.5, at(chime([1568, 2093, 3136], 1.0, 0.35) * 0.5, 0.05))), 0.9)

    write("ui_move", fade(mix(np.sin(2 * np.pi * 1900 * t_axis(0.04)) * env_exp(0.04, 0.01), highpass(noise(0.02), 3000) * env_exp(0.02, 0.004) * 0.3)), 0.5)
    write("ui_select", fade(mix(arp([76, 83], 0.06, 0.18, "square", 0.07) * 0.5, drive(thump(0.15, 200, 80), 2) * 0.5, whoosh(0.2, 2000, 6000) * 0.3)), 0.7)
    write("ui_back", fade(arp([79, 72], 0.06, 0.15, "square", 0.06) * 0.6), 0.6)
    write("ui_whoosh", fade(mix(whoosh(0.5, 300, 7000), whoosh(0.5, 200, 1500) * 0.5)), 0.7)
    write("ui_hint", fade(arp([84, 88], 0.05, 0.12, "sine", 0.05)), 0.5)
    write("ui_locked", fade(lowpass(square(110, 0.25, 0.4), 1500) * env_adsr(0.25, 0.005, 0.05, 0.8, 0.05)), 0.6)


# ----------------------------------------------------------------------------- music

class Track:
    def __init__(self, bpm, bars, beats_per_bar=4):
        self.bpm = bpm
        self.beat = 60.0 / bpm
        self.length = self.beat * beats_per_bar * bars
        self.n = int(SR * self.length)
        self.left = np.zeros(self.n + SR * 2)
        self.right = np.zeros(self.n + SR * 2)

    def add(self, x, time, gain=1.0, pan=0.0):
        s = int(SR * time)
        e = min(s + len(x), len(self.left))
        self.left[s:e] += x[:e - s] * gain * (1 - max(pan, 0))
        self.right[s:e] += x[:e - s] * gain * (1 + min(pan, 0))

    def render(self):
        # Wrap the overflow tail back to the start so the loop is seamless.
        left = self.left[:self.n].copy()
        right = self.right[:self.n].copy()
        tail = len(self.left) - self.n
        left[:tail] += self.left[self.n:]
        right[:tail] += self.right[self.n:]
        return left, right


def kick(gain=1.0):
    d = 0.4
    body = np.sin(sweep_phase(160, 42, d, 0.25)) * env_exp(d, 0.18)
    click = highpass(noise(0.01), 3000) * env_exp(0.01, 0.002) * 0.4
    return drive(mix(body, click), 1.6) * gain


def snare():
    d = 0.25
    return mix(bandpass(noise(d), 1200, 7000) * env_exp(d, 0.07), np.sin(2 * np.pi * 190 * t_axis(0.12)) * env_exp(0.12, 0.04) * 0.6)


def clap():
    d = 0.3
    n = bandpass(noise(d), 900, 4500)
    e = np.zeros(int(SR * d))
    for o in (0, 0.012, 0.024):
        burst = at(env_exp(d - o, 0.01 if o < 0.024 else 0.09), o)[:len(e)]
        e[:len(burst)] += burst
    return n * e


def hat(open_=False):
    d = 0.18 if open_ else 0.04
    return highpass(noise(d), 7000) * env_exp(d, 0.06 if open_ else 0.012)


def bass_note(freq, d, cutoff=700, wave="saw"):
    if wave == "saw":
        v = saw(freq, d) + saw(freq, d, 0.008) * 0.8 + np.sin(2 * np.pi * freq / 2 * t_axis(d)) * 0.8
    else:
        v = np.sin(2 * np.pi * freq * t_axis(d)) + 0.3 * square(freq, d, 0.5)
    v = sweep_filter(v, cutoff * 2.5, cutoff, "low", 12)
    return drive(v * env_adsr(d, 0.004, 0.05, 0.85, 0.02), 1.4)


def supersaw(freqs, d, cutoff=3500, decay=0.25):
    v = sum(saw(f, d, det) for f in freqs for det in (-0.01, -0.004, 0.0, 0.004, 0.01))
    v = lowpass(v, cutoff) * env_exp(d, decay, 0.003)
    return v / (len(freqs) * 3)


def pad_chord(freqs, d, cutoff=1400):
    v = sum(saw(f, d, det) for f in freqs for det in (-0.007, 0.0, 0.007))
    return lowpass(v, cutoff) * env_adsr(d, d * 0.25, 0.1, 0.8, d * 0.3) / (len(freqs) * 2)


def pluck(freq, d, cutoff=4000, decay=0.12):
    v = square(freq, d, 0.3) * 0.6 + saw(freq, d, 0.003) * 0.5
    return sweep_filter(v, cutoff, 300, "low", 10) * env_exp(d, decay)


def electric_piano(freqs, d):
    t = t_axis(d)
    v = sum(np.sin(2 * np.pi * f * t + 0.8 * np.sin(2 * np.pi * f * 2 * t) * np.exp(-t * 6)) for f in freqs)
    return v * env_exp(d, 0.6) / len(freqs)


def chord(root, kind):
    shapes = {"m": [0, 3, 7], "M": [0, 4, 7], "m7": [0, 3, 7, 10], "M7": [0, 4, 7, 11], "m9": [0, 3, 7, 10, 14], "7": [0, 4, 7, 10], "sus": [0, 5, 7]}
    return [note_freq(root + i) for i in shapes[kind]]


def music_race(name, bpm, progression, key_root, seed_lead, dark=False):
    tr = Track(bpm, 16)
    b = tr.beat
    for bar in range(16):
        t0 = bar * 4 * b
        root, kind = progression[bar % len(progression)]
        # Drums : DnB two-step.
        for k in (0, 2.5):
            tr.add(kick(), t0 + k * b, 0.9)
        if bar % 4 == 3:
            tr.add(kick(), t0 + 3.5 * b, 0.6)
        for s in (1, 3):
            tr.add(snare(), t0 + s * b, 0.55)
            tr.add(clap(), t0 + s * b, 0.25)
        for i in range(16):
            tr.add(hat(open_=(i % 8 == 6)), t0 + i * b / 4, 0.16 if i % 2 else 0.24, 0.3 if i % 2 else -0.3)
        # Bass : syncopated roots.
        for step, length in ((0, 0.75), (0.75, 0.5), (1.5, 1.0), (2.5, 0.5), (3.0, 0.5), (3.5, 0.5)):
            octave = 12 if step in (3.0,) else 0
            tr.add(bass_note(note_freq(root - 24 + octave), length * b, 500 if dark else 700), t0 + step * b, 0.5)
        # Chord stabs on offbeats.
        for step in (0.5, 1.75, 3.25):
            tr.add(supersaw(chord(root, kind), 0.4 * b * 2, 3000 if not dark else 2200, 0.18), t0 + step * b, 0.35, -0.2)
        # Lead arpeggio in the second half.
        if bar >= 4:
            notes = chord(root + 12, kind)
            local = np.random.default_rng(seed_lead + bar)
            for i in range(8):
                f = notes[(i * 2 + (bar % 3)) % len(notes)] * (2 if i % 4 == 3 else 1)
                tr.add(pluck(f, b * 0.45, 5000 if not dark else 3000, 0.09), t0 + i * b / 2, 0.22, 0.35 * np.sin(i))
            if bar >= 8 and bar % 2 == 0:
                melody = [notes[local.integers(0, len(notes))] * 2 for _ in range(4)]
                for i, f in enumerate(melody):
                    tr.add(lowpass(saw(f, b * 0.9) + square(f, b * 0.9, 0.5) * 0.4, 3500) * env_adsr(b * 0.9, 0.01, 0.1, 0.7, 0.05), t0 + i * b, 0.12, 0.1)
        tr.add(pad_chord(chord(root, kind), 4 * b, 1100), t0, 0.25)
    left, right = tr.render()
    write(name, None, 0.85, (left, right))


def music_title():
    tr = Track(118, 16)
    b = tr.beat
    prog = [(57, "m9"), (53, "M7"), (55, "7"), (52, "m7")]
    for bar in range(16):
        t0 = bar * 4 * b
        root, kind = prog[bar % 4]
        for k in range(4):
            tr.add(kick(0.9), t0 + k * b, 0.8)
            tr.add(hat(), t0 + (k + 0.5) * b, 0.25, 0.25)
            if k % 2 == 1:
                tr.add(clap(), t0 + k * b, 0.35)
        for i in range(16):
            if i % 4 != 2:
                tr.add(hat(), t0 + i * b / 4, 0.08, -0.3)
        for step, length in ((0, 0.5), (0.75, 0.25), (1.5, 0.5), (2.25, 0.25), (2.75, 0.5), (3.5, 0.25)):
            tr.add(bass_note(note_freq(root - 24), length * b, 600, "sine"), t0 + step * b, 0.55)
        for step in (0.5, 2.0, 3.5):
            tr.add(electric_piano(chord(root, kind), 0.9 * b), t0 + step * b, 0.3, 0.15)
        if bar >= 8:
            notes = chord(root + 12, kind)
            for i in range(8):
                if (i + bar) % 3 != 2:
                    tr.add(pluck(notes[(i * 3) % len(notes)] * 2, b * 0.3, 3500, 0.08), t0 + i * b / 2, 0.12, -0.4 + 0.1 * i)
    left, right = tr.render()
    write("music_title", None, 0.8, (left, right))


def music_intro():
    tr = Track(174, 8)
    b = tr.beat
    for bar in range(8):
        t0 = bar * 4 * b
        root, kind = [(57, "m"), (53, "M"), (55, "M"), (52, "m")][bar % 4]
        tr.add(pad_chord(chord(root, kind), 4 * b, 900), t0, 0.5)
        for i in range(16):
            tr.add(hat(), t0 + i * b / 4, 0.1 + 0.05 * (i % 4 == 0))
        tr.add(kick(0.7), t0, 0.6)
        tr.add(bass_note(note_freq(root - 24), 2 * b, 300, "sine"), t0, 0.4)
    left, right = tr.render()
    write("music_intro", None, 0.7, (left, right))


def music_win():
    tr = Track(132, 8)
    b = tr.beat
    prog = [(60, "M7"), (65, "M7"), (62, "m7"), (67, "7")]
    for bar in range(8):
        t0 = bar * 4 * b
        root, kind = prog[bar % 4]
        for k in range(4):
            tr.add(kick(0.8), t0 + k * b, 0.7)
            tr.add(hat(True), t0 + (k + 0.5) * b, 0.18)
        tr.add(snare(), t0 + b, 0.4)
        tr.add(snare(), t0 + 3 * b, 0.4)
        tr.add(supersaw(chord(root, kind), 2 * b, 4000, 0.6), t0, 0.35)
        tr.add(supersaw(chord(root, kind), 2 * b, 4000, 0.4), t0 + 2.5 * b, 0.25)
        tr.add(bass_note(note_freq(root - 24), 1.5 * b, 900), t0, 0.45)
        tr.add(bass_note(note_freq(root - 24), 1.5 * b, 900), t0 + 2 * b, 0.45)
        notes = chord(root + 12, kind)
        for i in range(8):
            tr.add(pluck(notes[i % len(notes)] * 2, b * 0.4, 6000, 0.1), t0 + i * b / 2, 0.15, 0.3)
    left, right = tr.render()
    write("music_win", None, 0.8, (left, right))


def music_lose():
    tr = Track(84, 8)
    b = tr.beat
    prog = [(57, "m"), (53, "M7"), (50, "m7"), (52, "sus")]
    for bar in range(8):
        t0 = bar * 4 * b
        root, kind = prog[bar % 4]
        tr.add(pad_chord(chord(root, kind), 4 * b, 800), t0, 0.5)
        tr.add(electric_piano(chord(root + 12, kind), 2 * b), t0, 0.35)
        tr.add(electric_piano([note_freq(root + 24)], b), t0 + 2.5 * b, 0.2)
        tr.add(bass_note(note_freq(root - 24), 3 * b, 250, "sine"), t0, 0.4)
    left, right = tr.render()
    write("music_lose", None, 0.65, (left, right))


if __name__ == "__main__":
    make_sfx()
    music_title()
    music_intro()
    music_race("music_race", 174, [(57, "m"), (53, "M"), (48, "M"), (55, "M")], 57, 3)
    music_race("music_race2", 180, [(52, "m"), (53, "M"), (52, "m"), (50, "m7")], 52, 9, dark=True)
    music_win()
    music_lose()
