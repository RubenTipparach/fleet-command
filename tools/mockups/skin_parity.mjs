// Prove a faction's battle console does everything the Concord's does (owner, 2026-11-03: "verify all functionality
// from concord works here"; openspec/changes/krai-battle-console). Headless, for each named shot of the page
// (window.MOCKUP_SHOTS, the scripted battle and test scenes): replay the shot, then press every control the Concord
// console registered in it, each from a fresh replay of the shot, a second of the battle after it, and record what it did: the
// console's state (station, tab, expanded panel, modes, toggles) and the battle's (every ship's position, heading,
// speed, hull, shields, power, systems, weapons, orders, the boarding queues, mines, craft, the log's length). The
// faction's page must give the same record, press for press; a control missing, a press doing nothing, or a different
// outcome is a failure, named with its shot and control. The Concord is first checked against itself, so a
// difference cannot come from the check.
//
//   NODE_PATH=$(npm root -g) node tools/mockups/skin_parity.mjs docs/mockups/battle-console-krai.html [shot,names]
import { createRequire } from 'node:module';
import path from 'node:path';

const { chromium } = createRequire(import.meta.url)('playwright');
const [target, only] = process.argv.slice(2);
if (!target) { console.error('usage: node tools/mockups/skin_parity.mjs <faction page.html> [shot,names]'); process.exit(2); }
const SOURCE = 'docs/mockups/battle-console.html';
const STEPS_AFTER_PRESS = 30; // one second at the page's 30 Hz step

const proxy = process.env.HTTPS_PROXY ? { server: process.env.HTTPS_PROXY, bypass: 'localhost,127.0.0.1' } : undefined;
// The 2D canvas stays on the CPU: on the software GPU every toDataURL of a painted piece reads back from swiftshader, and
// a page of painted skins took over four minutes to load (2026-11-07); WebGL still runs on swiftshader.
const browser = await chromium.launch({ proxy, args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--disable-accelerated-2d-canvas'] });

// One page's record: { shot: [{ id, present, pressed, state }] }, the controls taken from `plan` (the Concord's own
// list per shot) when given, else from what the page itself registered.
async function record(file, plan) {
  const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });
  let errors = [];
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('file://' + path.resolve(file), { timeout: 240000 });
  await page.waitForFunction(() => window.MOCKUP_READY, null, { timeout: 240000 });
  const skin = await page.evaluate(() => window.MOCKUP_SKIN);
  const shots = (await page.evaluate(() => Object.keys(window.MOCKUP_SHOTS))).filter((n) => !only || only.split(',').includes(n));
  const out = {};
  for (const shot of shots) {
    out[shot] = await page.evaluate(([shot, ids, steps]) => {
      const S = window.MOCKUP_SIM;
      const r = (v) => (typeof v === 'number' ? Math.round(v * 1e4) / 1e4 : v);
      const deep = (o) => JSON.parse(JSON.stringify(o, (k, v) => (typeof v === 'number' ? r(v) : v)));
      // everything a press could change, names left out (a skin renames the player's ships)
      const state = () => {
        const W = S.W;
        return deep({ ui: window.MOCKUP_UI(), t: W.t, over: W.over, obj: W.objDone, log: W.log.length, warns: W.log.filter((m) => m.warn).length,
          player: W.player.id, mines: W.mines.length, craft: W.craft.length, shots: W.shots.length,
          ships: W.ships.map((o) => ({ id: o.id, side: o.side, alive: o.alive, x: o.x, z: o.z, h: o.h, v: o.v, hull: o.hull, sh: o.shields, shUp: o.shUp, re: o.reinforce,
            pow: o.power, sys: o.sys, cap: o.cap, bat: o.battery, tgt: o.target, ord: o.order, eng: o.engaged, vO: o.vOrder, hO: o.hOrder, alert: o.alert, err: o.erratic,
            m: (o.mounts || []).map((m) => [m.charge, m.state, m.hp]), bq: o.bq, pads: o.pads, hold: o.hold, warp: o.warp && { a: o.warp.armed, b: o.warp.banks, p: o.warp.plot } })) });
      };
      window.MOCKUP_SHOTS[shot]();
      window.MOCKUP_DRAW();
      const list = ids || [...new Set(window.MOCKUP_HITS().map((h) => h.id))].sort();
      const rows = [{ id: '(shot)', present: true, pressed: false, state: state() }];
      for (const id of list) {
        // each control from the shot's own state, so one press never hides the next control
        window.MOCKUP_SHOTS[shot]();
        window.MOCKUP_DRAW();
        const present = window.MOCKUP_HITS().some((h) => h.id === id);
        let pressed = false, threw = null;
        try { pressed = present ? window.MOCKUP_PRESS(id) : false; } catch (e) { threw = String(e && e.message || e); }
        if (threw) { rows.push({ id, present, pressed: true, threw, state: null }); continue; }
        for (let i = 0; i < steps; i++) S.step();
        window.MOCKUP_DRAW();
        rows.push({ id, present, pressed, state: state() });
      }
      return rows;
    }, [shot, plan ? plan[shot].slice(1).map((q) => q.id) : null, STEPS_AFTER_PRESS]);
  }
  await page.close();
  return { skin, out, errors };
}

function compare(a, b, label) {
  let fails = 0, presses = 0, inert = 0;
  const ids = new Set();
  for (const shot of Object.keys(a.out)) {
    const A = a.out[shot], B = b.out[shot];
    if (!B) { console.log(`FAIL ${label} ${shot}: no such shot`); fails++; continue; }
    for (let i = 0; i < A.length; i++) {
      const p = A[i], q = B[i];
      if (p.threw) { fails++; console.log(`FAIL ${label} ${shot} press ${i}: ${p.id}: threw ${p.threw} (in the Concord itself)`); continue; }
      if (p.pressed) { presses++; ids.add(p.id); if (JSON.stringify(p.state.ui) === JSON.stringify(A[0].state.ui) && JSON.stringify(p.state.ships) === JSON.stringify(A[0].state.ships)) inert++; }
      const why = !q ? 'missing row' : q.threw ? `threw ${q.threw}` : p.present !== q.present ? `control ${q.present ? 'extra' : 'missing'}` : p.pressed !== q.pressed ? 'press did nothing' :
        JSON.stringify(p.state) !== JSON.stringify(q.state) ? 'different outcome' : null;
      if (why) {
        fails++;
        let detail = '';
        if (why === 'different outcome') { const sa = JSON.stringify(p.state.ui), sb = JSON.stringify(q.state.ui); detail = sa !== sb ? ` (console ${sa} vs ${sb})` : ' (battle state)'; }
        console.log(`FAIL ${label} ${shot} press ${i}: ${p.id}: ${why}${detail}`);
      }
    }
  }
  return { fails, presses, ids: ids.size, inert };
}

const concord = await record(SOURCE);
const again = await record(SOURCE, concord.out);
const self = compare(concord, again, 'concord-vs-concord');
console.log(`concord against itself: ${self.fails ? self.fails + ' difference(s)' : 'identical'} (${self.presses} presses of ${self.ids} controls)`);
const faction = await record(target, concord.out);
const res = compare(concord, faction, `concord-vs-${faction.skin}`);
for (const [n, r] of [['concord', concord], [faction.skin, faction]]) if (r.errors.length) { console.log(`${n}: ${r.errors.length} console error(s): ${r.errors.slice(0, 3).join(' | ')}`); res.fails++; }
console.log(`${faction.skin}: ${Object.keys(concord.out).length} shots, ${res.presses} presses of ${res.ids} distinct controls (${res.inert} changed nothing a second later in either skin, as in the Concord), ${res.fails ? res.fails + ' FAILURE(S)' : 'every press matched the Concord'}`);
await browser.close();
process.exit(res.fails || self.fails ? 1 : 0);
