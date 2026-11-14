"""Mix : channels (HP / pan / width), sends (plate, hall, dotted-1/8 ping-pong) belonging to the source's stem,
analytic ducking of the music by kick / snare (never the drums), drum bus glue + parallel saturation + soft clip,
per-stem low end mono below 120 Hz, then ONE master gain curve (bus comp 1.5:1 + true-peak limiter) computed on the
full mix and applied to every stem : the stems sum exactly to the master. Everything stateful runs in steady state
over the loop (it was rendered twice, the copy that is kept has the tails of the previous pass)."""
import numpy as np

from ..config import SR, BEAT
from ..dsp import filt, dyn, nonlin, loudness, space
from ..dsp.loops import steady, steady_ctx
from ..dsp.util import undb, to_stereo, ms_decode


class Channel:
    def __init__(self, name, stem, audio, drum=False, hp=None, lp=None, pan=0.0, width=0.0, gain_db=0.0,
                 duck=0.0, sends=None, seed=1, eq=None):
        self.name, self.stem, self.audio, self.drum = name, stem, audio, drum
        self.hp, self.lp, self.pan, self.width, self.gain_db = hp, lp, pan, width, gain_db
        self.duck = duck            # dB of ducking by the kick / snare curve (0 = none)
        self.sends = sends or {}    # {"plate": dB, "hall": dB, "pingpong": dB, "cosmic": dB}
        self.seed = seed
        self.eq = eq or []          # [(kind, f, gain_db, q)]


def _process_channel(ch, duck_curve):
    x = np.asarray(ch.audio, dtype=np.float64)

    def fx(z):
        y = z
        if ch.hp:
            y = filt.hp(filt.hp(y, ch.hp, 2), ch.hp, 2)
        if ch.lp:
            y = filt.lp(y, ch.lp, 2)
        for kind, f, g, q in ch.eq:
            y = filt.apply(y, filt.rbj(kind, f, q, g))
        return y
    x = steady_ctx(fx, x, 1.0)
    x = x * undb(ch.gain_db)
    if x.ndim == 1:
        if ch.width > 0:
            side = steady_ctx(lambda z: filt.decorrelate(z, ch.seed, 20.0), x, 0.5) * ch.width
            st = ms_decode(x, side)
        else:
            st = np.stack([x, x], axis=1)
        if ch.pan:
            a = (ch.pan + 1.0) * np.pi / 4.0
            st = st * np.array([np.cos(a), np.sin(a)]) * np.sqrt(2.0)
    else:
        st = x
    if ch.duck and duck_curve is not None:
        g = duck_curve ** (ch.duck / 6.0)  # curve is built for 6 dB
        st = st * g[:, None]
    return st


def pingpong(x, delay_s, fb=0.45, lp_hz=4500.0, hp_hz=300.0):
    """Dotted-1/8 ping-pong (circular : processed over the loop in steady state). Input mono, output stereo."""
    m = np.asarray(x, dtype=np.float64)
    if m.ndim == 2:
        m = m.mean(axis=1)
    d = int(round(delay_s * SR))

    def run(z):
        n = len(z)
        L = np.zeros(n)
        R = np.zeros(n)
        tap = filt.lp(filt.hp(z, hp_hz), lp_hz)
        g = 1.0
        side = 0
        k = d
        while g > 0.02 and k < n:
            (L if side == 0 else R)[k:] += tap[:n - k] * g
            g *= fb
            side ^= 1
            k += d
        return np.stack([L, R], axis=1)
    return steady_ctx(run, m, 3.0)


def drum_glue_gain(drum_sum, thresh_db=-14.0):
    """2:1 glue on the whole kit (both stems) : one gain curve applied to every drum channel."""
    return steady_ctx(lambda z: dyn.comp_gain(z, thresh_db, 2.0, 6.0, 0.01, 0.12), drum_sum, 2.0)


def mono_low(st, fc=120.0):
    def f(z):
        lo, hi = filt.lr_split(z, fc)
        m = lo.mean(axis=1, keepdims=True)
        return hi + m
    return steady_ctx(f, st, 1.0)


def build_stems(channels, duck_curve, stem_names=("base", "hyper", "light"), hp_layers=150.0, sat=0.25,
                clip_ceiling=0.9):
    """Process channels into stem buses (pre-master)."""
    n = len(channels[0].audio)
    stems = {s: np.zeros((n, 2)) for s in stem_names}
    sends = {s: {} for s in stem_names}
    processed = {}
    # streaming : music channels go straight into their stem and sends (memory), drums are kept for the glue
    for ch in channels:
        x = _process_channel(ch, duck_curve)
        ch.audio = None
        if ch.drum:
            processed[ch.name] = x
            continue
        stems[ch.stem] += x
        for k, db in ch.sends.items():
            sends[ch.stem].setdefault(k, np.zeros((n, 2)))
            sends[ch.stem][k] += x * undb(db)
        del x
    # drum glue : one gain from the sum of every drum channel
    drums = [ch for ch in channels if ch.drum]
    if drums:
        dsum = sum(processed[ch.name] for ch in drums)
        g = drum_glue_gain(dsum)
        del dsum
        for ch in drums:
            processed[ch.name] *= g[:, None]
    # drum sub-buses per stem : parallel saturation + soft clip
    for s in stem_names:
        dbus = sum((processed[ch.name] for ch in drums if ch.stem == s), np.zeros((n, 2)))
        if np.any(dbus):
            pk = np.max(np.abs(dbus)) + 1e-12

            def drumbus(z):
                d = z + sat * nonlin.tanh(z / pk, 3.0, 4) * pk * 0.5
                return nonlin.softclip(d / pk, clip_ceiling, 4) * pk
            # oversampled shapers see the loop wrapped (their FIR edges would click at the seam otherwise)
            stems[s] += steady_ctx(drumbus, dbus, 0.05)
        for ch in drums:
            if ch.stem == s:
                for k, db in ch.sends.items():
                    sends[s].setdefault(k, np.zeros((n, 2)))
                    sends[s][k] += processed[ch.name] * undb(db)
    # sends : each stem its own returns (so the stem stays self-contained)
    for s in stem_names:
        for k, src in sends[s].items():
            if k == "pingpong":
                ret = pingpong(src, 0.75 * BEAT)
            else:
                ir = space.make_ir(k)
                tail = len(ir)

                def conv(z, ir=ir):
                    return space.convolve(z.mean(axis=1), ir)[:len(z)]
                # circular reverb : the tail of the loop end lands on the loop start
                ret = steady_ctx(conv, src, tail / SR + 0.5)
                hpf = space.PRESETS[k]["hp"]
                ret = steady_ctx(lambda z: filt.hp(filt.hp(z, hpf, 4), hpf, 4), ret, 1.0)
            stems[s] += ret
    # the base (kick / sub) : nothing useful under 28 Hz
    stems[stem_names[0]] = steady_ctx(lambda z: filt.hp(z, 33.0, 4), stems[stem_names[0]], 1.0)
    for s in stem_names:
        if s != stem_names[0] and hp_layers:
            stems[s] = steady_ctx(lambda z: filt.hp(filt.hp(z, hp_layers, 4), hp_layers, 4), stems[s], 1.0)
        stems[s] = mono_low(stems[s])
    return stems


def master(stems, target_lufs=-14.0, base_target=-16.0, ceiling=-1.0, layer_scale=1.0, base="base", adjust_base=True,
           comp_thresh_rel=-6.0):
    """Master gain curve from the full mix (bus comp 1.5:1 then true-peak limiter), applied to every stem.
    When adjust_base, the non-base stems are scaled so the base alone lands on base_target while the full mix
    lands on target_lufs. Returns (stems, mix, info)."""
    names = list(stems.keys())
    k = layer_scale
    info = {}
    for it in range(4):
        sc = {s: stems[s] * (1.0 if s == base else k) for s in names}
        full = sum(sc.values())
        L0 = loudness.integrated(full)
        pre = undb(target_lufs - L0)
        full_p = full * pre
        thr = target_lufs + comp_thresh_rel + 8.0
        gc = steady_ctx(lambda z: dyn.comp_gain(z, thr, 1.5, 8.0, 0.02, 0.25, rms_win=0.05), full_p, 3.0)
        y = full_p * gc[:, None]
        gain_mu = 0.0
        for _ in range(4):
            gl = steady_ctx(lambda z: dyn.limiter(z, ceiling, release_ms=120.0, return_gain=True)[2], y * undb(gain_mu), 1.0)
            yl = y * undb(gain_mu) * gl[:, None]
            err = target_lufs - loudness.integrated(yl)
            if abs(err) < 0.1:
                break
            gain_mu += err
        total = pre * gc * undb(gain_mu) * gl
        out = {s: sc[s] * total[:, None] for s in names}
        mixed = sum(out.values())
        lb = loudness.integrated(out[base])
        info = dict(lufs_full=loudness.integrated(mixed), lufs_base=lb, limiter_gr=float(-20 * np.log10(np.min(gl) + 1e-12)),
                    comp_gr=float(-20 * np.log10(np.min(gc) + 1e-12)), layer_scale=k)
        if not adjust_base or abs(lb - base_target) < 0.4:
            break
        # base too loud -> layers up (k grows), base too quiet -> layers down
        k *= undb((lb - base_target) * 1.6)
        k = float(np.clip(k, 0.2, 5.0))
    return out, mixed, info
