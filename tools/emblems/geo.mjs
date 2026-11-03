// Print one faction emblem's shape numbers (its `geo`) from docs/mockups/lib/emblems.js as JSON, so the 3D render
// (tools/blender/emblem_*.py) reads the same numbers the flat marks are drawn from (CLAUDE.md section 6.1).
// The file is evaluated with a stub window: only DEFS (plain data) is read, nothing is drawn.
//
//   node tools/emblems/geo.mjs concord > build/concord.geo.json
import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';

const id = process.argv[2];
const src = fs.readFileSync(path.join(path.dirname(new URL(import.meta.url).pathname), '../../docs/mockups/lib/emblems.js'), 'utf8');
const ctx = { window: {}, Math };
vm.runInNewContext(src, ctx);
const def = ctx.window.FCEmblems.BY_ID[id];
if (!def) { console.error(`no emblem "${id}"`); process.exit(2); }
if (!def.geo) { console.error(`emblem "${id}" has no geo yet: its shapes are still drawn only as paths`); process.exit(3); }
process.stdout.write(JSON.stringify({ id, name: def.name, geo: def.geo, mark: def.mark }, null, 1) + '\n');
