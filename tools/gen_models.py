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


def quat_y(angle):
    return (0.0, math.sin(angle / 2), 0.0, math.cos(angle / 2))


def quat_mul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by,
            aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz)


def rotation(angle):
    """A keyframe's angle: a pitch about x, or (pitch, turn) for a turn about y as well. Negative
    pitch raises an arm forward; positive leans the torso forward."""
    if isinstance(angle, tuple):
        return quat_mul(quat_y(angle[1]), quat_x(angle[0]))
    return quat_x(angle)


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
                "rot: {:.5f} {:.5f} {:.5f} {:.5f}".format(*rotation(angle)),
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
    # Felling or quarrying: both hands on the haft, up over the shoulder and down hard
    chop = 0.9
    (OUT / "humanoid_chop.anim").write_text(clip("chop", chop, {
        "arm_r": [(0.0, -0.9), (0.38, -2.7), (0.5, -2.75), (0.62, -0.75), (chop, -0.9)],
        "arm_l": [(0.0, -0.8), (0.38, -2.5), (0.5, -2.55), (0.62, -0.7), (chop, -0.8)],
        "torso": [(0.0, 0.08), (0.38, -0.15), (0.5, -0.16), (0.62, 0.3), (chop, 0.08)],
    }))
    # Building: a mallet in the right hand, the left steadying the work
    build = 0.6
    (OUT / "humanoid_build.anim").write_text(clip("build", build, {
        "arm_r": [(0.0, -1.0), (0.25, -2.0), (0.38, -0.85), (build, -1.0)],
        "arm_l": [(0.0, -0.9), (build, -0.9)],
        "torso": [(0.0, 0.15), (0.25, 0.05), (0.38, 0.22), (build, 0.15)],
    }))
    # Picking berries: bent over the bush, a hand at a time
    forage = 1.0
    (OUT / "humanoid_forage.anim").write_text(clip("forage", forage, {
        "arm_r": [(0.0, -0.5), (0.25, -0.95), (0.5, -0.5), (0.75, -0.6), (forage, -0.5)],
        "arm_l": [(0.0, -0.6), (0.25, -0.5), (0.5, -0.6), (0.75, -0.95), (forage, -0.6)],
        "torso": [(0.0, 0.35), (0.5, 0.42), (forage, 0.35)],
    }))
    # A spear thrust: drawn back, then driven forward at chest height
    thrust = 0.7
    (OUT / "humanoid_thrust.anim").write_text(clip("thrust", thrust, {
        "arm_r": [(0.0, -1.2), (0.25, -0.8), (0.38, -1.65), (0.5, -1.6), (thrust, -1.2)],
        "arm_l": [(0.0, -0.9), (thrust, -0.9)],
        "torso": [(0.0, 0.0), (0.25, -0.1), (0.38, 0.18), (thrust, 0.0)],
    }))
    # A bow drawn and loosed: the bow arm straight at the mark, the other pulling the string back
    # across the chest and letting it go
    shoot = 0.8
    (OUT / "humanoid_shoot.anim").write_text(clip("shoot", shoot, {
        "arm_l": [(0.0, -1.5), (shoot, -1.5)],
        "arm_r": [(0.0, (-1.55, -0.55)), (0.15, (-1.55, -0.6)), (0.5, (-1.5, 0.35)),
                  (0.58, (-1.45, 0.45)), (0.62, (-1.3, 0.3)), (shoot, (-1.55, -0.55))],
        "torso": [(0.0, (0.0, -0.25)), (shoot, (0.0, -0.25))],
        "head": [(0.0, (0.0, 0.25)), (shoot, (0.0, 0.25))],
    }, loop=False))
    print(f"wrote {len(list(OUT.glob('*.anim')))} clips to {OUT}")


if __name__ == "__main__":
    main()
