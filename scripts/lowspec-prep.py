#!/usr/bin/env python3
"""lowspec-prep: readies a checkout for the low-spec mode on a small machine (a Raspberry Pi 5; owner, 2026-11-12: "lower
resolution of UI down to max 512 on each texture for the pi build"). Run it once on that machine's own checkout, before
Godot's first import: it caps every texture's import at 512 px on its longest side (process/size_limit) and turns on the
ETC2/ASTC texture compression an ARM GPU reads (rendering/textures/vram_compression/import_etc2_astc). The changes are that
checkout's only and are never committed (the script refuses a checkout whose changes would mix with others: run it on a
clean tree). The game scales every nine-slice and whole piece by its texture's own size (Kit/RenderedPieces), so a capped
image draws in the same place, softer.

    python3 scripts/lowspec-prep.py [--limit 512]
"""
import argparse
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--limit", type=int, default=512, help="longest side of any texture, px")
    ap.add_argument("--force", action="store_true", help="run on a tree with changes")
    a = ap.parse_args()
    if a.limit < 16:
        sys.exit("--limit: at least 16 px")
    dirty = subprocess.run(["git", "status", "--porcelain"], cwd=ROOT, capture_output=True, text=True).stdout.strip()
    if dirty and not a.force:
        sys.exit("lowspec-prep: the tree has changes; run it on a clean checkout (or --force)")
    n = 0
    for f in ROOT.rglob("*.import"):
        rel = f.relative_to(ROOT)
        if any(p in (".godot", ".tools", ".git", "node_modules") for p in rel.parts):
            continue
        # the baked planet maps are the low-spec mode's own textures, made for it at their size (PlanetBaker); capping
        # them would blur the planets the mode exists to show
        if rel.parts[:3] == ("assets", "space", "planets_baked"):
            continue
        text = f.read_text()
        if 'importer="texture"' not in text:
            continue
        new, k = re.subn(r"^process/size_limit=\d+$", f"process/size_limit={a.limit}", text, flags=re.M)
        if k == 0:
            new = text.rstrip("\n") + f"\nprocess/size_limit={a.limit}\n"
        if new != text:
            f.write_text(new)
            n += 1
    proj = ROOT / "project.godot"
    p = proj.read_text()
    key = "textures/vram_compression/import_etc2_astc=true"
    if key not in p:
        if "\n[rendering]\n" in p:
            p = p.replace("\n[rendering]\n", f"\n[rendering]\n\n{key}\n", 1)
        else:
            p += f"\n[rendering]\n\n{key}\n"
        proj.write_text(p)
    print(f"lowspec-prep: {n} textures capped at {a.limit} px; ETC2/ASTC import on. Now import: godot --headless --path . --import")
    return 0


if __name__ == "__main__":
    sys.exit(main())
