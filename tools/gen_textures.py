#!/usr/bin/env python3
"""Procedural block textures for Keystone.

Art direction (docs/DESIGN.md section 2): 32x32, clean stylised surfaces with
architectural trim and mortar lines that read from an overhead camera. Noise is
low-frequency and low-contrast on purpose. Per-pixel speckle is the look to avoid.

Deterministic: the same script always writes the same PNGs.
"""
from pathlib import Path

import numpy as np
from PIL import Image

SIZE = 32
OUT = Path(__file__).resolve().parent.parent / "textures" / "blocks"


def rng(name):
    return np.random.default_rng(sum(ord(c) * (i + 1) for i, c in enumerate(name)))


def smooth_noise(r, scale=8):
    """Tileable low-frequency noise in 0..1, from wrapped bilinear upsampling."""
    cells = max(2, SIZE // scale)
    grid = r.random((cells, cells))
    coords = np.arange(SIZE) * cells / SIZE
    i0 = coords.astype(int) % cells
    i1 = (i0 + 1) % cells
    t = coords - np.floor(coords)
    t = t * t * (3 - 2 * t)
    rows = grid[i0][:, i0] * (1 - t)[None, :] + grid[i0][:, i1] * t[None, :]
    rows_next = grid[i1][:, i0] * (1 - t)[None, :] + grid[i1][:, i1] * t[None, :]
    return rows * (1 - t)[:, None] + rows_next * t[:, None]


def flat(color):
    img = np.zeros((SIZE, SIZE, 3), dtype=float)
    img[:, :] = color
    return img


def tint(img, noise, amount):
    return img * (1 + (noise[..., None] - 0.5) * amount)


def shade(img, mask, factor):
    out = img.copy()
    out[mask] *= factor
    return out


def save(name, img, alpha=None):
    OUT.mkdir(parents=True, exist_ok=True)
    rgb = np.clip(img, 0, 255).astype(np.uint8)
    if alpha is None:
        Image.fromarray(rgb, "RGB").save(OUT / f"{name}.png")
    else:
        rgba = np.dstack([rgb, np.clip(alpha, 0, 255).astype(np.uint8)])
        Image.fromarray(rgba, "RGBA").save(OUT / f"{name}.png")


def ashlar(base, mortar, rows=4, seed="ashlar", jitter=0.06):
    """Dressed stone: running-bond courses with mortar and a soft bevel."""
    r = rng(seed)
    img = flat(base)
    course = SIZE // rows
    for row in range(rows):
        y0 = row * course
        offset = (row % 2) * (SIZE // 4)
        blocks = 2
        width = SIZE // blocks
        for b in range(blocks):
            x0 = (b * width + offset) % SIZE
            shift = 1 + r.uniform(-jitter, jitter)
            for y in range(y0, y0 + course):
                for x in range(width):
                    img[y % SIZE, (x0 + x) % SIZE] = np.array(base) * shift
            img[y0, [(x0 + x) % SIZE for x in range(width)]] *= 1.12
    for y in range(0, SIZE, course):
        img[(y + course - 1) % SIZE, :] = mortar
    for row in range(rows):
        offset = (row % 2) * (SIZE // 4)
        for b in range(2):
            x = (b * (SIZE // 2) + offset) % SIZE
            img[row * course:(row + 1) * course, x] = mortar
    return img


def stone():
    img = tint(flat((122, 126, 134)), smooth_noise(rng("stone"), 10), 0.22)
    r = rng("stone-seams")
    for _ in range(3):
        x, y = r.integers(0, SIZE, 2)
        for i in range(r.integers(6, 12)):
            img[y % SIZE, x % SIZE] *= 0.8
            x += r.integers(0, 2)
            y += r.integers(0, 2)
    return img


def loam():
    img = tint(flat((110, 80, 56)), smooth_noise(rng("loam"), 7), 0.28)
    return tint(img, smooth_noise(rng("loam2"), 14), 0.14)


def turf_top():
    img = tint(flat((94, 148, 72)), smooth_noise(rng("turf"), 10), 0.2)
    return tint(img, smooth_noise(rng("turf2"), 5), 0.1)


def turf_side():
    """Mostly bare soil with a thin straight sod line.

    Deliberately not a scalloped green cap over dirt: that silhouette is the signature
    of a well-known block game, and DESIGN.md section 2 rules out looking like it.
    """
    img = loam()
    green = turf_top()
    img[:3] = green[:3]
    img[3] *= 0.7
    return img


def pebbles(base, seed, count=18, radius=(2, 4), contrast=0.22):
    r = rng(seed)
    img = tint(flat(base), smooth_noise(r, 12), 0.1) * 0.82
    xs, ys = np.meshgrid(np.arange(SIZE), np.arange(SIZE))
    for _ in range(count):
        cx, cy, rad = r.uniform(0, SIZE), r.uniform(0, SIZE), r.uniform(*radius)
        tone = 1 + r.uniform(-contrast, contrast)
        dx = np.abs(xs - cx)
        dy = np.abs(ys - cy)
        dist = np.hypot(np.minimum(dx, SIZE - dx), np.minimum(dy, SIZE - dy))
        img[dist < rad] = np.array(base) * tone
        img[(dist < rad) & (ys - cy < -rad * 0.3)] *= 1.1
    return img


def rock():
    """A boulder's face: broad mottling, a few cracks, and edges a shade darker, as every face of the
    boulder shows the whole tile and its rocks should look rounded off."""
    r = rng("rock")
    base = np.array((136, 138, 142), dtype=float)
    img = tint(tint(flat(base), smooth_noise(r, 10), 0.22), smooth_noise(r, 4), 0.08)
    ys, xs = np.mgrid[0:SIZE, 0:SIZE].astype(float)
    for _ in range(3):
        x, y = r.uniform(4, SIZE - 4, 2)
        angle = r.uniform(0, np.pi)
        for _ in range(r.integers(8, 14)):
            img[int(y) % SIZE, int(x) % SIZE] *= 0.7
            img[int(y + 1) % SIZE, int(x) % SIZE] *= 1.06  # The lip below the crack catches light
            angle += r.uniform(-0.5, 0.5)
            x, y = x + np.cos(angle), y + np.sin(angle)
    edge = np.minimum(np.minimum(xs, SIZE - 1 - xs), np.minimum(ys, SIZE - 1 - ys))
    img *= (0.84 + 0.16 * np.clip(edge / 4, 0, 1))[..., None]
    return img


def bark():
    img = flat((92, 66, 44))
    r = rng("bark")
    for x in range(0, SIZE, 5):
        offset = r.integers(0, 5)
        img[:, (x + offset) % SIZE] *= 0.68
        img[:, (x + offset + 2) % SIZE] *= 1.1
    return tint(img, smooth_noise(r, 5), 0.16)


def log_end():
    ys, xs = np.mgrid[0:SIZE, 0:SIZE]
    dist = np.hypot(xs - 15.5, ys - 15.5)
    img = flat((176, 140, 92)) * (1 + 0.1 * np.sin(dist * 1.5))[..., None]
    img[dist > 14] = (92, 66, 44)
    return img


def canopy():
    r = rng("canopy")
    img = tint(flat((58, 112, 62)), smooth_noise(r, 6), 0.34)
    return tint(img, smooth_noise(r, 12), 0.16)


BUSH_SHEET = 24  # Rows of leaves. Below them, a berry. See the bush in gen_blocks.py.


def berry_bush(ripe=True):
    """Leaves in overlapping rounded clumps, each lit from above and shadowed under, with the
    sheet's edge cut out between the clumps along it. Every leaf face of the bush shows the whole
    sheet. Hidden pixels are a dark leaf green, so a distant mip darkens the edge instead of greying it.
    A ripe bush has berries in its leaves too: its real ones are too small to see from far off."""
    r = rng("berry")
    leaf = np.array((70, 128, 66), dtype=float)
    img = np.zeros((SIZE, SIZE, 3)) + (34, 70, 38)
    alpha = np.zeros((SIZE, SIZE))
    ys, xs = np.mgrid[0:BUSH_SHEET, 0:SIZE].astype(float)

    # The edge: an inset body, and clumps along its rim that reach out to the sheet's border
    sheet = img[:BUSH_SHEET]
    solid = (xs >= 3) & (xs <= SIZE - 4) & (ys >= 3) & (ys <= BUSH_SHEET - 4)
    sheet[solid] = leaf * 0.82
    clumps = []
    for x in np.arange(3.5, SIZE - 3, 6.2):
        clumps += [(x + r.uniform(-1, 1), 3.2), (x + r.uniform(-1, 1), BUSH_SHEET - 3.8)]
    for y in np.arange(7.5, BUSH_SHEET - 5, 5.5):
        clumps += [(3.2, y + r.uniform(-1, 1)), (SIZE - 3.8, y + r.uniform(-1, 1))]
    # And inside it, from the top row down, so each clump overlaps the one above it
    for y in np.arange(5, BUSH_SHEET - 3, 4.5):
        for x in np.arange(5 + (y % 9) * 0.5, SIZE - 4, 7):
            clumps.append((x + r.uniform(-1.2, 1.2), y + r.uniform(-0.8, 0.8)))
    clumps.sort(key=lambda c: c[1])
    for cx, cy in clumps:
        rx, ry = r.uniform(3.4, 4.2), r.uniform(2.8, 3.4)
        d = ((xs - cx) / rx) ** 2 + ((ys - cy) / ry) ** 2
        inside = d < 1
        light = 1.12 - 0.34 * np.clip((ys - (cy - ry)) / (2 * ry), 0, 1)
        sheet[inside] = (leaf * r.uniform(0.92, 1.08) * light[..., None])[inside]
        sheet[inside & (d > 0.62) & (ys > cy)] *= 0.72  # Shadow under its lower rim
        solid |= inside
    alpha[:BUSH_SHEET][solid] = 255
    img[:BUSH_SHEET] = tint(sheet, smooth_noise(r, 8)[:BUSH_SHEET], 0.12)
    if ripe:
        for cx, cy in ((9, 8), (22, 11), (13, 18)):
            dist = np.hypot(xs - cx, ys - cy)
            img[:BUSH_SHEET][dist < 1.7] = (196, 30, 52)
            img[:BUSH_SHEET][(dist < 0.9) & (xs < cx) & (ys < cy)] = (236, 132, 140)

    # The berry, lit from the top left
    by, bx = np.mgrid[0:SIZE - BUSH_SHEET, 0:8].astype(float)
    berry = np.zeros(by.shape + (3,)) + (176, 36, 56)
    berry *= (1.12 - 0.05 * (bx + by))[..., None]
    berry[(bx >= 1) & (bx <= 2) & (by >= 1) & (by <= 2)] = (236, 132, 140)
    img[BUSH_SHEET:, :8] = berry
    alpha[BUSH_SHEET:, :8] = 255
    return img, alpha


def planks():
    r = rng("planks")
    img = flat((166, 122, 74))
    rows = 4
    h = SIZE // rows
    for row in range(rows):
        board = np.array((166, 122, 74)) * r.uniform(0.92, 1.08)
        img[row * h: (row + 1) * h] = board
        grain = smooth_noise(r, 6)[row * h: (row + 1) * h]
        img[row * h: (row + 1) * h] *= (1 + (grain - 0.5) * 0.14)[..., None]
        img[(row + 1) * h - 1] *= 0.55
        img[row * h] *= 1.08
        joint = r.integers(4, 28)
        img[row * h: (row + 1) * h, joint] *= 0.6
    return img


def thatch():
    r = rng("thatch")
    img = flat((188, 158, 88))
    for _ in range(70):
        x, y = r.integers(0, SIZE, 2)
        length = r.integers(5, 11)
        tone = r.uniform(0.74, 1.18)
        for i in range(length):
            img[(y + i) % SIZE, (x + (i // 3)) % SIZE] *= tone
    for y in range(0, SIZE, 8):
        img[y] *= 0.78
    return img


def palisade():
    img = flat((36, 26, 18))
    r = rng("palisade")
    logs = 4
    w = SIZE // logs
    for i in range(logs):
        tone = r.uniform(0.92, 1.08)
        for x in range(1, w - 1):
            curve = np.sin((x / (w - 1)) * np.pi)
            img[:, i * w + x] = np.array((112, 80, 52)) * (0.72 + 0.34 * curve) * tone
    return tint(img, smooth_noise(r, 6), 0.12)


def scaffold():
    img = flat((150, 112, 66))
    alpha = np.zeros((SIZE, SIZE))
    for i in range(SIZE):
        for d in range(-1, 2):
            alpha[i, (i + d) % SIZE] = 255
    alpha[:3, :] = 255
    alpha[-3:, :] = 255
    alpha[:, :3] = 255
    alpha[:, -3:] = 255
    return img, alpha


def cordwood():
    img = flat((58, 42, 28))
    r = rng("cordwood")
    ys, xs = np.mgrid[0:SIZE, 0:SIZE]
    for cy in (5, 16, 27):
        for cx in (5 + (cy == 16) * 5, 16 + (cy == 16) * 5, 27):
            dist = np.hypot(xs - cx, ys - cy)
            tone = r.uniform(0.9, 1.1)
            face = np.array((186, 148, 96)) * tone * (1 + 0.08 * np.sin(dist * 2.2))[..., None]
            img = np.where((dist < 5)[..., None], face, img)
    return img


def grain_sack():
    img = tint(flat((202, 180, 132)), smooth_noise(rng("sack"), 8), 0.14)
    ys, xs = np.mgrid[0:SIZE, 0:SIZE]
    img *= (0.84 + 0.2 * np.sin(np.pi * ys / SIZE))[..., None]
    img[:, ::8] *= 0.9
    img[::16] *= 0.74
    return img


def keystone():
    img = ashlar((64, 72, 106), (38, 42, 66), rows=3, seed="keystone")
    ys, xs = np.mgrid[0:SIZE, 0:SIZE]
    diamond = np.abs(xs - 15.5) + np.abs(ys - 15.5)
    img[diamond < 11] = (236, 196, 96)
    img[diamond < 8] = (120, 216, 240)
    img[diamond < 4] = (232, 250, 255)
    return img


def banner(color):
    r = rng("banner")
    img = tint(flat(color), smooth_noise(r, 8), 0.1)
    img[:, :2] *= 0.7
    img[:, -2:] *= 0.7
    img[:2, :] = (214, 176, 90)
    return img


def main():
    save("stone", stone())
    save("loam", loam())
    save("turf_top", turf_top())
    save("turf_side", turf_side())
    save("gravel", pebbles((140, 132, 120), "gravel"))
    save("sand", tint(flat((214, 196, 150)), smooth_noise(rng("sand"), 9), 0.1))
    save("boulder", rock())
    save("bark", bark())
    save("log_end", log_end())
    save("canopy", canopy())
    save("berry_bush", *berry_bush())
    save("berry_bush_bare", *berry_bush(ripe=False))
    save("planks", planks())
    save("thatch", thatch())
    save("palisade", palisade())
    save("cordwood", cordwood())
    save("stone_pallet", ashlar((156, 156, 158), (98, 96, 92), rows=3, seed="pallet"))
    save("dressed_stone", ashlar((134, 134, 138), (88, 86, 84), seed="dressed"))
    save("grain_sack", grain_sack())
    save("flagstone", ashlar((118, 112, 104), (72, 68, 62), rows=2, seed="flag"))
    save("gravel_path", pebbles((168, 156, 130), "path", count=24, radius=(2, 3), contrast=0.12))
    save("keystone", keystone())
    save("banner_blue", banner((52, 92, 170)))
    save("banner_red", banner((170, 56, 52)))
    img, alpha = scaffold()
    save("scaffold", img, alpha)
    print(f"wrote {len(list(OUT.glob('*.png')))} textures to {OUT}")


if __name__ == "__main__":
    main()
