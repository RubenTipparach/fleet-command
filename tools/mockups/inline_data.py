#!/usr/bin/env python3
"""Write data files into mockup pages between INLINE markers, so a published page carries its data and the data keeps
one source in data/ (after Star Crew's tools/mockups/inline.py). A marker pair

    <!-- INLINE data/space/skies/quiet.json -->
    ...
    <!-- /INLINE -->

is filled with <script type="application/json" data-src="data/space/skies/quiet.json">minified JSON</script>.

A UI kit manifest (a JSON file with "pieces", each naming its image as "file" beside the manifest) is inlined with each
piece's image as a "data" URI (high-quality WebP when smaller than the PNG; any other JSON, such as a kit's shapes file, is inlined as it is), so a page can draw a kit rendered in Blender without fetching anything:

    <!-- INLINE assets/ui/kethran/manifest.json -->

A JavaScript file (a mockup module, such as a faction skin in docs/mockups/skins/) is inlined as a script element.

The ship schematics' manifest (assets/ships/schematics/manifest.json) is inlined with each ship's hull and wire layers as
data URIs (hull_data, wire_data), so the shop draws the same pictures as the game.

A glTF binary (an approved ship from assets/ships/, exported by tools/mockups/export_ships.mjs) is inlined as base64 in
a script element of type application/octet-stream, which the page decodes and hands to three.js's GLTFLoader.parse(), so
nothing is fetched (the artifact host serves no .glb).

    python3 tools/mockups/inline_data.py docs/mockups/game-flow.html [--check]

--check fails (exit 1) when a page's inlined data differs from the file, writing nothing.
"""
import base64
import io
import json
import pathlib
import re
import sys

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
MARK = re.compile(r"(<!-- INLINE (\S+) -->\n)(.*?)(<!-- /INLINE -->)", re.S)


def image_uri(png_path, lossless=False, quality=92):
    """A kit image as a data URI: high-quality WebP (alpha kept) when that is smaller than the PNG, else the PNG. The
    committed renders stay PNG; only the published page carries WebP, because a page holding ten rendered kits as PNG
    passed the artifact host's 16 MiB limit (2026-10-31). The encoder's output is deterministic for the same input.
    lossless: a map that carries data, not colour (a ship's pane map), kept exact: lossy WebP's colour subsampling would
    mix its channels. quality: the WebP quality; a manifest may set its own ("webp_quality"), as the Ssarith Hunt's ship maps
    do, six ships of seven 2048 px maps each being more than the host's 16 MiB at 92."""
    png = png_path.read_bytes()
    buf = io.BytesIO()
    if lossless:
        Image.open(io.BytesIO(png)).save(buf, "WEBP", lossless=True, method=6)
    else:
        Image.open(io.BytesIO(png)).save(buf, "WEBP", quality=quality, method=6)
    webp = buf.getvalue()
    if len(webp) < len(png):
        return "data:image/webp;base64," + base64.b64encode(webp).decode("ascii")
    return "data:image/png;base64," + base64.b64encode(png).decode("ascii")


def image_file(png_path, out_path, lossless=False, quality=92):
    """A map written beside the page as WebP (a manifest with "maps_dir"): the page loads it as a file the artifact
    publishes with it, so maps too large to inline (4096 px ship maps: owner, 2026-11-06, of a close-up, "close up ship
    details") stay out of the 16 MiB page. Deterministic for the same input; rewritten only when it changes."""
    buf = io.BytesIO()
    im = Image.open(png_path)
    if lossless:
        im.save(buf, "WEBP", lossless=True, method=6)
    else:
        im.save(buf, "WEBP", quality=quality, method=6)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    if not out_path.exists() or out_path.read_bytes() != buf.getvalue():
        out_path.write_bytes(buf.getvalue())


def load(src, page=None):
    """The JSON at src, with a kit manifest's piece images carried as data URIs, or, when the manifest names a
    "maps_dir" (relative to the repository), written there as WebP files and referred to by their path from the page."""
    path = ROOT / src
    obj = json.loads(path.read_text())
    if isinstance(obj, dict) and isinstance(obj.get("pieces"), dict) and all("file" in p for p in obj["pieces"].values()):
        md = obj.get("maps_dir")
        for name, piece in obj["pieces"].items():
            if md and page is not None:
                out = ROOT / md / (pathlib.Path(piece["file"]).stem + ".webp")
                image_file(path.parent / piece["file"], out, piece.get("lossless", False), obj.get("webp_quality", 92))
                piece["data"] = str(out.relative_to(page.resolve().parent)).replace("\\", "/")
            else:
                piece["data"] = image_uri(path.parent / piece["file"], piece.get("lossless", False), obj.get("webp_quality", 92))
    # the ship schematics' manifest (assets/ships/schematics/): each ship's two white layers carried as data URIs
    if isinstance(obj, dict) and isinstance(obj.get("ships"), list) and all("hull" in s and "wire" in s for s in obj["ships"]):
        for ship in obj["ships"]:
            ship["hull_data"] = image_uri(path.parent / ship["hull"])
            ship["wire_data"] = image_uri(path.parent / ship["wire"])
    return obj


def fill(text, page=None):
    def one(m):
        src = m.group(2)
        if src.endswith(".glb"):
            b64 = base64.b64encode((ROOT / src).read_bytes()).decode("ascii")
            return f'{m.group(1)}<script type="application/octet-stream" data-src="{src}">{b64}</script>\n{m.group(4)}'
        if src.endswith(".js"):
            code = (ROOT / src).read_text()
            if "</script" in code:
                raise SystemExit(f"{src} contains a closing script tag and cannot be inlined")
            return f'{m.group(1)}<script data-src="{src}">\n{code}</script>\n{m.group(4)}'
        data = json.dumps(load(src, page), separators=(",", ":"))
        return f'{m.group(1)}<script type="application/json" data-src="{src}">{data}</script>\n{m.group(4)}'
    return MARK.sub(one, text)


def main():
    check = "--check" in sys.argv
    stale = False
    for page in [a for a in sys.argv[1:] if not a.startswith("--")]:
        p = pathlib.Path(page)
        text = p.read_text()
        new = fill(text, p)
        if new != text:
            stale = True
            if not check:
                p.write_text(new)
            print(("STALE " if check else "wrote ") + page)
    sys.exit(1 if check and stale else 0)


if __name__ == "__main__":
    main()
