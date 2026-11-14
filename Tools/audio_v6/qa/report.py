"""HTML report (sortable red / green table, PNG per file, contact sheets per family, <audio> A/B rows v5 vs v6),
CSV and JSON."""
import csv
import html
import json
import os
import re

from ..config import REPORT_DIR, V5_DIR, WAV_DIR, CATEGORIES

COLS = [("name", "name"), ("category", "cat"), ("duration", "dur s"), ("lufs_i", "I"), ("lufs_m_max", "M max"),
        ("lufs_s_max", "S max"), ("true_peak", "TP"), ("limiter_gr", "GR"), ("crest", "crest"), ("plr", "PLR"),
        ("corr", "corr"), ("corr_lt150", "corr<150"), ("mono_loss_db", "mono loss"), ("e_lt40_db", "<40Hz"),
        ("centroid", "centroid"), ("lead_ms", "lead ms"), ("peak_ms", "peak ms"), ("seam_ratio", "seam")]

LEGACY = {
    r"^sfx_step_grass_run_(\d)$": r"sfx_step_\1",
    r"^music_w1_intro$": "music_intro",
    r"^music_w\d_intro$": "music_intro",
}


def v5_for(r):
    name = r["name"]
    cands = [name]
    for pat, rep in LEGACY.items():
        if re.match(pat, name):
            cands.append(re.sub(pat, rep, name))
    base = re.sub(r"(_w\d)?(_\d+)?$", "", name)
    cands += [base, base + "_1", r["asset"], r["asset"] + "_1"]
    if base.startswith("sfx_step"):
        cands.append("sfx_step_1")
    for c in cands:
        if os.path.exists(os.path.join(V5_DIR, c + ".wav")):
            return c
    return None


def _fmt(v):
    if isinstance(v, float):
        return "%.2f" % v if abs(v) < 1000 else "%.0f" % v
    return html.escape(str(v))


def write(results, family_sheets, demo_info=None, elapsed=None):
    os.makedirs(REPORT_DIR, exist_ok=True)
    results = sorted(results, key=lambda r: (r["group"], r["name"]))
    npass = sum(1 for r in results if r["passed"])
    # ---------------- CSV / JSON
    with open(os.path.join(REPORT_DIR, "qa.csv"), "w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow(["name", "category", "passed", "failed_rules"] + [c[0] for c in COLS[2:]])
        for r in results:
            m = r["metrics"]
            w.writerow([r["name"], r["category"], r["passed"], ";".join(x[0] for x in r["rules"] if not x[1])] +
                       [m.get(c[0], "") for c in COLS[2:]])
    slim = []
    for r in results:
        m = {k: v for k, v in r["metrics"].items() if k not in ("features", "momentary")}
        slim.append(dict(r, metrics=m))
    with open(os.path.join(REPORT_DIR, "qa.json"), "w", encoding="utf-8") as fh:
        from ..render import np_default
        json.dump(dict(passed=npass, total=len(results), results=slim, demo=demo_info), fh, indent=1, default=np_default)
    # ---------------- HTML
    rows = []
    for i, r in enumerate(results):
        m = r["metrics"]
        failed = [x for x in r["rules"] if not x[1]]
        waived = [x for x in r["rules"] if "WAIVED" in x[2]]
        status = "PASS" if r["passed"] else "FAIL"
        cells = []
        for key, _ in COLS:
            if key == "name":
                cells.append('<td class="nm"><a href="#d%d" onclick="tog(%d)">%s</a></td>' % (i, i, html.escape(r["name"])))
            elif key == "category":
                cells.append("<td>%s</td>" % r["category"])
            else:
                v = m.get(key, "")
                cells.append('<td data-v="%s">%s</td>' % (v if v != "" else "", _fmt(v) if v != "" else ""))
        cells.append('<td class="%s">%s%s</td>' % (status.lower(), status, " (w)" if waived else ""))
        cells.append("<td class='fr'>%s</td>" % html.escape(", ".join("%s: %s" % (x[0], x[2]) for x in failed)))
        rows.append('<tr class="r %s">%s</tr>' % (status.lower(), "".join(cells)))
        v5 = v5_for(r)
        v6src = "../wav/%s.wav" % r["name"]
        v5src = "../../../../Assets/_Game/Resources/Audio/%s.wav" % v5 if v5 else ""
        rules = "".join('<li class="%s">%s : %s</li>' % ("ok" if ok else "ko", html.escape(rn), html.escape(d))
                        for rn, ok, d in r["rules"])
        ab = ('<div class="ab"><span>v6</span><audio controls preload="none" src="%s"></audio>' % v6src)
        ab += ('<span>v5 (%s)</span><audio controls preload="none" src="%s"></audio></div>' % (v5, v5src) if v5 else
               '<span class="mut">no v5 counterpart (new id)</span></div>')
        meta = html.escape(json.dumps(r.get("meta", {})))
        rows.append('<tr id="d%d" class="det" style="display:none"><td colspan="%d">%s<img loading="lazy" src="png/%s.png">'
                    '<ul class="rules">%s</ul><div class="mut">meta %s</div></td></tr>' % (
                        i, len(COLS) + 2, ab, html.escape(r["name"]), rules, meta))
    head = "".join('<th onclick="sortBy(%d)">%s</th>' % (k, html.escape(lbl)) for k, (_, lbl) in enumerate(COLS))
    head += "<th onclick='sortBy(%d)'>status</th><th>failed rules</th>" % len(COLS)
    sheets = "".join('<figure><figcaption>%s</figcaption><img loading="lazy" src="sheets/%s.png"></figure>' % (
        html.escape(f), html.escape(f)) for f in sorted(family_sheets))
    demo = ""
    if demo_info:
        for d in demo_info:
            demo += ('<div class="demo"><h3>%s</h3><audio controls preload="none" src="../demo/%s"></audio>'
                     '<pre>%s</pre></div>' % (html.escape(d["name"]), html.escape(d["file"]), html.escape(d["summary"])))
    cats = "".join("<li><b>%s</b> : %s</li>" % (k, html.escape(json.dumps(v))) for k, v in CATEGORIES.items())
    page = TEMPLATE.format(npass=npass, total=len(results), head=head, rows="\n".join(rows), sheets=sheets, demo=demo,
                           cats=cats, elapsed=("full render %.0f s" % elapsed) if elapsed else "")
    with open(os.path.join(REPORT_DIR, "index.html"), "w", encoding="utf-8") as fh:
        fh.write(page)
    return os.path.join(REPORT_DIR, "index.html")


TEMPLATE = """<!doctype html><html><head><meta charset="utf-8"><title>Hyperspeed audio v6 QA</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
body{{font:13px/1.4 system-ui,Segoe UI,sans-serif;margin:16px;background:#fafafa;color:#222}}
table{{border-collapse:collapse;width:100%;font-size:12px}} th{{cursor:pointer;background:#eee;position:sticky;top:0}}
td,th{{padding:2px 6px;border-bottom:1px solid #ddd;text-align:right;white-space:nowrap}} td.nm,td.fr{{text-align:left}}
td.fr{{white-space:normal;color:#a00;max-width:520px}}
td.pass{{background:#c8f0c8;font-weight:600}} td.fail{{background:#f5c0c0;font-weight:600}}
tr.det td{{text-align:left;background:#fff}} tr.det img{{max-width:100%}} .ab{{display:flex;gap:10px;align-items:center;flex-wrap:wrap}}
.rules li.ok{{color:#2a7a2a}} .rules li.ko{{color:#b00;font-weight:600}} .mut{{color:#888}}
figure{{display:inline-block;margin:6px;vertical-align:top}} figure img{{max-width:900px}} .demo pre{{background:#f0f0f0;padding:6px}}
</style></head><body>
<h1>Hyperspeed audio v6 - QA report</h1>
<p><b>{npass} / {total}</b> files pass. {elapsed}. Click a name for the PNG (waveform, log spectrogram -100..0 dB,
momentary loudness vs target band, L/R correlation) and the v5 / v6 A/B players. Click a header to sort.</p>
<h2>Mix-sim demo reels</h2>{demo}
<h2>Files</h2>
<table id="t"><thead><tr>{head}</tr></thead><tbody>
{rows}
</tbody></table>
<h2>Contact sheets per family</h2>{sheets}
<h2>Category targets</h2><ul>{cats}</ul>
<script>
function tog(i){{var e=document.getElementById('d'+i);e.style.display=e.style.display=='none'?'':'none';}}
var dir=1;
function sortBy(k){{var tb=document.querySelector('#t tbody');
var ps=Array.from(tb.querySelectorAll('tr.r')).map(function(r){{return [r,r.nextElementSibling];}});
dir=-dir;ps.sort(function(p,q){{var x=p[0].children[k],y=q[0].children[k];var vx=x.dataset.v,vy=y.dataset.v;
if(vx!==undefined&&vx!==''&&vy!==undefined&&vy!==''){{return (parseFloat(vx)-parseFloat(vy))*dir;}}
return x.textContent.localeCompare(y.textContent)*dir;}});
ps.forEach(function(p){{tb.appendChild(p[0]);if(p[1])tb.appendChild(p[1]);}});}}
</script></body></html>"""
