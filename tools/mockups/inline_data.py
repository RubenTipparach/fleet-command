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


def image_uri(png_path):
    """A kit image as a data URI: high-quality WebP (alpha kept) when that is smaller than the PNG, else the PNG. The
    committed renders stay PNG; only the published page carries WebP, because a page holding ten rendered kits as PNG
    passed the artifact host's 16 MiB limit (2026-10-31). The encoder's output is deterministic for the same input."""
    png = png_path.read_bytes()
    buf = io.BytesIO()
    Image.open(io.BytesIO(png)).save(buf, "WEBP", quality=92, method=6)
    webp = buf.getvalue()
    if len(webp) < len(png):
        return "data:image/webp;base64," + base64.b64encode(webp).decode("ascii")
    return "data:image/png;base64," + base64.b64encode(png).decode("ascii")


def load(src):
    """The JSON at src, with a kit manifest's piece images carried as data URIs."""
    path = ROOT / src
    obj = json.loads(path.read_text())
    if isinstance(obj, dict) and isinstance(obj.get("pieces"), dict) and all("file" in p for p in obj["pieces"].values()):
        for piece in obj["pieces"].values():
            piece["data"] = image_uri(path.parent / piece["file"])
    return obj


def fill(text):
    def one(m):
        src = m.group(2)
        if src.endswith(".js"):
            code = (ROOT / src).read_text()
            if "</script" in code:
                raise SystemExit(f"{src} contains a closing script tag and cannot be inlined")
            return f'{m.group(1)}<script data-src="{src}">\n{code}</script>\n{m.group(4)}'
        data = json.dumps(load(src), separators=(",", ":"))
        return f'{m.group(1)}<script type="application/json" data-src="{src}">{data}</script>\n{m.group(4)}'
    return MARK.sub(one, text)


def main():
    check = "--check" in sys.argv
    stale = False
    for page in [a for a in sys.argv[1:] if not a.startswith("--")]:
        p = pathlib.Path(page)
        text = p.read_text()
        new = fill(text)
        if new != text:
            stale = True
            if not check:
                p.write_text(new)
            print(("STALE " if check else "wrote ") + page)
    sys.exit(1 if check and stale else 0)


if __name__ == "__main__":
    main()
