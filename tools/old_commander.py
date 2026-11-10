#!/usr/bin/env python3
"""The commander as it was at a commit, with every name it defines given an old_ prefix, so that it
can play the current one in tools/duel.sh. Writes it to stdout.

  old_commander.py COMMIT
"""
import re
import subprocess
import sys

source = subprocess.run(["git", "show", f"{sys.argv[1]}:scripts/commander.fsc"], capture_output=True,
                        text=True, check=True).stdout
names = set(re.findall(r"^fn ([a-z_]+)", source, re.M)) | set(re.findall(r"^set ([A-Z_]+)", source, re.M))
for name in sorted(names, key=len, reverse=True):
    source = re.sub(rf"(?<![A-Za-z_.:]){name}(?![A-Za-z_])", f"old_{name}", source)
print(source)
