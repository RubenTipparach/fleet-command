#!/usr/bin/env python3
"""Compile a guide page and the local images it shows into one self-contained HTML file, so a page whose images live
beside it (docs/design/organic-bible.html and docs/design/organic-bible/*.jpg) publishes as a single artifact file and
can be recompiled whenever the page grows (owner, 2026-10-31, of the Organic Panel Bible: "just compile organic guide
as html, since it's in draft and we'll be adding to it").

Every src="..." or href="..." that names a local image file (jpg, jpeg, png, webp, svg) relative to the page is
replaced by a data URI; links to other pages and remote URLs are left as they are. The source page stays the one to
edit; the compiled file is a build product written beside the artifact's record.

    python3 tools/mockups/compile_page.py docs/design/organic-bible.html docs/artifacts/organic-bible/index.html
"""
import base64
import pathlib
import re
import sys

TYPES = {".jpg": "image/jpeg", ".jpeg": "image/jpeg", ".png": "image/png", ".webp": "image/webp", ".svg": "image/svg+xml"}
REF = re.compile(r'(src|href)="([^"#?:]+)"')


def compile_page(src, out):
    """Write out: src with every local image it references carried as a data URI. Fails naming a missing image."""
    page = pathlib.Path(src)
    text = page.read_text()

    def one(m):
        ref = m.group(2)
        kind = TYPES.get(pathlib.Path(ref).suffix.lower())
        if kind is None:
            return m.group(0)
        path = page.parent / ref
        if not path.is_file():
            raise SystemExit(f"{src}: {m.group(1)}={ref} names no file ({path})")
        return f'{m.group(1)}="data:{kind};base64,{base64.b64encode(path.read_bytes()).decode("ascii")}"'

    html = REF.sub(one, text)
    pathlib.Path(out).parent.mkdir(parents=True, exist_ok=True)
    pathlib.Path(out).write_text(html)
    return len(html.encode())


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: compile_page.py <page.html> <out.html>")
    n = compile_page(sys.argv[1], sys.argv[2])
    print(f"wrote {sys.argv[2]} ({n / 1e6:.2f} MB)")
