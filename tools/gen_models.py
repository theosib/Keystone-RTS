#!/usr/bin/env python3
"""Generate the humanoid rig and animation clips in models/.

Clips are written out rather than hand-edited because keyframes hold quaternions.
Every keyframe repeats the bone's rest position: a clip replaces the bone's pose.
"""
import math
from pathlib import Path

OUT = Path(__file__).resolve().parent.parent / "models"

BONES = {
    "hips": (None, (0, 0.82, 0)),
    "torso": ("hips", (0, 0.08, 0)),
    "head": ("torso", (0, 0.62, 0)),
    "arm_l": ("torso", (-0.30, 0.52, 0)),
    "arm_r": ("torso", (0.30, 0.52, 0)),
    "leg_l": ("hips", (-0.12, 0, 0)),
    "leg_r": ("hips", (0.12, 0, 0)),
}


def quat_x(angle):
    return (math.sin(angle / 2), 0.0, 0.0, math.cos(angle / 2))


def skeleton():
    lines = ["# Shared humanoid rig. Feet at y=0, facing +Z."]
    for name, (parent, pos) in BONES.items():
        lines += [f"bone: {name}", f"parent: {parent or '-'}", "position: {} {} {}".format(*pos)]
    return "\n".join(lines) + "\n"


def clip(name, duration, tracks, loop=True):
    lines = [f"name: {name}", f"duration: {duration}", f"loop: {'true' if loop else 'false'}"]
    for bone, frames in tracks.items():
        pos = BONES[bone][1]
        lines.append(f"track: {bone}")
        for t, angle in frames:
            lines += [
                f"keyframe: {t}",
                "pos: {} {} {}".format(*pos),
                "rot: {:.5f} {:.5f} {:.5f} {:.5f}".format(*quat_x(angle)),
            ]
    return "\n".join(lines) + "\n"


def swing(amplitude, duration, phase=1):
    q = duration / 4
    return [
        (0.0, 0.0),
        (round(q, 4), amplitude * phase),
        (round(2 * q, 4), 0.0),
        (round(3 * q, 4), -amplitude * phase),
        (duration, 0.0),
    ]


def main():
    OUT.mkdir(exist_ok=True)
    (OUT / "humanoid.skeleton").write_text(skeleton())

    (OUT / "humanoid_idle.anim").write_text(clip("idle", 2.4, {
        "arm_l": swing(0.05, 2.4),
        "arm_r": swing(0.05, 2.4, -1),
        "torso": swing(0.02, 2.4),
    }))
    walk = 0.6
    (OUT / "humanoid_walk.anim").write_text(clip("walk", walk, {
        "leg_l": swing(0.65, walk),
        "leg_r": swing(0.65, walk, -1),
        "arm_l": swing(0.5, walk, -1),
        "arm_r": swing(0.5, walk),
        "torso": swing(0.03, walk / 2),
    }))
    work = 0.8
    (OUT / "humanoid_work.anim").write_text(clip("work", work, {
        "arm_r": [(0.0, -1.9), (0.3, -2.3), (0.5, -0.2), (work, -1.9)],
        "arm_l": [(0.0, -1.2), (0.3, -1.5), (0.5, -0.5), (work, -1.2)],
        "torso": [(0.0, 0.0), (0.3, -0.12), (0.5, 0.2), (work, 0.0)],
    }))
    print(f"wrote {len(list(OUT.glob('*.anim')))} clips to {OUT}")


if __name__ == "__main__":
    main()
