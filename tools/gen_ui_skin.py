#!/usr/bin/env python3
"""The Hearth skin: UI art for Epoch I, painted procedurally.

Dark oak and wrought iron for the frames, firelight for what's alive. Everything is drawn at
2x and read with the 9-slice borders given here, so corners and rivets keep their shape at any
size. Grain runs along the long axis because centres stretch. As with the block textures, noise is low-frequency: no per-pixel speckle.

Writes ui/skin/hearth.png and ui/skin/hearth.fgs together, so the two can't drift apart.
Deterministic: the same script always writes the same files.
"""
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "ui" / "skin"

ATLAS_SIZE = 1024
GUTTER = 6      # Between regions, so filtering never picks up a neighbour

OAK_DARK = (50, 33, 22)
OAK = (86, 58, 37)
OAK_WARM = (120, 80, 46)
IRON = (62, 60, 58)
IRON_LIT = (128, 124, 116)
PARCHMENT = (214, 193, 150)
EMBER = (232, 134, 40)
EMBER_HOT = (255, 196, 88)
BLACK = (14, 10, 8)


def rng(name):
    return np.random.default_rng(sum(ord(c) * (i + 1) for i, c in enumerate(name)))


def smooth_noise(height, width, cell_y, cell_x, gen):
    """Value noise, cosine-interpolated, roughly in [0, 1]."""
    rows = int(height / cell_y) + 3
    cols = int(width / cell_x) + 3
    grid = gen.random((rows, cols))
    y = np.arange(height) / cell_y
    x = np.arange(width) / cell_x
    y0 = y.astype(int)
    x0 = x.astype(int)
    fy = (1 - np.cos((y - y0) * np.pi)) / 2
    fx = (1 - np.cos((x - x0) * np.pi)) / 2
    fy = fy[:, None]
    fx = fx[None, :]
    a = grid[y0][:, x0]
    b = grid[y0][:, x0 + 1]
    c = grid[y0 + 1][:, x0]
    d = grid[y0 + 1][:, x0 + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def lerp(a, b, t):
    return np.asarray(a, float) + (np.asarray(b, float) - np.asarray(a, float)) * t


def wood(height, width, name, dark=OAK_DARK, light=OAK, planks=1):
    """Planks with grain along x. Seams are dark lines between boards."""
    gen = rng(name)
    grain = (smooth_noise(height, width, 3.5, 90, gen) * 0.55
             + smooth_noise(height, width, 11, 180, gen) * 0.3
             + smooth_noise(height, width, 34, 240, gen) * 0.15)
    rgb = lerp(dark, light, grain[..., None])

    if planks > 1:
        board = height / planks
        for i in range(1, planks):
            seam = int(board * i)
            rgb[seam - 1:seam + 1] *= 0.45
            rgb[seam + 1:seam + 3] *= 1.12
    return rgb


def iron(height, width, name):
    gen = rng(name)
    tone = smooth_noise(height, width, 14, 14, gen)[..., None]
    return lerp(IRON, (84, 80, 76), tone)


def shade_edges(rgb, width, top=1.25, left=1.15, bottom=0.55, right=0.65):
    """A bevel: light from the top left, as if lit by the room."""
    height, wide = rgb.shape[:2]
    y = np.arange(height)[:, None]
    x = np.arange(wide)[None, :]
    def band(distance):
        return np.clip(1 - distance / width, 0, 1) ** 2
    light = (1 + band(y) * (top - 1)) * (1 + band(x) * (left - 1))
    dark = (1 + band(height - 1 - y) * (bottom - 1)) * (1 + band(wide - 1 - x) * (right - 1))
    return rgb * (light * dark)[..., None]


def rounded_alpha(height, width, radius):
    """Antialiased rounded rectangle coverage."""
    y = np.arange(height)[:, None] + 0.5
    x = np.arange(width)[None, :] + 0.5
    dy = np.maximum(np.maximum(radius - y, y - (height - radius)), 0)
    dx = np.maximum(np.maximum(radius - x, x - (width - radius)), 0)
    return np.clip(radius + 0.5 - np.sqrt(dx * dx + dy * dy), 0, 1)


def inset_distance(height, width):
    """Distance in pixels from each pixel to the nearest edge."""
    y = np.arange(height)[:, None]
    x = np.arange(width)[None, :]
    return np.minimum(np.minimum(y, height - 1 - y), np.minimum(x, width - 1 - x))


def rivet(rgb, cy, cx, radius):
    y = np.arange(rgb.shape[0])[:, None]
    x = np.arange(rgb.shape[1])[None, :]
    d = np.sqrt((y - cy) ** 2 + (x - cx) ** 2)
    cover = np.clip(radius - d + 0.5, 0, 1)[..., None]
    lit = np.clip(1 - (((y - (cy - radius * 0.4)) ** 2 + (x - (cx - radius * 0.4)) ** 2) ** 0.5) / radius, 0, 1)
    colour = lerp(IRON, IRON_LIT, lit[..., None])
    # A shadow under the head seats it in the metal
    shadow = np.clip(radius + 2 - np.sqrt((y - cy - 1.5) ** 2 + (x - cx - 1.5) ** 2), 0, 1)[..., None]
    rgb[:] = rgb * (1 - shadow * 0.5)
    rgb[:] = rgb * (1 - cover) + colour * cover


def bracket(rgb, band, name):
    """Iron banding round a frame, with a rivet in each corner plate."""
    height, width = rgb.shape[:2]
    metal = shade_edges(iron(height, width, name), 3, 1.5, 1.3, 0.5, 0.6)
    frame = (inset_distance(height, width) < band)[..., None]
    # Corner plates, big enough to read as forged rather than drawn
    plate = band * 2 + 8
    y = np.arange(height)[:, None]
    x = np.arange(width)[None, :]
    near_y = (y < plate) | (y >= height - plate)
    near_x = (x < plate) | (x >= width - plate)
    corners = (near_y & near_x)[..., None]
    mask = frame | corners
    rgb[:] = np.where(mask, metal, rgb)

    # An inner shadow where the wood meets the iron
    inner = inset_distance(height, width) - band
    falloff = np.clip(1 - inner / 10, 0, 1) * (inner >= 0) * ~(near_y & near_x)
    rgb[:] = rgb * (1 - falloff[..., None] * 0.55)

    r = 4.5
    offset = plate * 0.55
    for cy in (offset, height - 1 - offset):
        for cx in (offset, width - 1 - offset):
            rivet(rgb, cy, cx, r)


def rgba(rgb, alpha):
    out = np.zeros(rgb.shape[:2] + (4,), float)
    out[..., :3] = np.clip(rgb, 0, 255)
    out[..., 3] = np.clip(alpha, 0, 1) * 255
    return out


def panel(height, width, name, dark, light, planks, band, radius=8):
    rgb = wood(height, width, name, dark, light, planks)
    bracket(rgb, band, name + "_iron")
    return rgba(rgb, rounded_alpha(height, width, radius))


def button(height, width, name, light, glow=None, pressed=False):
    rgb = wood(height, width, name, OAK_DARK, light)
    if pressed:
        rgb = shade_edges(rgb * 0.82, 7, 0.5, 0.6, 1.2, 1.1)
    else:
        rgb = shade_edges(rgb, 7, 1.5, 1.3, 0.5, 0.6)
    edge = inset_distance(height, width)
    rim = (edge < 3)[..., None]
    colour = np.array(glow if glow is not None else BLACK, float)
    rgb = np.where(rim, lerp(rgb, colour, 0.8), rgb)
    if glow is not None:
        bloom = np.clip(1 - (edge - 3) / 12, 0, 1)[..., None] * (edge >= 3)[..., None]
        rgb = rgb + (colour - rgb) * bloom * 0.28
    return rgba(rgb, rounded_alpha(height, width, 6))


def sunken(height, width, name, tone=(26, 18, 13), rim=(90, 66, 44), lit=None):
    """A recess cut into the wood: inputs, tracks."""
    gen = rng(name)
    noise = smooth_noise(height, width, 12, 60, gen)[..., None]
    rgb = lerp(tone, np.asarray(tone) * 1.5, noise)
    edge = inset_distance(height, width)
    y = np.arange(height)[:, None]
    x = np.arange(width)[None, :]
    # Shadow falls in from the top left of a recess
    shadow = np.clip(1 - y / 10, 0, 1) * 0.6 + np.clip(1 - x / 10, 0, 1) * 0.35
    rgb = rgb * (1 - np.clip(shadow, 0, 0.8))[..., None]
    outline = (edge < 2)[..., None]
    rgb = np.where(outline, np.array(lit if lit is not None else rim, float), rgb)
    return rgba(rgb, rounded_alpha(height, width, 5))


def round_knob(size, name, dark, light):
    centre = (size - 1) / 2
    y = np.arange(size)[:, None]
    x = np.arange(size)[None, :]
    d = np.sqrt((y - centre) ** 2 + (x - centre) ** 2)
    radius = size / 2 - 1
    alpha = np.clip(radius - d + 0.5, 0, 1)
    hot = np.sqrt((y - centre * 0.7) ** 2 + (x - centre * 0.7) ** 2)
    rgb = lerp(dark, light, np.clip(1 - hot / size, 0, 1)[..., None])
    rim = np.clip(d - (radius - 3), 0, 1)[..., None]
    rgb = lerp(rgb, BLACK, rim * 0.8)
    return rgba(rgb, alpha)


def ember_bar(height, width, name):
    gen = rng(name)
    flicker = smooth_noise(height, width, 20, 46, gen)[..., None]
    y = np.arange(height)[:, None] / max(height - 1, 1)
    core = (1 - np.abs(y - 0.42) * 2)[..., None]
    rgb = lerp(EMBER, EMBER_HOT, np.clip(core * 0.8 + flicker * 0.35, 0, 1))
    rgb = rgb * (0.75 + 0.35 * core)
    return rgba(shade_edges(rgb, 5, 1.2, 1.0, 0.6, 1.0), rounded_alpha(height, width, 4))


def tint_overlay(height, width, colour, strength):
    """A translucent wash for hovered rows, which sit on whatever panel is behind them."""
    edge = inset_distance(height, width)
    fade = np.clip(edge / 8, 0, 1)
    rgb = np.ones((height, width, 3)) * np.array(colour, float)
    return rgba(rgb, fade * strength)


def tab(height, width, name, light, lit=False):
    rgb = wood(height, width, name, OAK_DARK, light)
    rgb = shade_edges(rgb, 6, 1.4, 1.2, 1.0, 0.7)
    if lit:
        y = np.arange(height)[:, None]
        rgb = rgb + (np.array(EMBER_HOT, float) - rgb) * (np.clip(1 - y / 5, 0, 1)[..., None] * 0.9)
    return rgba(rgb, rounded_alpha(height, width + 0, 6))


# Region name -> (art, inset as left, top, right, bottom in atlas pixels)
REGIONS = {
    "panel": (panel(220, 300, "panel", OAK_DARK, OAK, 4, 9), (34, 34, 34, 34)),
    "popup": (panel(160, 240, "popup", (42, 28, 19), (72, 49, 32), 3, 7), (28, 28, 28, 28)),
    "title": (panel(56, 240, "title", OAK_DARK, OAK_WARM, 1, 5, radius=6), (24, 16, 24, 16)),
    "title_lit": (panel(56, 240, "title_lit", (72, 44, 24), (156, 100, 52), 1, 5, radius=6), (24, 16, 24, 16)),
    "menubar": (panel(48, 200, "menubar", OAK_DARK, OAK, 1, 4, radius=2), (20, 12, 20, 12)),

    "button": (button(68, 200, "button", (168, 118, 72)), (16, 16, 16, 16)),
    "button_hot": (button(68, 200, "button_hot", (198, 140, 82), EMBER), (20, 20, 20, 20)),
    "button_down": (button(68, 200, "button_down", OAK, EMBER_HOT, pressed=True), (20, 20, 20, 20)),

    "frame": (sunken(56, 160, "frame"), (14, 14, 14, 14)),
    "frame_hot": (sunken(56, 160, "frame_hot", lit=(170, 116, 64)), (14, 14, 14, 14)),
    "frame_active": (sunken(56, 160, "frame_active", lit=EMBER), (14, 14, 14, 14)),

    "check": (sunken(52, 52, "check"), (12, 12, 12, 12)),
    "check_hot": (sunken(52, 52, "check_hot", lit=EMBER), (12, 12, 12, 12)),
    "radio": (round_knob(52, "radio", (22, 15, 11), (54, 38, 28)), (0, 0, 0, 0)),
    "radio_hot": (round_knob(52, "radio_hot", (44, 26, 14), (118, 74, 36)), (0, 0, 0, 0)),

    "track": (sunken(52, 200, "track"), (16, 16, 16, 16)),
    "grab": (round_knob(48, "grab", IRON, IRON_LIT), (0, 0, 0, 0)),
    "grab_hot": (round_knob(48, "grab_hot", (120, 80, 46), EMBER_HOT), (0, 0, 0, 0)),

    "bar_track": (sunken(40, 200, "bar_track"), (12, 12, 12, 12)),
    "bar_fill": (ember_bar(40, 200, "bar_fill"), (8, 8, 8, 8)),

    "scroll_track": (sunken(160, 32, "scroll_track"), (10, 14, 10, 14)),
    "scroll_grab": (rgba(shade_edges(iron(120, 32, "scroll_grab"), 5, 1.6, 1.4, 0.5, 0.6), rounded_alpha(120, 32, 8)), (10, 18, 10, 18)),
    "scroll_grab_hot": (rgba(shade_edges(lerp(iron(120, 32, "scroll_grab_hot"), EMBER, 0.35), 5, 1.6, 1.4, 0.5, 0.6), rounded_alpha(120, 32, 8)), (10, 18, 10, 18)),

    "row_hot": (tint_overlay(36, 80, EMBER, 0.28), (10, 10, 10, 10)),
    "row_down": (tint_overlay(36, 80, EMBER_HOT, 0.42), (10, 10, 10, 10)),
    "row_selected": (tint_overlay(36, 80, (170, 116, 64), 0.34), (10, 10, 10, 10)),

    "tab": (tab(60, 160, "tab", OAK), (16, 14, 16, 8)),
    "tab_hot": (tab(60, 160, "tab_hot", (138, 92, 54)), (16, 14, 16, 8)),
    "tab_selected": (tab(60, 160, "tab_selected", OAK_WARM, lit=True), (16, 14, 16, 8)),

    "close": (round_knob(40, "close", (120, 40, 26), (214, 92, 54)), (0, 0, 0, 0)),
}

PARTS = {
    "window": {"normal": "panel"},
    "popup": {"normal": "popup"},
    "tooltip": {"normal": "popup"},
    "title_bar": {"normal": "title", "selected": "title_lit"},
    "menu_bar": {"normal": "menubar"},
    "button": {"normal": "button", "hovered": "button_hot", "active": "button_down"},
    "close_button": {"normal": "close"},
    "frame": {"normal": "frame", "hovered": "frame_hot", "active": "frame_active"},
    "checkbox": {"normal": "check", "hovered": "check_hot"},
    "radio": {"normal": "radio", "hovered": "radio_hot"},
    "slider_track": {"normal": "track"},
    "slider_grab": {"normal": "grab", "hovered": "grab_hot"},
    "progress_track": {"normal": "bar_track"},
    "progress_fill": {"normal": "bar_fill"},
    "scrollbar_track": {"normal": "scroll_track"},
    "scrollbar_grab": {"normal": "scroll_grab", "hovered": "scroll_grab_hot"},
    "header": {"normal": "row_selected", "hovered": "row_hot", "active": "row_down",
               "selected": "row_selected"},
    "tab": {"normal": "tab", "hovered": "tab_hot", "selected": "tab_selected"},
}

STYLE = {
    "window_padding": (17, 15),
    "frame_padding": (11, 6),
    "item_spacing": (9, 7),
    "item_inner_spacing": (7, 5),
    "scrollbar_size": 16,
    "grab_min_size": 22,
    "indent_spacing": 18,
}

COLORS = {
    "Text": (0.95, 0.88, 0.74, 1.0),
    "TextDisabled": (0.62, 0.54, 0.44, 1.0),
    "CheckMark": (1.0, 0.72, 0.30, 1.0),
    "TextSelectedBg": (0.9, 0.5, 0.15, 0.4),
    "Separator": (0.42, 0.30, 0.20, 0.9),
    "NavCursor": (1.0, 0.72, 0.30, 1.0),
    "ModalWindowDimBg": (0.05, 0.03, 0.02, 0.6),
    "PopupBg": (0.16, 0.11, 0.08, 1.0),
    "ResizeGrip": (0.0, 0.0, 0.0, 0.0),
    "ResizeGripHovered": (0.9, 0.5, 0.15, 0.6),
    "ResizeGripActive": (1.0, 0.72, 0.30, 0.9),
}


def pack(regions):
    """Shelf-pack tallest first. Returns name -> (x, y)."""
    placed = {}
    x = y = GUTTER
    shelf = 0
    for name, (art, _) in sorted(regions.items(), key=lambda item: -item[1][0].shape[0]):
        height, width = art.shape[:2]
        if x + width + GUTTER > ATLAS_SIZE:
            x = GUTTER
            y += shelf + GUTTER
            shelf = 0
        if y + height + GUTTER > ATLAS_SIZE:
            raise SystemExit(f"atlas full at {name}")
        placed[name] = (x, y)
        x += width + GUTTER
        shelf = max(shelf, height)
    return placed


def fgs_number(value):
    return f"{value:g}"


def write_definition(placed):
    lines = ['# Generated by tools/gen_ui_skin.py. Change the art there, not here.', '{=name "hearth"']
    lines.append('=atlas "hearth.png"')
    lines.append('=units_per_pixel 0.5')
    lines.append('=font "Almendra-Bold.ttf"')
    lines.append('=font_size 18')

    region_items = []
    for name, (art, inset) in sorted(REGIONS.items()):
        x, y = placed[name]
        height, width = art.shape[:2]
        sides = " ".join(str(v) for v in inset)
        region_items.append(f'={name} {{=x {x} =y {y} =w {width} =h {height} =inset [{sides}]}}')
    lines.append('=regions {' + " ".join(region_items) + '}')

    part_items = []
    for part, states in PARTS.items():
        inner = " ".join(f'={state} "{region}"' for state, region in states.items())
        part_items.append(f'={part} {{{inner}}}')
    lines.append('=parts {' + " ".join(part_items) + '}')

    style_items = []
    for key, value in STYLE.items():
        if isinstance(value, tuple):
            style_items.append(f'={key} [{value[0]} {value[1]}]')
        else:
            style_items.append(f'={key} {value}')
    lines.append('=style {' + " ".join(style_items) + '}')

    color_items = []
    for key, value in COLORS.items():
        color_items.append(f'={key} [' + " ".join(fgs_number(c) for c in value) + ']')
    lines.append('=colors {' + " ".join(color_items) + '}}')

    # finescript ends a statement at a newline, so the map has to be one line
    (OUT / "hearth.fgs").write_text(lines[0] + "\n" + " ".join(lines[1:]) + "\n")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    placed = pack(REGIONS)
    atlas = np.zeros((ATLAS_SIZE, ATLAS_SIZE, 4), float)
    for name, (art, _) in REGIONS.items():
        x, y = placed[name]
        height, width = art.shape[:2]
        atlas[y:y + height, x:x + width] = art
    Image.fromarray(atlas.round().astype(np.uint8), "RGBA").save(OUT / "hearth.png", optimize=True)
    write_definition(placed)
    used = max(y + REGIONS[n][0].shape[0] for n, (_, y) in placed.items())
    print(f"hearth.png: {len(REGIONS)} regions, {used}px of {ATLAS_SIZE} used")


if __name__ == "__main__":
    main()
