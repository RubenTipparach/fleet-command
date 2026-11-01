#!/usr/bin/env python3
"""Build the survey's picture options for the Vhorr Brood (docs/design/survey.html, section W).

The owner, 2026-11-01: "For every question in the survey please show the images specifically that you are asking me
about. Don't assume anything please generate image options for every component in the survey you are asking about. And
don't show kits. Show specifically the options you are asking me to pick". So every option of every question carries its
own picture of exactly that component in exactly that option.

Inputs (scratch, not committed; the designs are not approved, Organic Panel Bible rule 0):
- one render of the whole desktop console per style, from
  `blender ... build_ui_vhorr_organic.py -- --style <style> --rest-only --out <renders>/<style>`;
- the key options sheet (a larva key, a wart key and a tab wart, idle and pressed) and its JSON, `--keys <png>`;
- a console-skins page that draws organic slots with their bleed (`--page`).

For each style, the page is copied with the style's renders inlined as the Vhorr organic kit, and
tools/mockups/survey_option_shots.mjs screenshots each component alone (labels and readouts on top, as in play). The
crops are written as docs/design/survey/<component>-<letter>.jpg and inlined into survey.html between its IMGS markers.

    python3 tools/mockups/survey_option_images.py --renders /tmp/sv --page /tmp/vb_page_with_bleed.html --keys /tmp/sv/keys.png
    python3 tools/mockups/survey_option_images.py --inline-only      # re-inline the committed pictures
"""
import argparse
import base64
import io
import json
import os
import pathlib
import re
import subprocess

from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "docs" / "design" / "survey"
SURVEY = ROOT / "docs" / "design" / "survey.html"
STYLES = {"A": "ridge", "B": "plain", "C": "crown", "D": "wrap", "E": "hang", "F": "web", "G": "feelers", "H": "mixed"}
COMPONENTS = ("strip", "ssd", "helm", "weapons", "bottom", "nameplate")
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf"
MAX_W = 900          # px: a picture's width in the page (thumbnail and the enlarged view)
QUALITY = 82


def webp_uri(png_path):
    png = png_path.read_bytes()
    buf = io.BytesIO()
    Image.open(io.BytesIO(png)).save(buf, "WEBP", quality=92, method=6)
    data = buf.getvalue() if len(buf.getvalue()) < len(png) else png
    kind = "webp" if data is not png else "png"
    return f"data:image/{kind};base64," + base64.b64encode(data).decode("ascii")


def style_page(page, render_dir, dst):
    """The page with the style's renders as the Vhorr organic kit."""
    man = json.loads((render_dir / "manifest.json").read_text())
    for p in man["pieces"].values():
        p["data"] = webp_uri(render_dir / p["file"])
    text = page.read_text()
    tag = '<script type="application/json" data-src="assets/ui/vhorr/organic/manifest.json">'
    i = text.index(tag) + len(tag)
    j = text.index("</script>", i)
    dst.write_text(text[:i] + json.dumps(man, separators=(",", ":")) + text[j:])


def save_jpg(img, path):
    img = img.convert("RGB")
    if img.width > MAX_W:
        img = img.resize((MAX_W, round(img.height * MAX_W / img.width)), Image.LANCZOS)
    img.save(path, "JPEG", quality=QUALITY, optimize=True, progressive=True)


def keys_pictures(png, meta):
    """The key options: each kind idle beside pressed, the label drawn on its metal button (sinking with it when
    pressed), over the game's dark."""
    sheet = Image.open(png).convert("RGBA")
    d = json.loads(pathlib.Path(meta).read_text())
    px = d["px_per_mm"]
    caps = d["caps"]
    rows = {"larva": (8, 80), "wart": (84, 168), "tab": (174, 232)}   # mm: each kind's band on the sheet
    names = {"larva": "Engage", "wart": "Engage", "tab": "Normal"}
    for kind, (ya, yb) in rows.items():
        band = sheet.crop((0, ya * px, sheet.width, yb * px))
        bg = Image.new("RGBA", band.size, (11, 9, 16, 255))
        bg.alpha_composite(band)
        dr = ImageDraw.Draw(bg)
        for c in caps:
            x0, y0, x1, y1 = c["rect"]
            if not (ya <= y0 <= yb):
                continue
            ckind = "tab" if c["kind"] == "tab" else ("larva" if y0 < 80 else "wart")
            if ckind != kind:
                continue
            drop = 2.0 if c["state"] == "pressed" else 0.0
            cx, cy = (x0 + x1) / 2 * px, ((y0 + y1) / 2 + drop - ya) * px
            col = (186, 242, 150) if c["state"] == "active" else (255, 214, 160)
            size = 15 * px   # the label fits its button: at most 80% of its width and 60% of its height
            while size > 8:
                fnt = ImageFont.truetype(FONT, size)
                l, t, r, b = dr.textbbox((0, 0), names[kind], font=fnt)
                if r - l <= 0.8 * (x1 - x0) * px and b - t <= 0.6 * (y1 - y0) * px:
                    break
                size -= 1
            dr.text((cx, cy), names[kind], font=fnt, fill=col, anchor="mm")
        dr.text((10, 8), "idle", font=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 22), fill=(200, 190, 170))
        dr.text((bg.width // 2 + 10, 8), "active" if kind == "tab" else "pressed",
                font=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 22), fill=(200, 190, 170))
        save_jpg(bg, OUT / f"keys-{kind}.jpg")


BACKINGS = {"A": "tissue", "B": "tendrils", "C": "lattice"}   # survey W8: what fills the space between the panels
COMPARE = ROOT / ".tools" / "compare"


SSD_RING_MM = 105.0   # the ship display drawing's outer reach from its centre: shield ring radius 100 + half its 9 px line
BLOCKOUT = ROOT / "assets" / "ui" / "vhorr" / "organic" / "blockout.json"


def ssd_to_glass(text):
    """The page copy's ship display drawn centred in, and scaled to, the glass the blockout gives it (owner, 2026-11-01:
    "this ship display should span the whole width"), as it would be in play, not at the page's own pocket size."""
    slots = json.loads(BLOCKOUT.read_text())["layouts"]["desktop"]["slots"]
    sl = next(s for s in slots if s["piece"] == "ssd.bezel")
    gx, gy, gw, gh = next(q["rect"] for q in sl["protected"] if q["kind"] == "live-drawing")
    cx, cy = gx - sl["rect"][0] + gw / 2, gy - sl["rect"][1] + gh / 2
    k = (min(gw, gh) / 2 - 4.0) / SSD_RING_MM
    old = "function ssdLive(x, S) { S.live.ssd(x, 230, 150,"
    if old not in text:
        raise SystemExit("backing: the page's ship display drawing line has changed")
    return text.replace(old, f"function ssdLive(x, S) {{ x.translate({cx}, {cy}); x.scale({k:.4f}, {k:.4f}); "
                             "x.translate(-230, -150); S.live.ssd(x, 230, 150,")


def backing_pictures(console_dir, backing_dir, page, node_path, only="", name="brood-w-bg"):
    """W8's options: the whole console, with the owner's W1-W7 picks (console_dir, rendered from
    assets/ui/vhorr/organic/styles.json), over the battle filling the whole screen, with each backing
    (backing_dir/backing.<mode>.png from `build_ui_vhorr_organic.py -- --backing <modes>`) under the panels. Writes the
    survey picture (the column's lower half and the battle beside it, where the gutters are widest) as
    docs/design/survey/backing-<letter>.jpg, and the whole console at desktop and phone size, and a close-up, into
    .tools/compare/<name>-<letter>*.png for looking at before they are shown. With a name other than the default, only
    those compare shots are written (a look at a change, the survey's pictures untouched)."""
    COMPARE.mkdir(parents=True, exist_ok=True)
    for letter, mode in BACKINGS.items():
        if only and letter not in only:
            continue
        png = backing_dir / f"backing.{mode}.png"
        meta = json.loads(png.with_suffix(".json").read_text())
        work = backing_dir / "shots" / mode
        work.mkdir(parents=True, exist_ok=True)
        pg = work / "page.html"
        style_page(page, console_dir, pg)
        x, y, w, h = meta["image_rect"]
        inj = "<script>window.BROOD_BACKING = " + json.dumps({"src": webp_uri(png), "rect": [x, y, w, h]}) + ";</script>"
        text = pg.read_text()
        i = text.index("<script")   # before the page's own scripts, which draw the battle at the console's size with it
        text = text[:i] + inj + text[i:]
        size = "vc.width = 1448; vc.height = 960;"
        if size not in text:
            raise SystemExit("backing: the page's battle view size line has changed")
        text = text.replace(size, "vc.width = window.BROOD_BACKING ? 1920 : 1448; vc.height = window.BROOD_BACKING ? 1080 : 960;")
        ro = "x.fillText(cs(s), 40, h - 150 + i * 32)"   # the target readout kept where it was, beside the column
        if ro not in text:
            raise SystemExit("backing: the page's target readout line has changed")
        pg.write_text(text.replace(ro, "x.fillText(cs(s), window.BROOD_BACKING ? 512 : 40, h - (window.BROOD_BACKING ? 194 : 150) + i * 32)"))
        pg.write_text(ssd_to_glass(pg.read_text()))
        r = subprocess.run(["node", str(ROOT / "tools" / "mockups" / "survey_option_shots.mjs"), str(pg), str(work), "console"],
                           capture_output=True, text=True, env={**os.environ, "NODE_PATH": node_path})
        if r.returncode:
            raise SystemExit(f"backing {letter}: {r.stderr[-2000:]}")
        info = json.loads(r.stdout.strip().splitlines()[-1])
        if info["errors"]:
            raise SystemExit(f"backing {letter}: page errors {info['errors']}")
        full = Image.open(work / "console.png")
        k = full.width / 1920.0
        full.save(COMPARE / f"{name}-{letter.lower()}.png")
        Image.open(work / "phone.png").save(COMPARE / f"{name}-{letter.lower()}-phone.png")
        full.crop((0, round(560 * k), round(780 * k), round(1080 * k))).save(COMPARE / f"{name}-{letter.lower()}-zoom.png")
        full.crop((0, round(300 * k), round(780 * k), round(700 * k))).save(COMPARE / f"{name}-{letter.lower()}-ssd.png")
        if name == "brood-w-bg":
            save_jpg(full.crop((0, round(520 * k), round(780 * k), round(1080 * k))), OUT / f"backing-{letter}.jpg")
        print(f"backing {letter} ({mode}): pictures written")


def inline():
    """Write every picture in docs/design/survey/ into survey.html between its IMGS markers, as data URIs."""
    imgs = {}
    for p in sorted(OUT.glob("*.jpg")):
        imgs[p.stem] = "data:image/jpeg;base64," + base64.b64encode(p.read_bytes()).decode("ascii")
    text = SURVEY.read_text()
    a, b = "// IMGS-BEGIN\n", "// IMGS-END\n"
    i, j = text.index(a) + len(a), text.index(b)
    body = "const PICS = " + json.dumps(imgs, separators=(",", ":")) + ";\n"
    SURVEY.write_text(text[:i] + body + text[j:])
    return len(imgs), SURVEY.stat().st_size


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--renders", default="/tmp/sv")
    ap.add_argument("--page", default="/tmp/vb_page_with_bleed.html")
    ap.add_argument("--keys", default="/tmp/sv/keys.png")
    ap.add_argument("--only", default="", help="letters, e.g. AC")
    ap.add_argument("--inline-only", action="store_true")
    ap.add_argument("--shots-name", default="brood-w-bg", help="W8: the compare shots' name; another name writes only those")
    ap.add_argument("--backings", default="", help="W8 only: the console renders with the picks (dir), then the backings "
                    "dir, comma separated, e.g. /tmp/bw/console,/tmp/bw/backing")
    a = ap.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    if a.backings:
        cd, bd = (pathlib.Path(x) for x in a.backings.split(","))
        node_path = subprocess.run(["npm", "root", "-g"], capture_output=True, text=True).stdout.strip()
        backing_pictures(cd, bd, pathlib.Path(a.page), node_path, a.only, a.shots_name)
    elif not a.inline_only:
        renders, page = pathlib.Path(a.renders), pathlib.Path(a.page)
        node_path = subprocess.run(["npm", "root", "-g"], capture_output=True, text=True).stdout.strip()
        for letter, style in STYLES.items():
            if a.only and letter not in a.only:
                continue
            rd = renders / style
            if not (rd / "manifest.json").exists():
                print(f"skip {letter}: no renders in {rd}")
                continue
            work = renders / "shots" / style
            work.mkdir(parents=True, exist_ok=True)
            pg = work / "page.html"
            style_page(page, rd, pg)
            r = subprocess.run(["node", str(ROOT / "tools" / "mockups" / "survey_option_shots.mjs"), str(pg), str(work)],
                               capture_output=True, text=True, env={**os.environ, "NODE_PATH": node_path})
            if r.returncode:
                raise SystemExit(f"{letter}: {r.stderr[-2000:]}")
            info = json.loads(r.stdout.strip().splitlines()[-1])
            if info["errors"]:
                raise SystemExit(f"{letter}: page errors {info['errors']}")
            for comp in COMPONENTS:
                save_jpg(Image.open(work / f"{comp}.png"), OUT / f"{comp}-{letter}.jpg")
            print(f"style {letter} ({style}): {len(COMPONENTS)} pictures")
        if pathlib.Path(a.keys).exists():
            keys_pictures(a.keys, pathlib.Path(a.keys).with_suffix(".json"))
            print("keys: 3 pictures")
    n, size = inline()
    print(f"inlined {n} pictures; survey.html {size / 1e6:.2f} MB")


if __name__ == "__main__":
    main()
