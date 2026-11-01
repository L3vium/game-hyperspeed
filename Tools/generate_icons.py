"""White UI icons on transparent background for the Hyper Speed menus.

    python Tools/generate_icons.py
"""
import math
import os
from PIL import Image, ImageDraw

S = 256
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Game", "Art", "Icons")
W = (255, 255, 255, 255)


def canvas():
    img = Image.new("RGBA", (S * 2, S * 2), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    img.resize((S, S), Image.LANCZOS).save(os.path.join(OUT, "icon_" + name + ".png"))
    print("icon", name)


def gear():
    img, d = canvas()
    c = S
    teeth = 10
    pts = []
    for i in range(teeth * 4):
        a = i / (teeth * 4) * math.tau
        r = 210 if (i % 4) in (1, 2) else 165
        pts.append((c + math.cos(a) * r, c + math.sin(a) * r))
    d.polygon(pts, fill=W)
    d.ellipse([c - 80, c - 80, c + 80, c + 80], fill=(0, 0, 0, 0))
    save(img, "gear")


def trophy():
    img, d = canvas()
    d.rectangle([170, 380, 342, 420], fill=W)
    d.rectangle([228, 300, 284, 390], fill=W)
    d.pieslice([130, -10, 382, 330], 0, 180, fill=W)
    d.rectangle([130, 60, 382, 170], fill=W)
    for x0, x1 in ((60, 170), (342, 452)):
        d.arc([x0, 70, x1, 230], 0, 360, fill=W, width=26)
    d.rectangle([130, 60, 382, 90], fill=W)
    save(img, "trophy")


def lock():
    img, d = canvas()
    d.rounded_rectangle([110, 220, 402, 450], radius=30, fill=W)
    d.arc([160, 70, 352, 300], 180, 360, fill=W, width=44)
    d.rectangle([160, 180, 204, 240], fill=W)
    d.rectangle([308, 180, 352, 240], fill=W)
    d.ellipse([230, 300, 282, 352], fill=(0, 0, 0, 0))
    d.rectangle([246, 330, 266, 400], fill=(0, 0, 0, 0))
    save(img, "lock")


def chevron(name, flip):
    img, d = canvas()
    pts = [(120, 60), (260, 60), (400, 256), (260, 452), (120, 452), (260, 256)]
    if flip:
        pts = [(S * 2 - x, y) for x, y in pts]
    d.polygon(pts, fill=W)
    save(img, name)


def menu():
    img, d = canvas()
    for y in (110, 230, 350):
        d.polygon([(90, y), (430, y), (400, y + 60), (60, y + 60)], fill=W)
    save(img, "menu")


def note():
    img, d = canvas()
    d.ellipse([90, 330, 230, 440], fill=W)
    d.ellipse([290, 290, 430, 400], fill=W)
    d.rectangle([196, 110, 230, 390], fill=W)
    d.rectangle([396, 70, 430, 350], fill=W)
    d.polygon([(196, 110), (430, 50), (430, 120), (196, 180)], fill=W)
    save(img, "note")


def flag():
    img, d = canvas()
    d.rectangle([110, 60, 140, 460], fill=W)
    for row in range(4):
        for col in range(4):
            if (row + col) % 2 == 0:
                d.rectangle([140 + col * 70, 70 + row * 55, 210 + col * 70, 125 + row * 55], fill=W)
    d.rectangle([140, 70, 420, 290], outline=W, width=10)
    save(img, "flag")


def play():
    img, d = canvas()
    d.polygon([(150, 80), (440, 256), (150, 432)], fill=W)
    save(img, "play")


def bolt():
    img, d = canvas()
    d.polygon([(300, 30), (120, 290), (240, 290), (190, 482), (400, 200), (270, 200)], fill=W)
    save(img, "bolt")


if __name__ == "__main__":
    gear()
    trophy()
    lock()
    chevron("arrow_right", False)
    chevron("arrow_left", True)
    menu()
    note()
    flag()
    play()
    bolt()
