"""Pass / fail rules (spec thresholds) for one file, a stem set, and a family of takes."""
import numpy as np

from ..config import CATEGORIES, SHORT_FILE_S, SHORT_TOL

METRIC_KEY = {"I": "lufs_i", "M": "lufs_m_max", "S": "lufs_s_max"}


def check_file(m, category, loop=False, mono=False):
    """Returns a list of (rule, ok, detail)."""
    c = CATEGORIES[category]
    res = []

    def rule(name, ok, detail):
        res.append((name, bool(ok), detail))

    if c.get("metric"):
        key = METRIC_KEY[c["metric"]]
        tol = c["tol"]
        if m["duration"] < SHORT_FILE_S:
            tol = max(tol, SHORT_TOL)
        v = m[key]
        rule("loudness", abs(v - c["target"]) <= tol + 1e-6, "%s %.2f (target %.1f +-%.1f)" % (key, v, c["target"], tol))
    rule("true_peak", m["true_peak"] <= c["tp"] + 0.05, "%.2f dBTP (ceiling %.1f)" % (m["true_peak"], c["tp"]))
    rule("clipped", m["clipped"] == 0, "%d" % m["clipped"])
    rule("dc", m["dc"] < 0.001, "%.5f" % m["dc"])
    if c.get("gr_max") is not None:
        rule("limiter_gr", m["limiter_gr"] <= c["gr_max"] + 1e-6, "%.2f dB (max %.1f)" % (m["limiter_gr"], c["gr_max"]))
    if not mono:
        rule("corr_lt150", m["corr_lt150"] >= 0.9, "%.3f" % m["corr_lt150"])
        rule("corr", m["corr"] >= 0.0, "%.3f" % m["corr"])
    if c.get("lead_ms") is not None:
        rule("lead_silence", m["lead_ms"] <= c["lead_ms"], "%.2f ms" % m["lead_ms"])
    if c.get("peak_ms") is not None:
        rule("time_to_peak", m["peak_ms"] <= c["peak_ms"], "%.1f ms" % m["peak_ms"])
    if c.get("crest_min") is not None:
        rule("crest", m["crest"] >= c["crest_min"], "%.1f dB (min %.0f)" % (m["crest"], c["crest_min"]))
    if c.get("plr_min") is not None:
        rule("plr", m["plr"] >= c["plr_min"], "%.1f dB" % m["plr"])
    if c.get("low40"):
        rule("low40", m["e_lt40_db"] <= -20.0, "%.1f dB" % m["e_lt40_db"])
    if c.get("hp150"):
        rule("nothing_below_150", m["e_lt150_db"] <= -24.0, "%.1f dB" % m["e_lt150_db"])
    if loop or c.get("loop"):
        rule("seam_jump", m.get("seam_ratio", 0) <= 1.0, "jump %.4f vs p99 %.4f" % (m.get("seam_jump", 0), m.get("seam_p99", 0)))
        rule("seam_dip", m.get("seam_dip_db", 0) >= -1.0, "%.2f dB" % m.get("seam_dip_db", 0))
        rule("seam_flux", m.get("seam_flux_ratio", 0) <= 2.0, "%.2f x median" % m.get("seam_flux_ratio", 0))
    else:
        rule("tail_end", m["tail_end_db"] <= -50.0, "%.1f dBFS" % m["tail_end_db"])
    return res


def check_family(names, feats):
    """Take diversity : flag near-duplicates (spectral envelope and RMS envelope both almost identical)."""
    res = []
    k = len(names)
    worst = 0.0
    pair = None
    for i in range(k):
        for j in range(i + 1, k):
            a, b = np.array(feats[i]["spec"]), np.array(feats[j]["spec"])
            sd = float(np.sqrt(np.mean((a - b) ** 2)))
            ea, eb = np.array(feats[i]["env"]), np.array(feats[j]["env"])
            n = min(len(ea), len(eb))
            ec = float(np.corrcoef(ea[:n], eb[:n])[0, 1]) if n > 2 and np.std(ea[:n]) > 0 and np.std(eb[:n]) > 0 else 1.0
            sim = ec - sd / 10.0
            if sim > worst:
                worst, pair = sim, (names[i], names[j], sd, ec)
    if pair is not None:
        dup = pair[2] < 0.25 and pair[3] > 0.995
        res.append(("take_diversity", not dup, "closest %s / %s : spectral diff %.2f dB, env corr %.3f" % pair))
    return res


def check_stems(stems, master, base_m, full_m):
    """Stems : equal lengths, stem sum == master (residual < -60 dB), base and full loudness."""
    res = []
    lens = {k: len(v) for k, v in stems.items()}
    res.append(("stem_lengths", len(set(lens.values())) == 1, str(lens)))
    s = sum(stems.values())
    resid = s - master
    r = 10 * np.log10((np.mean(resid ** 2) + 1e-30) / (np.mean(master ** 2) + 1e-30))
    res.append(("stem_sum_residual", bool(r < -60.0), "%.1f dB" % r))
    from ..dsp import loudness
    names = list(stems.keys())
    acc = np.zeros_like(master)
    lv = []
    for i, k in enumerate(names):
        acc = acc + stems[k]
        lv.append("S%d %.1f" % (i, loudness.integrated(acc)))
    res.append(("stem_loudness", True, "LUFS-I : " + ", ".join(lv) + "  (S0 = base, S1 = +hyper, S2 = +light)"))
    return res
