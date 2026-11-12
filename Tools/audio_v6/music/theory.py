"""Harmony : chord shapes, voice-led voicings (minimal movement), scales, THE leitmotif (shared by title, world leads,
win cue) and the bass riffs."""
import itertools

import numpy as np

CHORDS = {
    "m": [0, 3, 7], "maj": [0, 4, 7], "m7": [0, 3, 7, 10], "m9": [0, 3, 7, 10, 14], "m11": [0, 3, 7, 10, 14, 17],
    "maj7": [0, 4, 7, 11], "maj9": [0, 4, 7, 11, 14], "69": [0, 4, 7, 9, 14], "add9": [0, 4, 7, 14],
    "sus2": [0, 2, 7], "sus4": [0, 5, 7], "7": [0, 4, 7, 10], "7b9": [0, 4, 7, 10, 13], "m6": [0, 3, 7, 9],
}
# tones a voicing must keep (indices into the shape) : 3rd and 7th / 6th / 9th colour
ESSENTIAL = {
    "m": [1, 2], "maj": [1, 2], "m7": [1, 3], "m9": [1, 3, 4], "m11": [1, 3, 4], "maj7": [1, 3], "maj9": [1, 3, 4],
    "69": [1, 3, 4], "add9": [1, 3], "sus2": [1, 2], "sus4": [1, 2], "7": [1, 3], "7b9": [1, 3, 4], "m6": [1, 3],
}


def hz(m):
    return 440.0 * 2 ** ((np.asarray(m, dtype=np.float64) - 69.0) / 12.0)


def chord_pcs(root, kind):
    return [(root + i) % 12 for i in CHORDS[kind]]


def voicing(root, kind, prev=None, lo=53, hi=79, voices=5):
    """A voicing of `voices` notes in [lo, hi] keeping the essential tones, no cluster in the low register,
    minimising the movement from `prev` (or centred in the register when prev is None)."""
    pcs = chord_pcs(root, kind)
    ess = {pcs[i] for i in ESSENTIAL[kind]}
    cands = [m for m in range(lo, hi + 1) if m % 12 in pcs]
    best, best_cost = None, 1e9
    k = min(voices, len(set(pcs)) + 2)
    for combo in itertools.combinations(cands, k):
        got = {m % 12 for m in combo}
        if not ess.issubset(got):
            continue
        ok = True
        for a, b in zip(combo[:-1], combo[1:]):
            if b - a < 2 or (a < 60 and b - a < 5) or b - a > 9:
                ok = False
                break
        if not ok:
            continue
        if prev is None:
            cost = abs(np.mean(combo) - (lo + hi) / 2.0) + 0.1 * (combo[-1] - combo[0])
        else:
            p = sorted(prev)
            c = sorted(combo)
            if len(p) == len(c):
                cost = sum(abs(a - b) for a, b in zip(p, c))
            else:
                cost = sum(min(abs(a - b) for b in p) for a in c)
            cost += 0.05 * abs(np.mean(combo) - (lo + hi) / 2.0)
        if cost < best_cost:
            best, best_cost = list(combo), cost
    if best is None:
        best = sorted([root + i for i in CHORDS[kind]])
        while best[0] < lo:
            best = [m + 12 for m in best]
    return best


def voice_lead(progression, root_midi, lo=53, hi=79, voices=5):
    """Voicings for a progression [(root offset, kind)], each led smoothly from the previous one (cyclic : the
    first is re-led from the last so the loop seam moves as little as the rest)."""
    out = []
    prev = None
    for off, kind in progression:
        v = voicing(root_midi + off, kind, prev, lo, hi, voices)
        out.append(v)
        prev = v
    out[0] = voicing(root_midi + progression[0][0], progression[0][1], out[-1], lo, hi, voices)
    return out


def degree(scale, root, d):
    """MIDI note of scale degree d (0-based, any integer) above root."""
    return root + scale[d % 7] + 12 * (d // 7)


# ------------------------------------------------------------------ THE leitmotif
# (degree, start in 16ths, length in 16ths) over a 2-bar phrase ; A states, B answers. Grouping 3+3+4 (the dotted
# drum & bass lead rhythm). Degrees in the natural minor of the world (0 = tonic, 7 = octave).
MOTIF_A = [(4, 0, 3), (6, 3, 3), (7, 6, 4), (6, 10, 2), (4, 12, 4), (3, 16, 3), (4, 19, 3), (2, 22, 6), (1, 28, 2), (2, 30, 2)]
MOTIF_B = [(4, 0, 3), (6, 3, 3), (7, 6, 4), (9, 10, 2), (8, 12, 4), (7, 16, 3), (6, 19, 3), (7, 22, 10)]
# a brighter cadence for the win cue : climbs to the 3rd above the octave and rests on the octave
MOTIF_WIN = [(4, 0, 3), (6, 3, 3), (7, 6, 4), (9, 10, 2), (11, 12, 4), (9, 16, 3), (8, 19, 3), (7, 22, 10)]


def motif_notes(phrase, scale, root, transpose_deg=0):
    """[(midi, start16, len16)]."""
    return [(degree(scale, root, d + transpose_deg), s, l) for d, s, l in phrase]


# ------------------------------------------------------------------ bass riffs (semitones from the chord root, 16ths)
# Two bars per chord. Syncopated, two-step friendly : notes avoid sitting on every kick.
RIFF_ROLL = [(0, 0, 6), (0, 7, 2), (12, 10, 2), (0, 12, 3), (0, 16, 5), (-2, 22, 2), (0, 24, 3), (3, 28, 3)]
RIFF_JUMP = [(0, 0, 3), (12, 3, 1), (0, 6, 2), (0, 10, 4), (7, 14, 2), (0, 16, 3), (12, 19, 1), (0, 22, 4), (-5, 26, 2), (-2, 28, 4)]
RIFF_LONG = [(0, 0, 14), (-2, 14, 2), (0, 16, 16)]
RIFF_BUILD = [(0, i * 2, 2) for i in range(16)]
