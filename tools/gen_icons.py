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
BERRY = (150, 44, 66, 255)


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


def shelter(draw):
    draw.rectangle([110, 270, 402, 460], fill=WOOD, outline=OUTLINE, width=LINE)
    draw.rectangle([214, 330, 298, 460], fill=BARK, outline=OUTLINE, width=LINE // 2)
    draw.polygon([(256, 50), (480, 290), (32, 290)], fill=SACK, outline=OUTLINE, width=LINE)
    draw.line([(150, 280), (256, 110), (362, 280)], fill=SACK_DARK, width=16)


def forager_hut(draw):
    for x, y in [(170, 170), (256, 120), (342, 170)]:
        draw.ellipse([x - 66, y - 66, x + 66, y + 66], fill=BERRY, outline=OUTLINE, width=LINE)
    draw.polygon([(60, 230), (452, 230), (390, 460), (122, 460)], fill=WOOD, outline=OUTLINE, width=LINE)
    for y in (300, 370):
        draw.line([(100, y), (412, y)], fill=RING, width=14)
    draw.line([(214, 240), (200, 450)], fill=RING, width=14)
    draw.line([(298, 240), (312, 450)], fill=RING, width=14)


def sawpit(draw):
    draw.rounded_rectangle([40, 300, 472, 450], radius=70, fill=BARK, outline=OUTLINE, width=LINE)
    log_end(draw, 400, 375, 62)
    teeth = [(60, 230)]
    for x in range(60, 440, 44):
        teeth += [(x + 22, 280), (x + 44, 230)]
    draw.polygon([(60, 130), (440, 130)] + teeth[::-1], fill=IRON, outline=OUTLINE, width=LINE // 2)
    draw.rounded_rectangle([20, 100, 110, 200], radius=20, fill=WOOD, outline=OUTLINE, width=LINE // 2)


def watchtower(draw):
    draw.line([(170, 480), (210, 200)], fill=OUTLINE, width=46)
    draw.line([(342, 480), (302, 200)], fill=OUTLINE, width=46)
    draw.line([(170, 480), (210, 200)], fill=BARK, width=22)
    draw.line([(342, 480), (302, 200)], fill=BARK, width=22)
    draw.line([(185, 380), (327, 300)], fill=BARK, width=18)
    draw.line([(327, 380), (185, 300)], fill=BARK, width=18)
    draw.rectangle([120, 150, 392, 230], fill=WOOD, outline=OUTLINE, width=LINE)
    draw.polygon([(256, 30), (420, 150), (92, 150)], fill=SACK, outline=OUTLINE, width=LINE)


def palisade_wall(draw):
    for x in (80, 192, 304, 416):
        stake = [(x - 50, 470), (x - 50, 150), (x, 50), (x + 50, 150), (x + 50, 470)]
        draw.polygon(stake, fill=WOOD, outline=OUTLINE, width=LINE)
        draw.line([(x, 170), (x, 440)], fill=RING, width=12)
    draw.rectangle([20, 270, 492, 322], fill=BARK, outline=OUTLINE, width=LINE // 2)


def train_hand(draw):
    draw.line([(150, 460), (320, 190)], fill=OUTLINE, width=62)
    draw.line([(150, 460), (320, 190)], fill=WOOD, width=30)
    draw.polygon([(200, 130), (290, 40), (480, 220), (390, 310)], fill=IRON, outline=OUTLINE, width=LINE)
    draw.line([(254, 100), (420, 262)], fill=STONE_LIT, width=16)


def militia(draw):
    draw.polygon([(256, 24), (306, 90), (306, 340), (206, 340), (206, 90)], fill=STONE_LIT, outline=OUTLINE, width=LINE)
    draw.line([(256, 80), (256, 330)], fill=IRON, width=14)
    draw.rectangle([120, 330, 392, 386], fill=EMBER, outline=OUTLINE, width=LINE // 2)
    draw.rectangle([222, 386, 290, 470], fill=BARK, outline=OUTLINE, width=LINE // 2)
    draw.ellipse([204, 440, 308, 500], fill=EMBER, outline=OUTLINE, width=LINE // 2)


def shortbow(draw):
    draw.arc([-110, 40, 330, 472], -68, 68, fill=OUTLINE, width=52)
    draw.arc([-110, 40, 330, 472], -68, 68, fill=BARK, width=26)
    draw.line([(192, 64), (192, 448)], fill=PARCHMENT, width=10)
    draw.line([(90, 256), (420, 256)], fill=OUTLINE, width=32)
    draw.line([(90, 256), (420, 256)], fill=WOOD, width=14)
    draw.polygon([(490, 256), (396, 200), (396, 312)], fill=IRON, outline=OUTLINE, width=LINE // 2)
    draw.polygon([(50, 200), (130, 256), (50, 312), (90, 256)], fill=EMBER, outline=OUTLINE, width=LINE // 3)


def scout(draw):
    draw.ellipse([36, 36, 476, 476], fill=PARCHMENT, outline=OUTLINE, width=LINE)
    draw.polygon([(256, 66), (312, 256), (200, 256)], fill=EMBER, outline=OUTLINE, width=LINE // 2)
    draw.polygon([(256, 446), (312, 256), (200, 256)], fill=IRON, outline=OUTLINE, width=LINE // 2)
    draw.ellipse([226, 226, 286, 286], fill=BARK, outline=OUTLINE, width=LINE // 2)


def menu(draw):
    draw.rounded_rectangle([70, 56, 442, 456], radius=44, fill=PARCHMENT, outline=OUTLINE, width=LINE)
    for y in (160, 256, 352):
        draw.line([(144, y), (368, y)], fill=BARK, width=34)


ICONS = (timber, stone, provisions, population, army, shelter, forager_hut, sawpit, watchtower,
         palisade_wall, train_hand, militia, shortbow, scout, menu)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for paint in ICONS:
        canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        paint(ImageDraw.Draw(canvas))
        canvas.resize((SIZE, SIZE), Image.LANCZOS).save(OUT / f"{paint.__name__}.png", optimize=True)


if __name__ == "__main__":
    main()
