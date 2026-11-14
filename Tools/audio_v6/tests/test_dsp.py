"""DSP unit tests (pytest-compatible ; `py -3.10 -m audio_v6 test` runs them without pytest)."""
import numpy as np
from scipy.signal import windows

from audio_v6.config import SR
from audio_v6.dsp import osc, filt, dyn, loudness, nonlin, modal, motion, env, space


def _inharmonic_db(y, f0):
    n = len(y)
    Y = np.abs(np.fft.rfft(y * windows.blackmanharris(n)))
    f = np.fft.rfftfreq(n, 1.0 / SR)
    Ydb = 20 * np.log10(Y / Y.max() + 1e-20)
    mask = f > 30
    for k in range(1, int(SR / 2 / f0) + 1):
        mask &= np.abs(f - k * f0) > 40
    return Ydb[mask].max()


def test_polyblep_saw_alias():
    y = osc.saw(3500.0, SR)
    assert _inharmonic_db(y[2000:-2000], 3500.0) <= -60.0


def test_pulse_and_triangle_band_limited():
    assert _inharmonic_db(osc.pulse(3500.0, SR, 0.3)[2000:-2000], 3500.0) <= -60.0
    assert _inharmonic_db(osc.triangle(2500.0, SR)[2000:-2000], 2500.0) <= -60.0


def test_limiter_true_peak_worst_case():
    rng = np.random.default_rng(0)
    y = rng.standard_normal((SR * 3, 2)) * 0.3
    y[::997] *= 8.0                       # isolated spikes, 20 dB over
    y[:SR, 0] += 1.5 * np.sin(np.pi / 2 * np.arange(SR) + np.pi / 4)  # fs/4 inter-sample peaks
    y[SR * 2:SR * 2 + 10] = 4.0           # a square burst
    for ceiling in (-1.0, -3.0):
        out, gr = dyn.limiter(y, ceiling)
        assert loudness.true_peak(out) <= ceiling + 0.01
        assert gr > 0.0


def test_limiter_transparent_under_ceiling():
    rng = np.random.default_rng(1)
    y = rng.standard_normal((SR, 2)) * 0.05
    out, gr = dyn.limiter(y, -1.0)
    assert gr == 0.0 and np.allclose(out, y)


def test_svf_matches_biquad():
    rng = np.random.default_rng(2)
    x = rng.standard_normal(SR)
    for mode in ("lp", "hp", "notch"):
        assert np.max(np.abs(filt.svf(x, 1500.0, 1.5, mode) - filt.biquad(x, mode, 1500.0, 1.5))) < 1e-9
    assert np.max(np.abs(filt.svf(x, 1500.0, 1.5, "bpn") - filt.biquad(x, "bp", 1500.0, 1.5))) < 1e-9


def test_modulated_svf_stable_and_smooth():
    rng = np.random.default_rng(3)
    x = rng.standard_normal(SR * 2) * 0.1
    fc = np.geomspace(8000.0, 80.0, len(x))
    y = filt.svf(x, fc, 8.0, "lp")
    assert np.all(np.isfinite(y)) and np.max(np.abs(y)) < 5.0


def test_ladder_resonance_peak():
    x = np.zeros(SR)
    x[0] = 1.0
    h = filt.ladder(x, 1000.0, 0.85)
    H = np.abs(np.fft.rfft(h))
    f = np.fft.rfftfreq(len(h), 1.0 / SR)
    assert 850 < f[np.argmax(H)] < 1150


def test_karplus_strong_pitch():
    rng = np.random.default_rng(4)
    y = osc.karplus_strong(220.0, SR, rng, decay=2.0)
    Y = np.abs(np.fft.rfft(y * np.hanning(len(y))))
    f = np.fft.rfftfreq(len(y), 1.0 / SR)
    m = (f > 150) & (f < 300)
    assert abs(f[m][np.argmax(Y[m])] - 220.0) < 2.0


def test_nonlin_oversampled_alias():
    # a driven tanh of a 5 kHz sine : odd harmonics above Nyquist must not fold back loudly
    t = np.arange(SR) / SR
    y = nonlin.tanh(np.sin(2 * np.pi * 5000 * t), 4.0, 4)
    assert _inharmonic_db(y[2000:-2000], 5000.0) <= -50.0


def test_decorrelation_keeps_mono_sum():
    rng = np.random.default_rng(5)
    x = rng.standard_normal(SR)
    side = filt.decorrelate(x, 3)
    L, R = x + side, x - side
    assert np.allclose((L + R) / 2, x)
    lo = lambda s: filt.lp(filt.lp(s, 150.0), 150.0)  # noqa: E731
    c = np.corrcoef(lo(L), lo(R))[0, 1]
    assert c > 0.98


def test_reverb_ir_low_end_mono_and_decorrelated():
    ir = space.make_ir("hall")
    c = np.corrcoef(ir[:, 0], ir[:, 1])[0, 1]
    assert -0.1 < c < 0.5
    wet = space.send(np.r_[1.0, np.zeros(SR)], "hall", 0.0)
    lo = filt.lp(filt.lp(wet, 120.0), 120.0)
    assert np.sum(lo ** 2) / np.sum(wet ** 2) < 0.01


def test_modal_decay_order():
    rng = np.random.default_rng(6)
    y = modal.hit(400.0, "bar", SR, rng, decay=1.0, damp=1.0)
    assert np.all(np.isfinite(y))
    early = np.sqrt(np.mean(y[:SR // 10] ** 2))
    late = np.sqrt(np.mean(y[SR // 2:] ** 2))
    assert late < early * 0.3


def test_doppler_pass_by_pitch_drop():
    t = np.arange(SR * 2) / SR
    src = np.sin(2 * np.pi * 1000 * t)
    out = motion.pass_by(src, 40.0, 5.0, 1.0, absorb=False, ground=0.0)
    m = out.mean(axis=1)

    def f_at(seg):
        Y = np.abs(np.fft.rfft(seg * np.hanning(len(seg)), 1 << 16))
        return np.argmax(Y) * SR / (1 << 16)
    f_in = f_at(m[int(0.2 * SR):int(0.5 * SR)])
    f_out = f_at(m[int(1.5 * SR):int(1.8 * SR)])
    # approaching ~ 1000 * c / (c - v), receding ~ 1000 * c / (c + v)
    assert f_in > 1080 and f_out < 925


def test_adsr_shape():
    e = env.adsr(SR, 0.01, 0.1, 0.5, 0.2)
    assert abs(e.max() - 1.0) < 1e-3 and abs(e[int(0.5 * SR)] - 0.5) < 1e-3 and e[-1] < 1e-3
