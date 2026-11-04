#!/usr/bin/env python3
"""Check that every published artifact is tracked as HTML in the repository (CLAUDE.md section 4; owner, 2026-11-04:
"add to claude md that all artifactrs must be tracked as html in the repo").

Reads the mockup tracker's own lists (docs/mockups/tracker.html: MOCKUPS, GUIDES, MISC), the one list of every artifact,
and for each entry with a link checks that:
  - its page is an .html file committed in git (git ls-files), and
  - its link is recorded in a docs/artifacts/<name>/README.md.
The tracker itself is checked the same way. Exits 1 and names each failure.

    python3 tools/mockups/artifact_check.py
"""
import glob
import re
import subprocess
import sys

TRACKER = 'docs/mockups/tracker.html'
TRACKER_URL = 'https://claude.ai/artifact/8VqA9YjhCrgoiY2CGR1Qxu'


def tracked(path):
    return subprocess.run(['git', 'ls-files', '--error-unmatch', path], capture_output=True).returncode == 0


def main():
    src = open(TRACKER, encoding='utf-8').read()
    # Each entry is one object literal: title, page and url, in either order of id and title.
    entries = [(TRACKER.split('/')[-1], TRACKER, TRACKER_URL)]
    for m in re.finditer(r"\{[^{}]*?title: '((?:[^'\\]|\\.)*)'[^{}]*?page: '([^']*)'[^{}]*?url: '([^']*)'", src, re.S):
        entries.append(m.groups())
    readmes = {p: open(p, encoding='utf-8').read() for p in glob.glob('docs/artifacts/*/README.md')}
    bad = 0
    for title, page, url in entries:
        if not url:
            print(f'skip {title}: not published')
            continue
        problems = []
        if not page.endswith('.html'):
            problems.append(f'page {page} is not .html')
        elif not tracked(page):
            problems.append(f'page {page} is not committed')
        if not any(url in text for text in readmes.values()):
            problems.append(f'link {url} is in no docs/artifacts/*/README.md')
        if problems:
            bad += 1
            print(f'FAIL {title}: ' + '; '.join(problems))
        else:
            print(f'ok   {title}: {page}')
    print(f'{len(entries)} entries, {bad} failing')
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main())
