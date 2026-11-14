"""Synthesised drum kit : kick (beater click + 220 -> 55 Hz punch + sub tuned to the root), snare (modal shell
180-220 + 330 Hz, band-passed wires, crack), 808-ratio square hats, ride (~30 modes + bell), crash, toms, and a
top-loop break kit. Multi-velocity : every hit is rendered per (velocity, round robin) with seeded variation."""
import functools

import numpy as np

from ..config import SR
from ..dsp import osc, env, filt, modal, nonlin
from ..dsp.util import ns, mix


def _r(seed):
    return np.random.default_rng(seed)


@functools.lru_cache(maxsize=256)
def kick(vel_q, rr, root_hz=43.65, seed=1, tone=1.0):
    """vel_q : velocity quantised 1..8 ; rr : round robin index."""
    r = _r(seed * 1000 + vel_q * 31 + rr)
    v = vel_q / 8.0
    n = ns(0.6)
    t = np.arange(n) / SR
    # pitch : 220 Hz punch falling to 55, settling on the root (sub tuned to the key)
    f = root_hz + (55.0 - root_hz) * np.exp(-t / 0.09) + (220.0 * tone - 55.0) * np.exp(-t / (0.010 + 0.002 * r.uniform()))
    ph = np.cumsum(f) / SR
    body = np.sin(2 * np.pi * ph) * env.bp([(0, 0), (0.0008, 1.0), (0.05, 0.75, -1.0), (0.3, 0.0, -3.5)], n)
    body = nonlin.tanh(body, 1.6 + 0.6 * v, 4)
    click = filt.bp(r.standard_normal(ns(0.006)), 1500, 9000) * env.decay(ns(0.006), 0.0012, attack=0.0001)
    beater = modal.contact(0.35)
    bt = np.zeros(ns(0.004))
    bt[:len(beater)] = beater / beater.max()
    k = mix(body * (0.85 + 0.15 * v), click * (0.25 + 0.2 * v), filt.hp(bt, 800) * 0.3 * v)
    return k * (0.55 + 0.45 * v)


@functools.lru_cache(maxsize=256)
def snare(vel_q, rr, seed=2, tune=200.0, bright=1.0, wires=1.0):
    r = _r(seed * 1000 + vel_q * 31 + rr)
    v = vel_q / 8.0
    n = ns(0.45)
    shell = modal.modes(tune * r.uniform(0.98, 1.02), [1.0, 1.594, 2.136, 2.296], n, r, decay=0.16, damp=0.8,
                        amps=[1.0, 0.5, 0.35, 0.25], contact_ms=1.0 - 0.5 * v)
    ring330 = modal.modes(330.0 * r.uniform(0.99, 1.01), [1.0], n, r, decay=0.12, amps=[0.4], contact_ms=0.8)
    w = filt.bp(r.standard_normal(n), 1800, 9500 * bright) * env.bp(
        [(0, 0), (0.001, 1.0), (0.02, 0.55, -1), (0.2 + 0.08 * v, 0.0, -3)], n)
    crack = filt.hp(r.standard_normal(ns(0.004)), 2500) * env.decay(ns(0.004), 0.0008, attack=0.0001)
    s = mix(filt.hp(shell + ring330, 120) * 0.9, w * (0.45 + 0.35 * v) * wires, crack * (0.15 + 0.5 * v * bright))
    s = nonlin.tanh(s / (np.max(np.abs(s)) + 1e-12), 1.4, 4)
    return s * (0.35 + 0.65 * v)


@functools.lru_cache(maxsize=256)
def hat(vel_q, rr, open_=False, seed=3, pitch=1.0):
    """808-style : six square oscillators at the classic ratios, band-passed, enveloped."""
    r = _r(seed * 1000 + vel_q * 31 + rr + (500 if open_ else 0))
    v = vel_q / 8.0
    d = (0.32 if open_ else 0.045 + 0.02 * v) * r.uniform(0.9, 1.1)
    n = ns(d * 1.3)
    freqs = np.array([205.3, 304.4, 369.6, 522.7, 540.0, 800.0]) * 1.42 * pitch * r.uniform(0.995, 1.005)
    sq = sum(osc.pulse(f, n, 0.5, phase0=r.uniform()) for f in freqs)
    nz = r.standard_normal(n) * 0.4
    m = filt.bp(sq + nz, 7000, 15000, 2)
    m = filt.peak(m, 10500, 3.0, 1.5)
    e = env.bp([(0, 0), (0.0004, 1.0), (d * 0.25, 0.4, -1.0), (d, 0.0, -3.0)], n)
    return m * e / (np.max(np.abs(m * e)) + 1e-12) * (0.3 + 0.7 * v)


@functools.lru_cache(maxsize=64)
def ride(vel_q, rr, bell=False, seed=4):
    r = _r(seed * 1000 + vel_q * 31 + rr + (700 if bell else 0))
    v = vel_q / 8.0
    n = ns(2.2)
    ratios = np.sort(r.uniform(1.0, 26.0, 30)) ** 0.85
    body = modal.modes(380.0, ratios, n, r, decay=1.8, damp=0.35, contact_ms=0.12, fjit=0.0, phase_random=True)
    body = filt.hp(body, 2500)
    b = modal.modes(2900.0 if bell else 3400.0, [1.0, 1.51, 2.32], n, r, decay=1.2 if bell else 0.5,
                    amps=[1.0, 0.6, 0.3], contact_ms=0.1)
    tick = filt.bp(r.standard_normal(ns(0.01)), 3000, 12000) * env.decay(ns(0.01), 0.002, attack=0.0001)
    x = mix(body / (np.max(np.abs(body)) + 1e-12) * 0.6, b / (np.max(np.abs(b)) + 1e-12) * (0.6 if bell else 0.25), tick * 0.4)
    return x * (0.4 + 0.6 * v)


@functools.lru_cache(maxsize=8)
def crash(seed=5, length=3.0):
    r = _r(seed)
    n = ns(length)
    ratios = np.sort(r.uniform(1.0, 40.0, 40)) ** 0.9
    body = modal.modes(300.0, ratios, n, r, decay=length * 0.8, damp=0.3, contact_ms=0.2, fjit=0.0, phase_random=True)
    wash = filt.bp(r.standard_normal(n), 3000, 14000) * env.bp([(0, 0), (0.002, 1), (0.1, 0.5, -1), (length, 0, -4)], n)
    x = mix(filt.hp(body, 1500) / (np.max(np.abs(body)) + 1e-12) * 0.5, wash * 0.5)
    return x


@functools.lru_cache(maxsize=32)
def tom(pitch_hz, vel_q, rr, seed=6):
    r = _r(seed * 1000 + int(pitch_hz) + vel_q * 31 + rr)
    v = vel_q / 8.0
    n = ns(0.5)
    t = np.arange(n) / SR
    f = pitch_hz * (1 + 0.3 * np.exp(-t / 0.03))
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * env.decay(n, 0.12, attack=0.0008)
    sk = filt.bp(r.standard_normal(ns(0.02)), 1000, 6000) * env.decay(ns(0.02), 0.003)
    return mix(x, sk * 0.3) * (0.4 + 0.6 * v)


@functools.lru_cache(maxsize=128)
def break_hit(kind, vel_q, rr, seed=7):
    """Top-loop break kit : brighter, roomier, pre-saturated ('sampled break' character)."""
    r = _r(seed * 1000 + vel_q * 31 + rr + {"k": 1, "s": 2, "g": 3, "h": 4}[kind] * 101)
    v = vel_q / 8.0
    if kind == "k":
        x = kick(vel_q, rr, 55.0, seed + 11, 1.2)
    elif kind == "s":
        x = snare(vel_q, rr, seed + 12, 240.0, 1.3, 1.3)
    elif kind == "g":
        x = snare(max(1, vel_q - 3), rr, seed + 13, 240.0, 1.1, 1.2) * 0.5
    else:
        x = hat(vel_q, rr, False, seed + 14, 1.15)
    room = filt.lp(r.standard_normal(ns(0.08)), 6000) * env.decay(ns(0.08), 0.02) * 0.15 * v
    y = mix(x, room)
    return nonlin.tanh(y / (np.max(np.abs(y)) + 1e-12), 2.5, 4) * (0.4 + 0.6 * v)


def vq(vel):
    """Quantise a 0..1 velocity to the 1..8 layers."""
    return int(np.clip(round(vel * 8), 1, 8))
