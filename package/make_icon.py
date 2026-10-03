"""Draws package/icon.png (256x256, required by Thunderstore): two crossed oars over waves.

Run with: python3 package/make_icon.py   (needs Pillow)
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

SIZE = 256
SCALE = 4  # draw large, then downsample for smooth edges
S = SIZE * SCALE

SEA_TOP = (18, 42, 66)
SEA_BOTTOM = (8, 20, 34)
WAVE = (46, 104, 140)
WAVE_LIGHT = (92, 160, 190)
WOOD = (196, 140, 78)
WOOD_DARK = (120, 78, 38)


def lerp(a, b, t):
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))


def rotate(points, angle, center):
    cx, cy = center
    c, s = math.cos(angle), math.sin(angle)
    return [(cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c) for x, y in points]


def draw_oar(draw, angle):
    """An oar centred on the canvas: shaft with a grip at the top and a blade at the bottom."""
    cx, cy = S / 2, S / 2 - 8 * SCALE
    length = 200 * SCALE
    shaft_w = 9 * SCALE
    top, bottom = cy - length / 2, cy + length / 2
    blade_top = bottom - 64 * SCALE
    blade_w = 30 * SCALE

    shaft = [(cx - shaft_w / 2, top + 14 * SCALE), (cx + shaft_w / 2, top + 14 * SCALE),
             (cx + shaft_w / 2, blade_top), (cx - shaft_w / 2, blade_top)]
    grip = [(cx - shaft_w * 0.8, top), (cx + shaft_w * 0.8, top),
            (cx + shaft_w * 0.8, top + 20 * SCALE), (cx - shaft_w * 0.8, top + 20 * SCALE)]
    blade = [(cx - shaft_w / 2, blade_top - 10 * SCALE), (cx + shaft_w / 2, blade_top - 10 * SCALE),
             (cx + blade_w / 2, blade_top + 14 * SCALE), (cx + blade_w / 2, bottom - 8 * SCALE),
             (cx, bottom), (cx - blade_w / 2, bottom - 8 * SCALE), (cx - blade_w / 2, blade_top + 14 * SCALE)]

    for shape, fill in ((shaft, WOOD), (grip, WOOD_DARK), (blade, WOOD)):
        draw.polygon(rotate(shape, angle, (cx, cy)), fill=fill, outline=WOOD_DARK, width=3 * SCALE)
    # A dark stripe down the blade for depth.
    stripe = [(cx - 2 * SCALE, blade_top), (cx + 2 * SCALE, blade_top), (cx + 2 * SCALE, bottom - 12 * SCALE), (cx - 2 * SCALE, bottom - 12 * SCALE)]
    draw.polygon(rotate(stripe, angle, (cx, cy)), fill=WOOD_DARK)


def draw_waves(draw, base_y, amplitude, color, phase):
    points = [(0, S)]
    for x in range(0, S + 1, SCALE):
        points.append((x, base_y + amplitude * math.sin(x / S * 2 * math.pi * 2.5 + phase)))
    points.append((S, S))
    draw.polygon(points, fill=color)


def main():
    img = Image.new("RGB", (S, S))
    draw = ImageDraw.Draw(img)
    for y in range(S):
        draw.line([(0, y), (S, y)], fill=lerp(SEA_TOP, SEA_BOTTOM, y / S))

    # Soft glow behind the oars.
    glow = Image.new("L", (S, S), 0)
    ImageDraw.Draw(glow).ellipse([S * 0.18, S * 0.12, S * 0.82, S * 0.76], fill=70)
    glow = glow.filter(ImageFilter.GaussianBlur(30 * SCALE))
    img.paste(Image.new("RGB", (S, S), WAVE_LIGHT), (0, 0), glow)

    draw = ImageDraw.Draw(img)
    draw_oar(draw, math.radians(35))
    draw_oar(draw, math.radians(-35))
    draw_waves(draw, S * 0.80, 7 * SCALE, WAVE, 0.0)
    draw_waves(draw, S * 0.87, 6 * SCALE, WAVE_LIGHT, 1.6)
    draw_waves(draw, S * 0.93, 5 * SCALE, WAVE, 3.1)

    out = Path(__file__).with_name("icon.png")
    img.resize((SIZE, SIZE), Image.LANCZOS).save(out, optimize=True)
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
