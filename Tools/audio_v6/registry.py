"""Asset registry : recipes register with @asset(...) and are expanded into render jobs (takes x worlds)."""
import fnmatch
import importlib
import os
from dataclasses import dataclass, field
from typing import Callable, Optional

import numpy as np

from .config import SR, seed_for, V5_DIR

RECIPE_MODULES = [
    "audio_v6.sfx.movement", "audio_v6.sfx.speed", "audio_v6.sfx.impacts", "audio_v6.sfx.world",
    "audio_v6.sfx.rivals", "audio_v6.sfx.race", "audio_v6.sfx.ui",
    "audio_v6.music.worlds", "audio_v6.music.cues",
]


@dataclass
class Asset:
    name: str
    fn: Callable
    category: str
    takes: int = 1
    worlds: tuple = ()
    loop: bool = False
    mono: bool = False
    group: str = "sfx"            # sfx | ui | music | loop | engine | world
    priority: int = 128           # 0 = most important (Unity convention)
    cooldown_ms: int = 0
    max_voices: int = 4
    spatial: Optional[dict] = None  # 3D settings for the game : min / max distance, rolloff, doppler
    import_class: str = "sfx_short"
    family: Optional[str] = None
    outputs: Optional[dict] = None  # multi-output recipes : {output_name: category}
    master: bool = True           # False : the recipe returns finished (mastered) audio
    install: bool = True
    cost: float = 1.0             # relative render cost (scheduling)
    notes: str = ""
    meta: dict = field(default_factory=dict)  # extra manifest fields (loop points, bars, sync points ...)


REGISTRY: dict = {}
# Legacy / convenience copies : output name -> source output name (same bytes, QA'd as the source).
ALIASES: dict = {}


def asset(name, category, **kw):
    def deco(fn):
        REGISTRY[name] = Asset(name=name, fn=fn, category=category, **kw)
        return fn
    return deco


def alias(dst, src):
    ALIASES[dst] = src


def load_all():
    for m in RECIPE_MODULES:
        try:
            importlib.import_module(m)
        except ModuleNotFoundError as e:
            if e.name != m:
                raise
    _auto_aliases()
    return REGISTRY


def _auto_aliases():
    """The game calls plain ids (AudioManager.Play("sfx_pad")) : every asset rendered as takes or per world also
    gets its plain name as a copy of take 1 / world 1 when the game already uses that id (it exists in v5), unless
    that name is already an output or an alias."""
    outs = set()
    for a in REGISTRY.values():
        if a.outputs:
            outs.update(a.outputs.keys())
    for a in REGISTRY.values():
        if a.outputs or (a.takes == 1 and not a.worlds) or a.name in ALIASES or a.name in outs:
            continue
        if not os.path.exists(os.path.join(V5_DIR, a.name + ".wav")):
            continue  # only ids the game already knows (back-compat), not every new variant family
        src = a.name + ("_w1" if a.worlds else "") + ("_1" if a.takes > 1 else "")
        ALIASES[a.name] = src
    if "music_w1_intro" in REGISTRY and "music_intro" not in ALIASES:
        ALIASES["music_intro"] = "music_w1_intro"


@dataclass
class Job:
    asset: str
    take: int
    world: Optional[int]

    def output_name(self):
        a = REGISTRY[self.asset]
        if a.outputs:
            return self.asset
        s = a.name
        if self.world is not None:
            s += "_w%d" % self.world
        if a.takes > 1:
            s += "_%d" % self.take
        return s

    def output_names(self):
        a = REGISTRY[self.asset]
        if a.outputs:
            return list(a.outputs.keys())
        return [self.output_name()]


class Ctx:
    """What a recipe receives : its seeded rng, take (1-based), world (or None), sample rate."""

    def __init__(self, job: Job):
        a = REGISTRY[job.asset]
        self.asset = a
        self.name = job.output_name()
        self.take = job.take
        self.world = job.world
        self.sr = SR
        self.seed = seed_for(a.name, job.take, "w%s" % job.world)
        self.rng = np.random.default_rng(self.seed)

    def sub_rng(self, tag):
        return np.random.default_rng(seed_for(self.asset.name, self.take, "w%s|%s" % (self.world, tag)))


def jobs(only=None, groups=None):
    out = []
    for a in REGISTRY.values():
        if groups and a.group not in groups:
            continue
        worlds = a.worlds or (None,)
        for w in worlds:
            for t in range(1, a.takes + 1):
                j = Job(a.name, t, w)
                if only:
                    names = j.output_names() + [a.name]
                    if not any(fnmatch.fnmatch(nm, pat) for nm in names for pat in only):
                        continue
                out.append(j)
    out.sort(key=lambda j: -REGISTRY[j.asset].cost)
    return out
