// Export the crew skill icons for the game (change crew-leveling): each subject of docs/mockups/lib/skill-icons.js (the one
// copy of how they are drawn, the Concord's) painted by that same code in white on a clear ground at its 2x size (96 px),
// written to assets/ui/skills/<ability>.png; the game tints them (src/Game/Ui/SkillIcons.cs). Re-run when the painter
// changes, never edit a png.
//
//   NODE_PATH=$(npm root -g) node tools/mockups/export_skill_icons.mjs
import { createRequire } from 'node:module';
import path from 'node:path';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';

const { chromium } = createRequire(import.meta.url)('playwright');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const OUT = path.join(ROOT, 'assets', 'ui', 'skills');
const lib = (f) => fs.readFileSync(path.join(ROOT, 'docs', 'mockups', 'lib', f), 'utf8');
const browser = await chromium.launch({ args: ['--disable-accelerated-2d-canvas'] });
const page = await browser.newPage();
await page.setContent('<html><body></body></html>');
await page.addScriptTag({ content: lib('kitdraw.js') });
await page.addScriptTag({ content: lib('skill-icons.js') });
const icons = await page.evaluate(() => Object.keys(window.FCSkillIcons.SUBJECTS).map((k) => [k, window.FCSkillIcons.icon(k, '#ffffff').toDataURL('image/png').split(',')[1]]));
await browser.close();
fs.mkdirSync(OUT, { recursive: true });
for (const [k, b64] of icons) fs.writeFileSync(path.join(OUT, `${k}.png`), Buffer.from(b64, 'base64'));
console.log(`wrote ${icons.length} skill icons to ${path.relative(ROOT, OUT)}`);
