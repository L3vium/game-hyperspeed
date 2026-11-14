"""Render pipeline for one job : recipe -> mastering (loudness target + true-peak limiter) -> 16-bit TPDF-dithered
WAV -> QA metrics and checks on the written file -> PNG. Runs inside worker processes."""
import glob
import hashlib
import inspect
import json
import os
import time

import numpy as np
from scipy.io import wavfile

from . import config, registry
from .config import SR, CATEGORIES, WAV_DIR, PNG_DIR, PKG
from .dsp import dyn, loudness, filt
from .dsp.loops import steady
from .dsp.util import undb, to_stereo
from .qa import metrics as qm, checks as qc


# ------------------------------------------------------------------ files

def write_wav(path, x, seed):
    """16-bit with TPDF dither (seeded : identical recipes give identical bytes)."""
    x = np.asarray(x, dtype=np.float64)
    rng = np.random.default_rng(seed)
    d = (rng.uniform(-0.5, 0.5, x.shape) + rng.uniform(-0.5, 0.5, x.shape))
    q = np.clip(np.round(x * 32767.0 + d), -32768, 32767).astype(np.int16)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    wavfile.write(path, SR, q)
    return q.astype(np.float64) / 32768.0


def read_wav(path):
    sr, q = wavfile.read(path)
    if q.dtype == np.int16:
        return q.astype(np.float64) / 32768.0, sr
    return q.astype(np.float64), sr


# ------------------------------------------------------------------ cache keys

_COMMON_SOURCES = None


def _common_hash():
    global _COMMON_SOURCES
    if _COMMON_SOURCES is None:
        h = hashlib.sha1()
        files = sorted(glob.glob(os.path.join(PKG, "dsp", "*.py"))) + [
            os.path.join(PKG, "config.py"), os.path.join(PKG, "render.py"), os.path.join(PKG, "sfx", "layers.py")] + \
            sorted(glob.glob(os.path.join(PKG, "music", "*.py")))
        for f in files:
            if os.path.exists(f):
                with open(f, "rb") as fh:
                    h.update(fh.read())
        _COMMON_SOURCES = h.hexdigest()
    return _COMMON_SOURCES


def cache_key(asset):
    h = hashlib.sha1(_common_hash().encode())
    mod = inspect.getsourcefile(asset.fn)
    with open(mod, "rb") as fh:
        h.update(fh.read())
    h.update(repr((asset.category, asset.takes, asset.worlds, asset.loop, asset.mono, config.GLOBAL_SEED)).encode())
    return h.hexdigest()


# ------------------------------------------------------------------ mastering

def master(x, category, loop=False):
    """Gain to the category target, then the true-peak limiter ; re-aim until within 0.05 LU.
    Returns (audio, limiter_gr_db)."""
    c = CATEGORIES[category]
    x = np.asarray(x, dtype=np.float64)
    if loop:
        x = steady(lambda z: filt.dc_block(z, 10.0), x)
    else:
        x = filt.dc_block(x, 10.0)
    if c["metric"] is None:
        y, gr = dyn.limiter(x, c["tp"])
        return y, gr
    gain = c["target"] - loudness.measure(x, c["metric"])
    y, gr = x, 0.0
    # one-shots : short release, so the (small) peak control does not pull the body's loudness down
    rel = 80.0 if (loop or category.startswith("music") or category == "stinger") else 20.0
    for _ in range(5):
        z = x * undb(gain)
        if loop:
            res = {}

            def lim(w):
                out, g = dyn.limiter(w, c["tp"], release_ms=rel)
                res["gr"] = g
                return out
            y = steady(lim, z)
            gr = res["gr"]
        else:
            y, gr = dyn.limiter(z, c["tp"], release_ms=rel)
        err = c["target"] - loudness.measure(y, c["metric"])
        if abs(err) < 0.05:
            break
        gain += err
    return y, gr


# ------------------------------------------------------------------ one job

def _result(name, a, job, category, x, gr, render_s, extra_checks=(), png=True):
    st = x
    m = qm.compute(st, SR, loop=a.loop, gr=gr)
    rules = qc.check_file(m, category, loop=a.loop, mono=(x.ndim == 1))
    rules += list(extra_checks)
    waived = a.meta.get("waive", {})
    out_rules = []
    for r, ok, d in rules:
        if not ok and r in waived:
            out_rules.append((r, True, d + "  [WAIVED : %s]" % waived[r]))
        else:
            out_rules.append((r, ok, d))
    passed = all(ok for _, ok, _ in out_rules)
    if png:
        try:
            from .qa import plots
            plots.file_png(os.path.join(PNG_DIR, name + ".png"), name, st, m, category)
        except Exception as e:  # plotting must never fail a render
            m["png_error"] = str(e)
    return dict(name=name, asset=a.name, category=category, group=a.group, family=a.family or a.name,
                take=job.take, world=job.world, loop=a.loop, mono=a.mono, metrics=m,
                rules=[list(r) for r in out_rules], passed=passed, render_s=render_s, install=a.install,
                meta=a.meta, priority=a.priority, cooldown_ms=a.cooldown_ms, max_voices=a.max_voices,
                spatial=a.spatial, import_class=a.import_class, takes=a.takes, worlds=list(a.worlds))


def render_job(job_tuple, png=True):
    registry.load_all()
    job = registry.Job(*job_tuple)
    a = registry.REGISTRY[job.asset]
    ctx = registry.Ctx(job)
    t0 = time.time()
    out = a.fn(ctx)
    results = []
    if a.outputs:
        written = {}
        grs = out.pop("_gr", {}) if isinstance(out, dict) else {}
        for name, x in out.items():
            cat = a.outputs[name]
            gr = grs.get(name, 0.0)
            if a.master:
                x, gr = master(x, cat, a.loop)
            written[name] = write_wav(os.path.join(WAV_DIR, name + ".wav"), x, config.seed_for(name, 0, "dither"))
            written[name + "/gr"] = gr
        dt = time.time() - t0
        stems = a.meta.get("stems")
        mix_name = a.meta.get("mix")
        for name in out.keys():
            extra = []
            if stems and name == mix_name:
                extra = qc.check_stems({s: written[s] for s in stems}, written[mix_name], None, None)
            results.append(_result(name, a, job, a.outputs[name], written[name], written[name + "/gr"], dt, extra, png))
        return results
    x = np.asarray(out, dtype=np.float64)
    if a.mono and x.ndim == 2:
        x = x.mean(axis=1)
    if not a.mono and x.ndim == 1:
        x = to_stereo(x)
    gr = 0.0
    if a.master:
        x, gr = master(x, a.category, a.loop)
    name = ctx.name
    xw = write_wav(os.path.join(WAV_DIR, name + ".wav"), x, config.seed_for(name, 0, "dither"))
    results.append(_result(name, a, job, a.category, xw, gr, time.time() - t0, (), png))
    return results


def requalify(job_tuple, png=True):
    """QA only, on the WAV already written (no render)."""
    registry.load_all()
    job = registry.Job(*job_tuple)
    a = registry.REGISTRY[job.asset]
    results = []
    names = job.output_names()
    loaded = {}
    for name in names:
        p = os.path.join(WAV_DIR, name + ".wav")
        if os.path.exists(p):
            loaded[name] = read_wav(p)[0]
    for name, x in loaded.items():
        cat = a.outputs[name] if a.outputs else a.category
        extra = []
        stems = a.meta.get("stems")
        if stems and name == a.meta.get("mix") and all(s in loaded for s in stems):
            extra = qc.check_stems({s: loaded[s] for s in stems}, x, None, None)
        results.append(_result(name, a, job, cat, x, 0.0, 0.0, extra, png))
    return results


def sheet_job(args):
    """Contact sheet of one family (worker)."""
    fam, items = args
    from .qa import plots
    audio = [(nm, read_wav(os.path.join(WAV_DIR, nm + ".wav"))[0]) for nm in items]
    plots.contact_sheet(os.path.join(config.REPORT_DIR, "sheets", fam + ".png"), fam, audio)
    return fam


def np_default(o):
    """JSON fallback for numpy scalars / arrays."""
    if isinstance(o, np.generic):
        return o.item()
    if isinstance(o, np.ndarray):
        return o.tolist()
    raise TypeError(type(o))


def save_json(path, obj):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(obj, fh, indent=1, default=np_default)
