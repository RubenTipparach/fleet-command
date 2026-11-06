#!/usr/bin/env python3
"""Check that every published artifact is tracked as HTML in the repository (CLAUDE.md section 4; owner, 2026-11-04:
"add to claude md that all artifactrs must be tracked as html in the repo").

Reads the mockup tracker's own lists (docs/mockups/tracker.html: MOCKUPS, GUIDES, MISC), the one list of every artifact,
and for each entry with a link checks that:
  - its page is an .html file committed in git (git ls-files), and
  - its link is recorded in a docs/artifacts/<name>/README.md.
The tracker itself is checked the same way. It also checks the tracker's own page:
  - its script parses (node --check): an unescaped quote in one entry stops the whole page, every list empty (found
    2026-11-06, owner: "what happened?"), and
  - every summary is one short line, at most SUMMARY_MAX characters, with no "New in v" history (owner, 2026-11-06: "why
    did you add so much detail to the mockup tracker? the mockups themselves already have documentation, or you can add
    documentation to openspec"): a mockup's history goes in its page, its OpenSpec change and docs/artifacts/<name>/.
Exits 1 and names each failure.

    python3 tools/mockups/artifact_check.py
"""
import glob
import os
import re
import subprocess
import sys
import tempfile

TRACKER = 'docs/mockups/tracker.html'
TRACKER_URL = 'https://claude.ai/artifact/8VqA9YjhCrgoiY2CGR1Qxu'
SUMMARY_MAX = 200  # characters: one line on the tracker, what the page is


def tracker_page(src):
    """The tracker's own page: its script parses, and every summary is one short line. Returns the failures."""
    problems = []
    js = src[src.rindex('<script>') + len('<script>'):src.rindex('</script>')]
    with tempfile.NamedTemporaryFile('w', suffix='.js', delete=False, encoding='utf-8') as f:
        f.write(js)
    r = subprocess.run(['node', '--check', f.name], capture_output=True, text=True)
    os.unlink(f.name)
    if r.returncode:
        problems.append('its script does not parse: ' + (r.stderr.strip().splitlines() or ['?'])[-1])
    for m in re.finditer(r"(?:id|title): '((?:[^'\\]|\\.)*)'[^{}]*?summary: '((?:[^'\\]|\\.)*)'", src, re.S):
        name, text = m.groups()
        if len(text) > SUMMARY_MAX:
            problems.append(f'{name}: summary is {len(text)} characters (one line, at most {SUMMARY_MAX})')
        if re.search(r'New in v\d', text):
            problems.append(f'{name}: summary carries version history ("New in v...")')
    return problems


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
    for problem in tracker_page(src):
        bad += 1
        print(f'FAIL tracker page: {problem}')
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
