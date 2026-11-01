#!/usr/bin/env python3
"""Check that no two parts of a unit model can z-fight.

Parts are boxes, and the scene draws both sides of every face, so only the depth test says which
of two surfaces shows. Where the same-facing faces of two parts overlap, they fight if they're
level, and speckle at a distance if they're close. Parts that look the same can't show it, and
neither can a stretch of face with another part over it.

Checks the rest pose only.
"""
import itertools
import sys
from pathlib import Path

MODELS = Path(__file__).resolve().parent.parent / "models"
MIN_GAP = 0.02  # 1cm still speckles from 60 blocks up, seen from above
SLOP = 1e-6


def bones(path):
    """Each bone's rest position in the model."""
    local, name = {}, None
    for line in path.read_text().splitlines():
        key, _, value = line.partition(":")
        if key == "bone":
            name = value.strip()
        elif key == "parent":
            parent = value.strip()
        elif key == "position":
            local[name] = (parent, [float(v) for v in value.split()])

    def place(bone):
        parent, position = local[bone]
        if parent == "-":
            return position
        return [a + b for a, b in zip(place(parent), position)]

    return {bone: place(bone) for bone in local}


def parts(path):
    """A model's boxes, as dicts with name, look, lo and hi in the skeleton's rest pose."""
    found, part = [], None
    for line in path.read_text().splitlines():
        key, _, value = line.partition(":")
        value = value.strip()
        if key == "part":
            part = {"name": value, "look": [(0.7, 0.7, 0.7), 0.0]}
            found.append(part)
        elif part and key == "bone":
            part["bone"] = value
        elif part and key in ("offset", "size"):
            part[key] = [float(v) for v in value.split()]
        elif part and key == "color":
            part["look"][0] = tuple(value.split())
        elif part and key == "team":
            part["look"][1] = float(value)
    return found


def pose(part, rig):
    centre = [a + b for a, b in zip(rig[part["bone"]], part["offset"])]
    part["lo"] = [c - s / 2 for c, s in zip(centre, part["size"])]
    part["hi"] = [c + s / 2 for c, s in zip(centre, part["size"])]


def inside(part, point):
    return all(part["lo"][i] - SLOP <= point[i] <= part["hi"][i] + SLOP for i in range(3))


def exposed(a, b, axis, level, outward, others):
    """Does any of a's and b's overlap show, on the plane at `level` looking from `outward`?

    Cuts the overlap at the edge of every other part, so each cell is all covered or all not.
    """
    across = [i for i in range(3) if i != axis]
    spans = []
    for i in across:
        start, end = max(a["lo"][i], b["lo"][i]), min(a["hi"][i], b["hi"][i])
        if end - start <= SLOP:
            return False
        cuts = {start, end}
        for other in others:
            cuts.update(v for v in (other["lo"][i], other["hi"][i]) if start < v < end)
        cuts = sorted(cuts)
        spans.append([(p + q) / 2 for p, q in zip(cuts, cuts[1:]) if q - p > SLOP])

    for u, v in itertools.product(*spans):
        point = [0.0, 0.0, 0.0]
        point[across[0]], point[across[1]], point[axis] = u, v, level + outward * SLOP * 10
        if not any(inside(other, point) for other in others):
            return True
    return False


def faults(model):
    for a, b in itertools.combinations(model, 2):
        if a["look"] == b["look"]:
            continue
        others = [part for part in model if part is not a and part is not b]
        for axis, side in itertools.product(range(3), ("lo", "hi")):
            outward = 1 if side == "hi" else -1
            level_a, level_b = a[side][axis], b[side][axis]
            gap = abs(level_a - level_b)
            if gap >= MIN_GAP - SLOP:
                continue
            level = max(level_a * outward, level_b * outward) * outward
            if exposed(a, b, axis, level, outward, others):
                face = ("+" if outward > 0 else "-") + "xyz"[axis]
                yield f"{a['name']} and {b['name']}: {face} faces {gap:.3f} apart"


def main():
    rig = bones(MODELS / "humanoid.skeleton")
    failed = False
    for path in sorted(MODELS.glob("*.emodel")):
        model = parts(path)
        for part in model:
            pose(part, rig)
        for fault in faults(model):
            print(f"{path.name}: {fault}")
            failed = True
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
