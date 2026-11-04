"""Side-by-side sheets of the battle console mockup and the game (CLAUDE.md section 12: look at the shots).

The mockup's named shots (tools/mockups/shoot.mjs, desktop) on the left, the game's stills (scripts/game-stills.sh) on
the right, each pair one PNG at half size with a caption. Both play the same skirmish seed for 600 steps, but the
mockup's scripted shot flies an intercept on autopilot and the game's stills do not, so ship positions differ; compare
the console, not the battle.

Usage: python3 tools/compare/side_by_side.py <mockup shots dir> <game stills dir> <out dir>
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

# (mockup page, its named shot, the game's still). The Krai pairs are mockup 8 (the battle console in the Krai skin)
# beside the game playing the Krai's Battle scenario (CLAUDE.md section 4: the game stays in step with its mockups).
PAIRS = [
    ("battle-console", "battle", "battle"), ("battle-console", "power", "power"), ("battle-console", "shields", "shields"),
    ("battle-console", "power_expanded", "power-expanded"), ("battle-console", "ship_expanded", "ship-expanded"),
    ("battle-console", "weapons_expanded", "weapons-expanded"), ("battle-console", "menu", "menu"),
    ("battle-console-krai", "battle", "krai-battle"), ("battle-console-krai", "power", "krai-power"),
    ("battle-console-krai", "ship_expanded", "krai-ship-expanded"), ("battle-console-krai", "weapons_expanded", "krai-weapons-expanded"),
    ("battle-console-krai", "menu", "krai-menu"),
]


def main(mock_dir: Path, game_dir: Path, out_dir: Path) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    font = ImageFont.truetype(str(Path(__file__).resolve().parents[2] / "assets/fonts/Antonio-VariableFont_wght.ttf"), 30)
    for page, mock, game in PAIRS:
        ma, gb = mock_dir / f"{page}-{mock}-desktop.png", game_dir / f"{game}.png"
        if not ma.exists() or not gb.exists():
            print("skipped", game, "(missing", ma if not ma.exists() else gb, ")")
            continue
        a = Image.open(ma).convert("RGB").resize((960, 540), Image.LANCZOS)
        b = Image.open(gb).convert("RGB").resize((960, 540), Image.LANCZOS)
        sheet = Image.new("RGB", (1940, 600), (12, 14, 24))
        sheet.paste(a, (0, 60))
        sheet.paste(b, (980, 60))
        d = ImageDraw.Draw(sheet)
        d.text((10, 12), f"MOCKUP (three.js): {page} {mock}", font=font, fill=(183, 166, 242))
        d.text((990, 12), f"GAME (Godot 4.7 C#): {game}", font=font, fill=(63, 169, 240))
        sheet.save(out_dir / f"{game}.png")
        print("sheet", out_dir / f"{game}.png")


if __name__ == "__main__":
    main(Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3]))
