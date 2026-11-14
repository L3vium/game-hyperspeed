"""Loudness tests against EBU Tech 3341 / 3342 reference signals."""
import numpy as np

from audio_v6.config import SR
from audio_v6.dsp import loudness


def _sine(seconds, dbfs, f=1000.0):
    t = np.arange(int(SR * seconds)) / SR
    s = np.sin(2 * np.pi * f * t) * 10 ** (dbfs / 20.0)
    return np.stack([s, s], axis=1)


def test_ebu3341_case1_stereo_sine_minus23():
    assert abs(loudness.integrated(_sine(20, -23.0)) - (-23.0)) <= 0.1


def test_ebu3341_case2_minus33():
    assert abs(loudness.integrated(_sine(20, -33.0)) - (-33.0)) <= 0.1


def test_ebu3341_case3_relative_gate():
    x = np.concatenate([_sine(10, -36.0), _sine(60, -23.0), _sine(10, -36.0)])
    assert abs(loudness.integrated(x) - (-23.0)) <= 0.1


def test_ebu3341_case4_absolute_and_relative_gate():
    x = np.concatenate([_sine(10, -72.0), _sine(10, -36.0), _sine(60, -23.0), _sine(10, -36.0), _sine(10, -72.0)])
    assert abs(loudness.integrated(x) - (-23.0)) <= 0.1


def test_ebu3341_momentary_and_short_term_constant():
    x = _sine(20, -23.0)
    assert abs(loudness.momentary_max(x) - (-23.0)) <= 0.1
    assert abs(loudness.short_term_max(x) - (-23.0)) <= 0.1


def test_kweighting_coefficients_48k():
    sos = loudness.k_sos(48000)
    assert abs(sos[0][0] - 1.53512485958697) < 1e-6
    assert abs(sos[0][4] - (-1.69065929318241)) < 1e-6
    assert abs(sos[1][4] - (-1.99004745483398)) < 1e-6


def test_true_peak_intersample():
    # fs/4 sine sampled at 45 degrees : samples at 0.5 * 0.707, true peak 0.5
    n = SR
    s = 0.5 * np.sin(np.pi / 2 * np.arange(n) + np.pi / 4)
    tp = loudness.true_peak(np.stack([s, s], axis=1))
    assert abs(tp - 20 * np.log10(0.5)) < 0.3


def test_lra_ebu3342_case1():
    # 20 s at -20 then 20 s at -30 LUFS -> LRA 10 LU (+-1)
    x = np.concatenate([_sine(20, -20.0), _sine(20, -30.0)])
    assert abs(loudness.lra(x) - 10.0) <= 1.0
