#!/usr/bin/env python3
"""Make a macOS executable self-contained.

Copies every library it needs, and every library those need, into a Frameworks directory, and
points them all at each other there. Prints the newest macOS that any of them was built for, which
is the oldest one the app can run on. Everything this changes loses its signature: sign afterwards.

    bundle_dylibs.py EXECUTABLE FRAMEWORKS [EXTRA.dylib ...]
"""
import os
import re
import shutil
import subprocess
import sys


def otool(*args):
    return subprocess.run(["otool", *args], check=True, capture_output=True, text=True).stdout


def install_name_tool(*args):
    subprocess.run(["install_name_tool", *args], check=True, capture_output=True)


def is_system(name):
    return name.startswith(("/usr/lib/", "/System/"))


def dependencies(path):
    names = [line.split(" (")[0].strip() for line in otool("-L", path).splitlines()[1:]]
    own = otool("-D", path).splitlines()[1:]
    return [name for name in names if name not in own]


def rpaths(path):
    return re.findall(r"cmd LC_RPATH\n\s+cmdsize \d+\n\s+path (.*?) \(offset", otool("-l", path))


def resolve(name, loader, searched):
    """The file a dependency really is: loader's own rpaths, then those of whatever loaded it."""
    here = os.path.dirname(loader)
    candidates = [name]
    if name.startswith("@rpath/"):
        candidates = [os.path.join(p, name[len("@rpath/"):]) for p in searched]
    for candidate in candidates:
        candidate = candidate.replace("@loader_path", here).replace("@executable_path", here)
        if os.path.exists(candidate):
            return os.path.realpath(candidate)
    sys.exit(f"can't find {name}, needed by {loader}")


def main():
    executable, frameworks, *extra = sys.argv[1:]
    os.makedirs(frameworks, exist_ok=True)

    bundled = {}  # File name in Frameworks -> where it came from
    queue = [(executable, [])]
    for path in extra:
        name = os.path.basename(path)
        bundled[name] = os.path.realpath(path)
        shutil.copyfile(path, os.path.join(frameworks, name))
        os.chmod(os.path.join(frameworks, name), 0o755)
        queue.append((os.path.join(frameworks, name), []))

    while queue:
        path, inherited = queue.pop()
        searched = rpaths(path) + inherited
        for name in dependencies(path):
            if is_system(name):
                continue
            local = os.path.basename(name)
            if local not in bundled:
                source = resolve(name, path, searched)
                bundled[local] = source
                copy = os.path.join(frameworks, local)
                shutil.copyfile(source, copy)
                os.chmod(copy, 0o755)
                queue.append((copy, searched))
            install_name_tool("-change", name, f"@rpath/{local}", path)
        for rpath in rpaths(path):
            install_name_tool("-delete_rpath", rpath, path)

    install_name_tool("-add_rpath", "@executable_path/../Frameworks", executable)
    newest = (0,)
    for local in bundled:
        copy = os.path.join(frameworks, local)
        install_name_tool("-id", f"@rpath/{local}", copy)
        install_name_tool("-add_rpath", "@loader_path", copy)
        for found in re.findall(r"minos (\d+(?:\.\d+)*)", otool("-l", copy)):
            newest = max(newest, tuple(int(part) for part in found.split(".")))
    print(".".join(str(part) for part in newest))


if __name__ == "__main__":
    main()
