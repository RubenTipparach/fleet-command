// Render a battle from a mockup page to an MP4, frame by frame, headless: the page's battle is stepped exactly one frame's
// time at a time (window.BATTLE, docs/mockups/fractal-vessels.html), drawn, captured and piped to ffmpeg, so the video
// plays at its true speed however slowly each frame renders (a cloud session draws with the software renderer and cannot
// record live, CLAUDE.md section 9). Also writes stills at chosen times, for looking at before the video is called done.
//
//   NODE_PATH=$(npm root -g) node tools/mockups/render_video.mjs <page.html> <battle> <out.mp4> \
//     [--w 1280] [--h 720] [--fps 30] [--seconds N] [--stills 6,12,20 --still-dir dir]
//
// --seconds 0 renders stills only.
import { createRequire } from 'node:module';
import { spawn } from 'node:child_process';
import path from 'node:path';
import fs from 'node:fs';

const { chromium } = createRequire(import.meta.url)('playwright');
const args = process.argv.slice(2);
const opt = (k, d) => { const i = args.indexOf('--' + k); return i >= 0 ? args[i + 1] : d; };
const [pagePath, battle, out] = args.filter((a, i) => !a.startsWith('--') && !(i > 0 && args[i - 1].startsWith('--')));
if (!pagePath || !battle || !out) { console.error('usage: render_video.mjs <page.html> <battle> <out.mp4> [--w --h --fps --seconds --stills --still-dir]'); process.exit(2); }
const W = +opt('w', 1280), H = +opt('h', 720), FPS = +opt('fps', 30);
const stills = (opt('stills', '') || '').split(',').filter(Boolean).map(Number).sort((a, b) => a - b);
const stillDir = opt('still-dir', path.dirname(out));

const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
const page = await browser.newPage({ viewport: { width: W, height: H }, deviceScaleFactor: 1 });
let errors = 0;
page.on('pageerror', (e) => { errors++; console.log('pageerror:', e.message); });
page.on('console', (m) => { if (m.type() === 'error') { errors++; console.log('console:', m.text()); } });
await page.goto('file://' + path.resolve(pagePath), { timeout: 240000 });
await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 240000 });
const length = await page.evaluate((b) => window.BATTLE.start(b, { video: true }), battle);
const seconds = +opt('seconds', length + 2);
const frames = Math.round(seconds * FPS);

let ff = null;
if (frames > 0) {
  ff = spawn('ffmpeg', ['-y', '-loglevel', 'error', '-f', 'image2pipe', '-framerate', String(FPS), '-c:v', 'mjpeg', '-i', '-',
    '-c:v', 'libx264', '-preset', 'slow', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', out], { stdio: ['pipe', 'inherit', 'inherit'] });
}
fs.mkdirSync(stillDir, { recursive: true });
const t0 = Date.now();
let si = 0, t = 0;
const last = Math.max(frames, stills.length ? Math.ceil(stills[stills.length - 1] * FPS) + 1 : 0);
for (let f = 0; f < last; f++) {
  if (f > 0) t = await page.evaluate((dt) => window.BATTLE.frame(dt), 1 / FPS);
  const want = f < frames || (si < stills.length && t >= stills[si] - 1e-6);
  if (!want) continue;
  const jpg = await page.screenshot({ type: 'jpeg', quality: 94 });
  if (f < frames && !ff.stdin.write(jpg)) await new Promise((r) => ff.stdin.once('drain', r));
  while (si < stills.length && t >= stills[si] - 1e-6) {
    const name = path.join(stillDir, `${path.basename(out, '.mp4')}-${String(stills[si]).replace('.', '_')}s.png`);
    await page.screenshot({ path: name });
    console.log('still', name);
    si++;
  }
  if (f % (FPS * 2) === 0) console.log(`frame ${f}/${last} (${t.toFixed(1)} s of battle, ${((Date.now() - t0) / 1000).toFixed(0)} s)`);
}
if (ff) { ff.stdin.end(); await new Promise((r) => ff.on('close', r)); console.log('wrote', out); }
await browser.close();
if (errors) { console.error(errors + ' page errors'); process.exit(1); }
