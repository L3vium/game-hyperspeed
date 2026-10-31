"""Draws the Hyper Speed app icon (Persona-like burst + slime runner head)."""
import math
import os
import random
from PIL import Image, ImageDraw, ImageFilter

S = 1024
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Game", "Art", "Icon.png")
RED = (235, 8, 25)
BLACK = (12, 6, 14)
WHITE = (255, 255, 255)
CYAN = (64, 235, 217)
CYAN_DARK = (40, 120, 200)

random.seed(7)
img = Image.new("RGBA", (S, S), RED + (255,))
d = ImageDraw.Draw(img)
cx, cy = S * 0.55, S * 0.52

# Radial speed lines
for i in range(90):
    a = i / 90 * math.tau + random.uniform(-0.02, 0.02)
    w = random.uniform(0.006, 0.03)
    r0 = random.uniform(260, 420)
    pts = [(cx + math.cos(a) * r0, cy + math.sin(a) * r0),
           (cx + math.cos(a - w) * 1500, cy + math.sin(a - w) * 1500),
           (cx + math.cos(a + w) * 1500, cy + math.sin(a + w) * 1500)]
    d.polygon(pts, fill=WHITE if i % 3 else BLACK)

# Jagged black burst
pts = []
for i in range(40):
    a = i / 40 * math.tau
    r = 400 if i % 2 == 0 else 300 + random.uniform(-20, 20)
    pts.append((cx + math.cos(a) * r * 1.05, cy + math.sin(a) * r))
d.polygon(pts, fill=BLACK)
pts = []
for i in range(40):
    a = i / 40 * math.tau + 0.05
    r = 350 if i % 2 == 0 else 290
    pts.append((cx + math.cos(a) * r * 1.05, cy + math.sin(a) * r))
d.polygon(pts, fill=WHITE)

# Slime head (tilted, leaning into speed)
head = Image.new("RGBA", (S, S), (0, 0, 0, 0))
h = ImageDraw.Draw(head)
hx, hy, hr = S / 2, S / 2 + 40, 250
h.ellipse([hx - hr - 18, hy - hr * 0.92 - 18, hx + hr + 18, hy + hr * 0.92 + 18], fill=BLACK)
h.ellipse([hx - 70, hy - hr - 150, hx + 70, hy - hr + 40], fill=BLACK)
h.ellipse([hx - hr, hy - hr * 0.92, hx + hr, hy + hr * 0.92], fill=CYAN)
h.ellipse([hx - 52, hy - hr - 130, hx + 52, hy - hr + 30], fill=CYAN)
h.ellipse([hx - hr * 0.95, hy + 20, hx + hr * 0.95, hy + hr * 0.92], fill=CYAN_DARK)
h.ellipse([hx - hr * 0.9, hy - hr * 0.85, hx + hr * 0.9, hy + hr * 0.7], fill=CYAN)
h.ellipse([hx - 150, hy - 190, hx - 70, hy - 130], fill=WHITE)
# Goggle band
h.rectangle([hx - hr - 6, hy - 120, hx + hr + 6, hy - 70], fill=RED, outline=BLACK, width=10)
# Eyes
for sx in (-1, 1):
    ex = hx + sx * 85 + 30
    h.ellipse([ex - 48, hy - 40, ex + 48, hy + 100], fill=WHITE, outline=BLACK, width=10)
    h.ellipse([ex - 14, hy - 5, ex + 34, hy + 80], fill=BLACK)
# Determined grin
h.chord([hx - 40, hy + 95, hx + 140, hy + 190], 0, 180, fill=BLACK)
head = head.rotate(-14, resample=Image.BICUBIC, center=(hx, hy))
img.alpha_composite(head, (int(cx - S / 2), int(cy - S / 2)))

# Halftone dots in the corner
for y in range(0, 300, 26):
    for x in range(0, 300, 26):
        r = max(0, 11 - (x + y) / 50)
        if r > 0:
            d.ellipse([x - r + 12, S - y - r - 12, x + r + 12, S - y + r - 12], fill=BLACK)

# Rounded mask
mask = Image.new("L", (S, S), 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, S - 1, S - 1], radius=180, fill=255)
img.putalpha(mask)
img.save(OUT)
print("icon written", OUT)
