"""Global settings of the v6 generator : sample rate, paths, seeds, loudness targets per category, world palettes."""
import os
import zlib

SR = 48000
GLOBAL_SEED = 2026_11_12
DSP_VERSION = "6.0"

PKG = os.path.dirname(os.path.abspath(__file__))
TOOLS = os.path.dirname(PKG)
PROJECT = os.path.dirname(TOOLS)
BUILD = os.path.join(PKG, "_build")
WAV_DIR = os.path.join(BUILD, "wav")
PNG_DIR = os.path.join(BUILD, "report", "png")
REPORT_DIR = os.path.join(BUILD, "report")
DEMO_DIR = os.path.join(BUILD, "demo")
CACHE_PATH = os.path.join(BUILD, "cache.json")
METRICS_DIR = os.path.join(BUILD, "metrics")
V5_DIR = os.path.join(PROJECT, "Assets", "_Game", "Resources", "Audio")
INSTALL_DIR = V5_DIR
MANIFEST_PATH = os.path.join(PROJECT, "Assets", "_Game", "Resources", "audio_manifest.json")

# ------------------------------------------------------------------ tempo (174 BPM everywhere)
BPM = 174.0
BEAT = 60.0 / BPM
BAR = 4.0 * BEAT
STEP16 = BEAT / 4.0


def seed_for(name, take=0, extra=""):
    """Per-asset seed : crc32 of (name, take, GLOBAL_SEED). Unchanged recipes give byte-identical files."""
    return zlib.crc32(("%s|%s|%s|%d" % (name, take, extra, GLOBAL_SEED)).encode("utf-8")) & 0x7FFFFFFF


# ------------------------------------------------------------------ loudness categories
# metric : "M" = max momentary (400 ms), "S" = max short-term (3 s), "I" = integrated.
# tp : true-peak ceiling dBTP. gr_max : most limiter gain reduction tolerated (dB) before the recipe is failed.
# lead_ms : most leading silence allowed (None = no check). peak_ms : time to peak (None = no check).
# crest_min : crest factor floor (dB). low40 : True = energy under 40 Hz must stay 20 dB under the total.
CATEGORIES = {
    "hero": dict(metric="M", target=-10.0, tol=1.0, tp=-1.0, gr_max=2.0, lead_ms=3.0, peak_ms=None, crest_min=None, low40=False),
    "core": dict(metric="M", target=-14.0, tol=1.0, tp=-1.0, gr_max=2.0, lead_ms=3.0, peak_ms=15.0, crest_min=None, low40=True),
    "impact": dict(metric="M", target=-14.0, tol=1.0, tp=-1.0, gr_max=2.0, lead_ms=3.0, peak_ms=None, crest_min=14.0, low40=True),
    "micro": dict(metric="M", target=-22.0, tol=2.0, tp=-3.0, gr_max=2.0, lead_ms=3.0, peak_ms=15.0, crest_min=12.0, low40=True),
    "ui": dict(metric="M", target=-20.0, tol=1.5, tp=-1.0, gr_max=2.0, lead_ms=3.0, peak_ms=None, crest_min=None, low40=True),
    "loop": dict(metric="I", target=-20.0, tol=1.0, tp=-1.0, gr_max=2.0, lead_ms=None, peak_ms=None, crest_min=None, low40=True, loop=True),
    "hero_loop": dict(metric="I", target=-20.0, tol=1.0, tp=-1.0, gr_max=2.0, lead_ms=None, peak_ms=None, crest_min=None, low40=False, loop=True),
    "engine": dict(metric="I", target=-18.0, tol=1.0, tp=-1.0, gr_max=2.0, lead_ms=None, peak_ms=None, crest_min=None, low40=True, loop=True),
    "distant": dict(metric="M", target=-20.0, tol=2.0, tp=-1.0, gr_max=2.0, lead_ms=None, peak_ms=None, crest_min=None, low40=False),
    "ambience": dict(metric="I", target=-26.0, tol=2.0, tp=-1.0, gr_max=2.0, lead_ms=None, peak_ms=None, crest_min=None, low40=True, loop=True),
    "music_full": dict(metric="I", target=-14.0, tol=0.5, tp=-1.0, gr_max=4.0, lead_ms=None, peak_ms=None, crest_min=None, low40=False, plr_min=8.0),
    "music_base": dict(metric="I", target=-16.0, tol=1.0, tp=-1.0, gr_max=None, lead_ms=None, peak_ms=None, crest_min=None, low40=False),
    "music_layer": dict(metric=None, target=None, tol=None, tp=-1.0, gr_max=None, lead_ms=None, peak_ms=None, crest_min=None, low40=False, hp150=True),
    "music_intro": dict(metric="S", target=-14.0, tol=1.0, tp=-1.0, gr_max=4.0, lead_ms=None, peak_ms=None, crest_min=None, low40=False),
    "music_title": dict(metric="I", target=-16.0, tol=0.5, tp=-1.0, gr_max=4.0, lead_ms=None, peak_ms=None, crest_min=None, low40=False, plr_min=8.0),
    "music_select": dict(metric="I", target=-17.0, tol=0.5, tp=-1.0, gr_max=4.0, lead_ms=None, peak_ms=None, crest_min=None, low40=False, plr_min=8.0),
    "stinger": dict(metric="S", target=-14.0, tol=1.0, tp=-1.0, gr_max=4.0, lead_ms=None, peak_ms=None, crest_min=None, low40=False),
}

# Short files (< 0.4 s) get a wider loudness tolerance.
SHORT_FILE_S = 0.4
SHORT_TOL = 1.5

# ------------------------------------------------------------------ worlds : key, harmony, colour
# MIDI note numbers. root = tonic of the world key (octave 2 for basses).
WORLDS = {
    1: dict(key="F minor", root=41, scale=(0, 2, 3, 5, 7, 8, 10), style="liquid / jump-up",
            # Fm9 - Dbmaj7 - Abmaj7 - Eb6/9 (roots relative to F)
            chords=[(0, "m9"), (8, "maj7"), (3, "maj7"), (10, "69")]),
    2: dict(key="G minor", root=43, scale=(0, 1, 3, 5, 7, 8, 10), style="jump-up / neuro",
            chords=[(0, "m9"), (8, "maj7"), (1, "maj7"), (10, "sus2")]),
    3: dict(key="E minor", root=40, scale=(0, 2, 3, 5, 7, 8, 10), style="dark neurofunk",
            chords=[(0, "m"), (1, "maj"), (0, "m"), (8, "maj"), (7, "maj")]),
}

# ------------------------------------------------------------------ planned game buses (mix-sim)
# Linear-domain gains the game is expected to apply (AudioManager : music layer 0.5 x MusicMaster 0.8, sfx 0.9).
BUS_GAIN_DB = {"music": -8.0, "sfx": -3.0, "loops": -3.0}
