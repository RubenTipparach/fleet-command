"""Fetch CC0 texture sets for Fleet Command's props (the freighters' containers first: openspec/changes/freighters, task 2.5;
owner, 2026-11-07: "you need like better cargo textures, lookem up online for cc0 options").

Poly Haven publishes every asset under CC0 (https://polyhaven.com/license); this script still asks the asset's own page
for the licence, and refuses any asset whose page does not say CC0, as tools/audio/fetch_cc0.py does for sounds. The
downloads are source material, never committed (tools/materials/raw/ is ignored); what is committed is what the Blender
generator bakes from them, and the record of every original in assets/containers/SOURCES.md (its URL, author, licence and
SHA-256), so a fetch can be checked byte for byte.

    python3 tools/textures/fetch_cc0.py container_side [--res 2k]

Writes tools/materials/raw/cc0/<asset>/<asset>_<map>_<res>.jpg and prints each file's SHA-256.
"""
import argparse
import hashlib
import json
import os
import sys
import urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RAW = os.path.join(ROOT, "tools", "materials", "raw", "cc0")
API = "https://api.polyhaven.com"
MAPS = ("Diffuse", "nor_gl", "Rough", "AO", "Displacement")   # OpenGL normals: three.js and Godot read them


def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": "fleet-command-texture-fetch"})
    with urllib.request.urlopen(req, timeout=60) as r:
        return r.read()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("asset")
    ap.add_argument("--res", default="2k")
    a = ap.parse_args()
    info = json.loads(get(f"{API}/info/{a.asset}"))
    page = get(f"https://polyhaven.com/a/{a.asset}").decode("utf-8", "replace")
    if "CC0" not in page and "cc0" not in page:
        sys.exit(f"refused: the page for {a.asset} does not say CC0")
    files = json.loads(get(f"{API}/files/{a.asset}"))
    out = os.path.join(RAW, a.asset)
    os.makedirs(out, exist_ok=True)
    print(f"{info.get('name', a.asset)} by {', '.join(info.get('authors', {}))}: CC0 (https://polyhaven.com/a/{a.asset})")
    for m in MAPS:
        url = files[m][a.res]["jpg"]["url"]
        data = get(url)
        path = os.path.join(out, os.path.basename(url))
        open(path, "wb").write(data)
        print(f"  {m}: {url} sha256 {hashlib.sha256(data).hexdigest()}")


if __name__ == "__main__":
    main()
