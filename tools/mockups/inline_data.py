#!/usr/bin/env python3
"""Write data files into mockup pages between INLINE markers, so a published page carries its data and the data keeps
one source in data/ (after Star Crew's tools/mockups/inline.py). A marker pair

    <!-- INLINE data/space/skies/quiet.json -->
    ...
    <!-- /INLINE -->

is filled with <script type="application/json" data-src="data/space/skies/quiet.json">minified JSON</script>.

    python3 tools/mockups/inline_data.py docs/mockups/game-flow.html [--check]

--check fails (exit 1) when a page's inlined data differs from the file, writing nothing.
"""
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
MARK = re.compile(r"(<!-- INLINE (\S+) -->\n)(.*?)(<!-- /INLINE -->)", re.S)


def fill(text):
    def one(m):
        src = m.group(2)
        data = json.dumps(json.loads((ROOT / src).read_text()), separators=(",", ":"))
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
