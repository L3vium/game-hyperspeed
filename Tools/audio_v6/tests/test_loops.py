"""Loop seam tests."""
import numpy as np

from audio_v6.config import SR
from audio_v6.dsp import osc, filt, loops, stft


def test_periodic_noise_seam():
    rng = np.random.default_rng(0)
    x = osc.colored(SR * 3, rng, -3.0)
    m = loops.seam_metrics(x)
    assert m["seam_ratio"] <= 1.0 and m["seam_dip_db"] >= -1.0 and m["seam_flux_ratio"] <= 2.0


def test_steady_state_filter_is_seamless():
    rng = np.random.default_rng(1)
    x = osc.colored(SR * 2, rng, 0.0)
    y = loops.steady(lambda z: filt.lp(z, 800.0), x)
    # the steady-state loop equals what the filter outputs while the loop keeps cycling
    z = filt.lp(np.concatenate([x] * 5), 800.0)
    assert np.max(np.abs(y - z[2 * len(x):3 * len(x)])) < 1e-6
    m = loops.seam_metrics(y)
    assert m["seam_ratio"] <= 1.0


def test_naive_cut_fails_seam():
    rng = np.random.default_rng(2)
    x = filt.lp(rng.standard_normal(SR * 2), 400.0)
    x = x[: SR + 777]
    x[SR // 2:] += 4 * np.std(x)   # the classic bad loop : the end does not come back to where the start is
    assert loops.seam_metrics(x)["seam_ratio"] > 1.0


def test_circular_stft_gain_map_seamless():
    rng = np.random.default_rng(3)
    n = SR * 2
    y = stft.shaped_noise(n, lambda t, f: stft.band(f, 300.0 + 200 * np.sin(2 * np.pi * t / 2.0), 3000.0), rng,
                          circular=True)
    m = loops.seam_metrics(y)
    assert m["seam_ratio"] <= 1.0 and m["seam_dip_db"] >= -1.0


def test_circular_place_and_wrap_tail():
    buf = np.zeros(100)
    from audio_v6.dsp.util import place
    place(buf, np.ones(30), 90, circular=True)
    assert buf[90:].sum() == 10 and buf[:20].sum() == 20
    assert np.allclose(loops.wrap_tail(np.ones(250), 100), np.r_[np.full(50, 3.0), np.full(50, 2.0)])
