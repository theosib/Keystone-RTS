#!/usr/bin/env python3
"""HUD icons, painted procedurally in the Hearth skin's colours.

Drawn large and scaled down, so the shapes stay clean at the size the HUD shows them.
Deterministic: the same script always writes the same files. Writes ui/icons/*.png.
"""
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "ui" / "icons"

CANVAS = 512
SIZE = 64
LINE = 22

OUTLINE = (34, 22, 14, 255)
BARK = (112, 72, 40, 255)
WOOD = (214, 170, 110, 255)
RING = (156, 108, 62, 255)
STONE = (156, 152, 146, 255)
STONE_LIT = (196, 192, 184, 255)
STONE_DARK = (108, 104, 100, 255)
SACK = (222, 196, 140, 255)
SACK_DARK = (176, 146, 94, 255)
TIE = (120, 72, 36, 255)
PARCHMENT = (226, 208, 168, 255)
HOOD = (186, 150, 96, 255)
IRON = (150, 146, 140, 255)
EMBER = (232, 134, 40, 255)
EMBER_HOT = (255, 196, 88, 255)


def log_end(draw, x, y, r):
    draw.ellipse([x - r, y - r, x + r, y + r], fill=WOOD, outline=OUTLINE, width=LINE)
    draw.ellipse([x - r * 0.55, y - r * 0.55, x + r * 0.55, y + r * 0.55], outline=RING, width=12)
    draw.ellipse([x - r * 0.18, y - r * 0.18, x + r * 0.18, y + r * 0.18], fill=RING)


def timber(draw):
    r = 104
    log_end(draw, 150, 350, r)
    log_end(draw, 362, 350, r)
    log_end(draw, 256, 170, r)


def stone(draw):
    body = [(70, 400), (40, 280), (120, 130), (280, 70), (420, 130), (476, 290), (430, 410), (240, 450)]
    draw.polygon(body, fill=STONE, outline=OUTLINE, width=LINE)
    draw.polygon([(120, 130), (280, 70), (300, 210), (170, 250)], fill=STONE_LIT)
    draw.polygon([(300, 210), (420, 130), (476, 290), (340, 320)], fill=STONE_DARK)
    draw.line([(170, 250), (300, 210), (340, 320), (240, 450)], fill=OUTLINE, width=LINE // 2)
    draw.line([(300, 210), (280, 70)], fill=OUTLINE, width=LINE // 2)
    draw.line([(170, 250), (70, 400)], fill=OUTLINE, width=LINE // 2)


def provisions(draw):
    draw.polygon([(190, 130), (140, 60), (372, 60), (322, 130)], fill=SACK_DARK, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle([70, 130, 442, 470], radius=130, fill=SACK, outline=OUTLINE, width=LINE)
    draw.rectangle([180, 112, 332, 152], fill=TIE, outline=OUTLINE, width=LINE // 2)
    for x, y in [(256, 230), (200, 320), (312, 320), (256, 400)]:
        draw.ellipse([x - 32, y - 52, x + 32, y + 52], fill=SACK_DARK)


def population(draw):
    draw.pieslice([70, 250, 442, 700], 180, 360, fill=HOOD, outline=OUTLINE, width=LINE)
    draw.ellipse([146, 60, 366, 280], fill=HOOD, outline=OUTLINE, width=LINE)
    draw.ellipse([192, 120, 320, 248], fill=PARCHMENT, outline=OUTLINE, width=LINE // 2)


def army(draw):
    draw.line([(420, 40), (120, 470)], fill=OUTLINE, width=44)
    draw.line([(420, 40), (120, 470)], fill=BARK, width=20)
    shield = [(110, 110), (402, 110), (402, 290), (256, 470), (110, 290)]
    draw.polygon(shield, fill=EMBER, outline=OUTLINE, width=LINE)
    draw.polygon([(256, 150), (340, 240), (256, 350), (172, 240)], fill=EMBER_HOT, outline=OUTLINE, width=LINE // 2)
    draw.line([(114, 110), (398, 110)], fill=IRON, width=14)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for paint in (timber, stone, provisions, population, army):
        canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        paint(ImageDraw.Draw(canvas))
        canvas.resize((SIZE, SIZE), Image.LANCZOS).save(OUT / f"{paint.__name__}.png", optimize=True)


if __name__ == "__main__":
    main()
