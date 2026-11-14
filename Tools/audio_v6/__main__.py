"""Hyperspeed audio v6 generator.

    cd Tools
    py -3.10 -m audio_v6 all      [--only GLOB ...] [--jobs N] [--fast] [--force]
    py -3.10 -m audio_v6 sfx | music | qa | demo | list | test
    py -3.10 -m audio_v6 install --yes   (copies changed files that PASSED QA into Assets/_Game/Resources/Audio)

Exit code is non-zero when any file fails QA.
"""
import argparse
import fnmatch
import json
import os
import shutil
import sys
import time
from concurrent.futures import ProcessPoolExecutor, as_completed

from . import config, registry, render
from .config import WAV_DIR, METRICS_DIR, REPORT_DIR, CACHE_PATH

HEAVY_JOBS = 2


def _load_cache():
    if os.path.exists(CACHE_PATH):
        with open(CACHE_PATH, encoding="utf-8") as fh:
            return json.load(fh)
    return {}


def _save_cache(c):
    os.makedirs(os.path.dirname(CACHE_PATH), exist_ok=True)
    with open(CACHE_PATH, "w", encoding="utf-8") as fh:
        json.dump(c, fh, indent=1, sort_keys=True)


def _store(res):
    render.save_json(os.path.join(METRICS_DIR, res["name"] + ".json"), res)


def _load_result(name):
    p = os.path.join(METRICS_DIR, name + ".json")
    if os.path.exists(p):
        with open(p, encoding="utf-8") as fh:
            return json.load(fh)
    return None


def _job_tuple(j):
    return (j.asset, j.take, j.world)


def run_render(jobs, n_jobs, png, force):
    cache = _load_cache()
    keys = {a.name: render.cache_key(a) for a in registry.REGISTRY.values()}
    todo, reused = [], 0
    for j in jobs:
        names = j.output_names()
        ok = (not force) and all(
            cache.get(nm) == keys[j.asset] and os.path.exists(os.path.join(WAV_DIR, nm + ".wav")) and _load_result(nm)
            for nm in names)
        if ok:
            reused += 1
        else:
            todo.append(j)
    print("render : %d jobs (%d cached)" % (len(todo), reused), flush=True)
    t0 = time.time()
    fails = []
    if todo:
        # memory : music renders hold ~2-3 GB each, so they get their own small pool next to the SFX pool
        heavy = [j for j in todo if registry.REGISTRY[j.asset].cost >= 20]
        light = [j for j in todo if registry.REGISTRY[j.asset].cost < 20]
        n_heavy = max(1, min(HEAVY_JOBS, len(heavy)))
        ex_h = ProcessPoolExecutor(max_workers=n_heavy) if heavy else None
        ex = ProcessPoolExecutor(max_workers=max(1, n_jobs - (n_heavy if heavy else 0)))
        futs = {ex_h.submit(render.render_job, _job_tuple(j), png): j for j in heavy} if heavy else {}
        futs.update({ex.submit(render.render_job, _job_tuple(j), png): j for j in light})
        try:
            for k, f in enumerate(as_completed(futs)):
                j = futs[f]
                try:
                    res = f.result()
                except Exception as e:  # report and continue
                    import traceback
                    traceback.print_exc()
                    fails.append((j.output_name(), repr(e)))
                    continue
                for r in res:
                    _store(r)
                    cache[r["name"]] = keys[j.asset]
                    bad = [x[0] for x in r["rules"] if not x[1]]
                    print("[%3d/%d] %-34s %s %5.1fs %s" % (k + 1, len(todo), r["name"], "PASS" if r["passed"] else "FAIL",
                                                           r["render_s"], ",".join(bad)), flush=True)
                if k % 10 == 0:
                    _save_cache(cache)
        finally:
            ex.shutdown()
            if ex_h:
                ex_h.shutdown()
    _save_cache(cache)
    print("render time %.1f s" % (time.time() - t0), flush=True)
    return fails, time.time() - t0


def apply_aliases():
    out = []
    for dst, src in registry.ALIASES.items():
        sp = os.path.join(WAV_DIR, src + ".wav")
        r = _load_result(src)
        if os.path.exists(sp) and r:
            shutil.copyfile(sp, os.path.join(WAV_DIR, dst + ".wav"))
            r2 = dict(r, name=dst, meta=dict(r.get("meta", {}), alias_of=src))
            out.append(r2)
            _store(r2)
    return out


def collect_results():
    """Every stored result whose output still belongs to a registered asset."""
    names = set()
    for j in registry.jobs():
        names.update(j.output_names())
    names.update(registry.ALIASES.keys())
    res = []
    for nm in sorted(names):
        r = _load_result(nm)
        if r and os.path.exists(os.path.join(WAV_DIR, nm + ".wav")):
            res.append(r)
    return res


def family_checks(results):
    from .qa import checks
    fams = {}
    for r in results:
        if r.get("meta", {}).get("alias_of"):
            continue
        if r["takes"] > 1:
            fams.setdefault((r["asset"], r["world"]), []).append(r)
    for (asset, world), rs in fams.items():
        rs.sort(key=lambda r: r["name"])
        rule = checks.check_family([r["name"] for r in rs], [r["metrics"]["features"] for r in rs])
        for r in rs:
            r["rules"] = [x for x in r["rules"] if x[0] != "take_diversity"] + [list(x) for x in rule]
            r["passed"] = all(x[1] for x in r["rules"])
    return results


def contact_sheets(results, n_jobs):
    fams = {}
    for r in results:
        if r.get("meta", {}).get("alias_of") or r["group"] == "music":
            continue
        fams.setdefault(r["family"], []).append(r["name"])
    fams = {k: sorted(v) for k, v in fams.items() if len(v) > 1}
    with ProcessPoolExecutor(max_workers=n_jobs) as ex:
        list(ex.map(render.sheet_job, list(fams.items())))
    return list(fams.keys())


def build_report(n_jobs, png, elapsed=None):
    from .qa import report
    results = family_checks(collect_results())
    for r in results:
        _store(r)
    sheets = contact_sheets(results, n_jobs) if png else []
    demo = None
    dp = os.path.join(config.DEMO_DIR, "demo.json")
    if os.path.exists(dp):
        with open(dp, encoding="utf-8") as fh:
            demo = json.load(fh)
    path = report.write(results, sheets, demo, elapsed)
    npass = sum(1 for r in results if r["passed"])
    print("report : %s  (%d / %d pass)" % (path, npass, len(results)))
    for r in results:
        if not r["passed"]:
            print("  FAIL %-34s %s" % (r["name"], "; ".join("%s %s" % (x[0], x[2]) for x in r["rules"] if not x[1])))
    return npass == len(results)


def run_qa(jobs, n_jobs, png):
    t0 = time.time()
    with ProcessPoolExecutor(max_workers=n_jobs) as ex:
        futs = [ex.submit(render.requalify, _job_tuple(j), png) for j in jobs]
        for f in as_completed(futs):
            for r in f.result():
                old = _load_result(r["name"])
                if old:
                    r["metrics"]["limiter_gr"] = old["metrics"].get("limiter_gr", 0.0)
                    r["render_s"] = old.get("render_s", 0.0)
                    from .qa import checks
                    a = registry.REGISTRY[r["asset"]]
                    rules = checks.check_file(r["metrics"], r["category"], a.loop, a.mono)
                    waived = a.meta.get("waive", {})
                    r["rules"] = [[n, ok or n in waived, d + ("  [WAIVED : %s]" % waived[n] if (not ok and n in waived) else "")]
                                  for n, ok, d in rules] + [x for x in r["rules"] if x[0] in ("stem_lengths", "stem_sum_residual", "stem_loudness")]
                    r["passed"] = all(x[1] for x in r["rules"])
                _store(r)
    print("qa time %.1f s" % (time.time() - t0))


def install(yes):
    """Copy changed, passing files into the game and write the manifest. Guarded by --yes."""
    if not yes:
        print("install : refusing without --yes (this writes into Assets/_Game/Resources)")
        return False
    results = family_checks(collect_results())
    os.makedirs(config.INSTALL_DIR, exist_ok=True)
    manifest = {}
    copied = 0
    for r in results:
        if not r.get("install", True) or r["name"] in r.get("meta", {}).get("no_install", []):
            continue
        if not r["passed"]:
            print("skip (QA fail) %s" % r["name"])
            continue
        src = os.path.join(WAV_DIR, r["name"] + ".wav")
        dst = os.path.join(config.INSTALL_DIR, r["name"] + ".wav")
        with open(src, "rb") as fh:
            data = fh.read()
        if not os.path.exists(dst) or open(dst, "rb").read() != data:
            with open(dst, "wb") as fh:
                fh.write(data)
            copied += 1
        manifest[r["name"]] = dict(group=r["group"], category=r["category"], priority=r["priority"], takes=r["takes"],
                                   cooldown_ms=r["cooldown_ms"], max_voices=r["max_voices"], spatial=r["spatial"],
                                   loop=r["loop"], world_keyed=bool(r["worlds"]), import_class=r["import_class"],
                                   **{k: v for k, v in r.get("meta", {}).items() if k not in ("waive", "no_install")})
    with open(config.MANIFEST_PATH, "w", encoding="utf-8") as fh:
        json.dump(manifest, fh, indent=1, sort_keys=True)
    print("install : %d files copied, manifest %s" % (copied, config.MANIFEST_PATH))
    return True


def run_tests():
    """pytest when available, else a minimal runner over tests/test_*.py functions."""
    try:
        import pytest  # noqa: F401
        return pytest.main([os.path.join(config.PKG, "tests"), "-q"]) == 0
    except ImportError:
        pass
    import importlib
    import traceback
    ok = True
    for mod in ("test_dsp", "test_loudness", "test_loops"):
        m = importlib.import_module("audio_v6.tests." + mod)
        for name in sorted(dir(m)):
            if name.startswith("test_"):
                t0 = time.time()
                try:
                    getattr(m, name)()
                    print("PASS %s.%s (%.2fs)" % (mod, name, time.time() - t0))
                except Exception:
                    ok = False
                    print("FAIL %s.%s" % (mod, name))
                    traceback.print_exc()
    return ok


def main(argv=None):
    p = argparse.ArgumentParser(prog="audio_v6")
    p.add_argument("cmd", choices=["all", "sfx", "music", "qa", "demo", "install", "list", "test", "report"])
    p.add_argument("--only", nargs="*", default=None, help="glob(s) on output or asset names")
    p.add_argument("--jobs", type=int, default=max(1, min(14, (os.cpu_count() or 4) - 2)))
    p.add_argument("--heavy", type=int, default=2, help="parallel music renders (memory ~3 GB each)")
    p.add_argument("--fast", action="store_true", help="skip PNGs and contact sheets")
    p.add_argument("--force", action="store_true", help="ignore the render cache")
    p.add_argument("--yes", action="store_true")
    a = p.parse_args(argv)
    global HEAVY_JOBS
    HEAVY_JOBS = a.heavy
    registry.load_all()
    png = not a.fast
    if a.cmd == "test":
        return 0 if run_tests() else 1
    if a.cmd == "list":
        for j in registry.jobs(a.only):
            print(", ".join(j.output_names()), "  [%s]" % registry.REGISTRY[j.asset].category)
        print("aliases :", registry.ALIASES)
        return 0
    if a.cmd == "install":
        return 0 if install(a.yes) else 1
    t0 = time.time()
    fails = []
    if a.cmd in ("all", "sfx", "music"):
        groups = None
        if a.cmd == "sfx":
            groups = [g for g in {x.group for x in registry.REGISTRY.values()} if g != "music"]
        if a.cmd == "music":
            groups = ["music"]
        jobs = registry.jobs(a.only, groups)
        fails, _ = run_render(jobs, a.jobs, png, a.force)
        apply_aliases()
    if a.cmd == "qa":
        run_qa(registry.jobs(a.only), a.jobs, png)
        apply_aliases()
    if a.cmd in ("all", "demo"):
        from .qa import mixsim
        mixsim.build_all()
    elapsed = time.time() - t0
    ok = build_report(a.jobs, png, elapsed if a.cmd == "all" else None)
    for nm, e in fails:
        print("ERROR %s : %s" % (nm, e))
    print("total %.1f s" % elapsed)
    return 0 if (ok and not fails) else 1


if __name__ == "__main__":
    sys.exit(main())
