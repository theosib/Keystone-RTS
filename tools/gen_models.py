#!/usr/bin/env python3
"""Generate the rigs, animation clips, and the animals' models in models/.

Clips are written out rather than hand-edited because keyframes hold quaternions.
Every keyframe repeats the bone's rest position: a clip replaces the bone's pose, so each rig has
clips of its own.
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


def skeleton(bones=BONES, title="Shared humanoid rig"):
    lines = [f"# {title}. Feet at y=0, facing +Z."]
    for name, (parent, pos) in bones.items():
        lines += [f"bone: {name}", f"parent: {parent or '-'}", "position: {} {} {}".format(*pos)]
    return "\n".join(lines) + "\n"


def clip(name, duration, tracks, loop=True, bones=BONES):
    lines = [f"name: {name}", f"duration: {duration}", f"loop: {'true' if loop else 'false'}"]
    for bone, frames in tracks.items():
        pos = bones[bone][1]
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


# Four legs: the body hangs from its middle, the neck from its front, and the legs from its corners.
# A deer stands tall on long legs; a boar is low, long and heavy in the shoulder.
def quadruped(hip, length, width, neck_up):
    half = length / 2
    return {
        "body": (None, (0, hip, 0)),
        "neck": ("body", (0, neck_up, half)),
        "tail": ("body", (0, 0.05, -half)),
        "leg_fl": ("body", (-width, 0, half - 0.08)),
        "leg_fr": ("body", (width, 0, half - 0.08)),
        "leg_bl": ("body", (-width, 0, -half + 0.08)),
        "leg_br": ("body", (width, 0, -half + 0.08)),
    }


DEER = quadruped(hip=0.78, length=1.0, width=0.14, neck_up=0.12)
BOAR = quadruped(hip=0.42, length=0.95, width=0.16, neck_up=0.02)


def box(name, bone, offset, size, color):
    return "\n".join([f"part: {name}", f"bone: {bone}", "offset: {} {} {}".format(*offset),
                      "size: {} {} {}".format(*size), "color: {} {} {}".format(*color)]) + "\n"


def legs(length, thick, color, hoof):
    out = ""
    for bone in ("leg_fl", "leg_fr", "leg_bl", "leg_br"):
        # The leg ends inside its hoof, which stands a little proud of it all round
        out += box(bone, bone, (0, -(length - 0.06) / 2, 0), (thick, length - 0.06, thick), color)
        out += box(bone + "_hoof", bone, (0, -length + 0.05, 0.01), (thick + 0.06, 0.1, thick + 0.08), hoof)
    return out


def deer_model():
    # Entities are drawn brighter than their colours (1.6 times the light), so these are dark
    coat, pale, dark = (0.46, 0.27, 0.13), (0.62, 0.55, 0.44), (0.14, 0.10, 0.07)
    return ("# A red deer: long legs, a pale rump, and antlers\nskeleton: deer\n"
            "anim:idle: deer_idle\nanim:walk: deer_walk\nanim:graze: deer_graze\n\n"
            + box("body", "body", (0, 0.1, 0), (0.42, 0.42, 1.0), coat)
            + box("belly", "body", (0, -0.08, 0), (0.3, 0.1, 0.8), pale)
            + box("rump", "body", (0, 0.12, -0.5), (0.36, 0.3, 0.06), pale)
            + box("neck", "neck", (0, 0.2, 0.06), (0.2, 0.42, 0.22), coat)
            + box("head", "neck", (0, 0.44, 0.2), (0.2, 0.2, 0.34), coat)
            + box("muzzle", "neck", (0, 0.37, 0.4), (0.12, 0.12, 0.08), dark)
            + box("ear_l", "neck", (-0.13, 0.56, 0.1), (0.08, 0.12, 0.04), coat)
            + box("ear_r", "neck", (0.13, 0.56, 0.1), (0.08, 0.12, 0.04), coat)
            + box("antler_l", "neck", (-0.06, 0.72, 0.12), (0.04, 0.3, 0.04), pale)
            + box("antler_r", "neck", (0.06, 0.72, 0.12), (0.04, 0.3, 0.04), pale)
            + box("tine_l", "neck", (-0.11, 0.78, 0.18), (0.1, 0.04, 0.04), pale)
            + box("tine_r", "neck", (0.11, 0.78, 0.18), (0.1, 0.04, 0.04), pale)
            + box("tail", "tail", (0, -0.02, -0.03), (0.08, 0.12, 0.06), pale)
            + legs(0.74, 0.08, coat, dark))


def boar_model():
    bristle, dark, tusk = (0.20, 0.13, 0.08), (0.10, 0.07, 0.05), (0.62, 0.60, 0.54)
    return ("# A wild boar: low, heavy in the shoulder, with tusks\nskeleton: boar\n"
            "anim:idle: boar_idle\nanim:walk: boar_walk\nanim:graze: boar_graze\nanim:gore: boar_gore\n\n"
            + box("body", "body", (0, 0.14, -0.04), (0.5, 0.48, 0.92), bristle)
            + box("shoulders", "body", (0, 0.2, 0.3), (0.54, 0.58, 0.34), bristle)
            + box("ridge", "body", (0, 0.48, 0.1), (0.12, 0.08, 0.7), dark)
            + box("head", "neck", (0, 0.08, 0.2), (0.36, 0.34, 0.36), bristle)
            + box("snout", "neck", (0, 0.02, 0.42), (0.2, 0.16, 0.14), dark)
            + box("tusk_l", "neck", (-0.12, 0.04, 0.46), (0.04, 0.12, 0.04), tusk)
            + box("tusk_r", "neck", (0.12, 0.04, 0.46), (0.04, 0.12, 0.04), tusk)
            + box("ear_l", "neck", (-0.14, 0.3, 0.12), (0.08, 0.1, 0.04), dark)
            + box("ear_r", "neck", (0.14, 0.3, 0.12), (0.08, 0.1, 0.04), dark)
            + box("tail", "tail", (0, -0.02, -0.05), (0.04, 0.16, 0.04), dark)
            + legs(0.38, 0.11, bristle, dark))


def animal_clips(prefix, bones, stride, step, gore=False):
    """Idle, walk, and head down to graze. A walk moves diagonal pairs together."""
    clips = {
        "idle": clip("idle", 3.0, {
            "neck": [(0.0, 0.0), (1.4, 0.06), (1.6, (0.0, 0.25)), (2.6, (0.0, 0.25)), (3.0, 0.0)],
            "tail": swing(0.2, 1.5),
        }, bones=bones),
        "walk": clip("walk", step, {
            "leg_fl": swing(stride, step), "leg_br": swing(stride, step),
            "leg_fr": swing(stride, step, -1), "leg_bl": swing(stride, step, -1),
            "neck": swing(0.05, step / 2),
        }, bones=bones),
        "graze": clip("graze", 2.0, {
            "neck": [(0.0, 1.1), (0.5, 1.2), (1.0, 1.1), (1.5, 1.2), (2.0, 1.1)],
            "tail": swing(0.25, 1.0),
        }, bones=bones),
    }
    if gore:
        # Head down and driven up into whatever's in front
        clips["gore"] = clip("gore", 0.8, {
            "neck": [(0.0, 0.0), (0.3, 0.55), (0.45, -0.45), (0.8, 0.0)],
            "body": [(0.0, 0.0), (0.3, 0.08), (0.45, -0.1), (0.8, 0.0)],
        }, loop=False, bones=bones)
    for name, text in clips.items():
        (OUT / f"{prefix}_{name}.anim").write_text(text)


def animals():
    (OUT / "deer.skeleton").write_text(skeleton(DEER, "A deer"))
    (OUT / "boar.skeleton").write_text(skeleton(BOAR, "A wild boar"))
    animal_clips("deer", DEER, stride=0.55, step=0.5)
    animal_clips("boar", BOAR, stride=0.6, step=0.4, gore=True)
    (OUT / "deer.emodel").write_text(deer_model())
    (OUT / "boar.emodel").write_text(boar_model())


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
    # A load held before the chest in both hands. The model lays it over idle and walk while a
    # Hand holds one (`hold_pose`), and it has only the arms, so the legs go on walking.
    (OUT / "humanoid_carry.anim").write_text(clip("carry", 1.0, {
        "arm_l": [(0.0, -1.05), (1.0, -1.05)],
        "arm_r": [(0.0, -1.05), (1.0, -1.05)],
    }))
    animals()
    print(f"wrote {len(list(OUT.glob('*.anim')))} clips to {OUT}")


if __name__ == "__main__":
    main()
