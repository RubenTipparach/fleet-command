// Fleet Command mockups: the campaign's missions as scripts (openspec/changes/campaign-missions; owner, 2026-11-05: "you
// can fly around the galaxy map performing missions. Mockup campaign systems first. Then I'll get you some mission scripts
// to implement in our three js mockup env"). One registry, one shape for every mission: the contracts offered on the map
// and the story missions alike are scripts of the shape below, so a new mission is one define() call and no change to the
// page. The page (docs/mockups/game-flow.html) owns the campaign; a script only reads it through `ctx` and answers.
//
// FCMissions.define(script) adds a script. A script is:
//   id, name, glyph (one letter on the map marker), kind ('contract' expires after `turns`; 'story' never expires)
//   reward            base credits
//   turns             turns a contract stays on offer (default 3)
//   where(ctx)        the sectors it may be offered in: a list of hexes from ctx.hexes()
//   brief(ctx, m)     the briefing text; objective(ctx, m) the one-line goal
//   needs(ctx, m)     null, or why the fleet cannot take it yet ("Boarding needs marines aboard.")
//   battle(ctx, m)    null for a mission done on arrival (a survey, a delivery), or
//                     { enemy: faction id, count: ships, name? } for the battle it starts
//   result(ctx, m, win)  what happens after: { credits, standing: { faction: change }, prize: hull id, next: [script id,
//                     or { id, c, r }], log: 'text' }; any field may be left out
// m is the offered mission: { id, script, c, r, turns, strength, reward, data } (data: whatever where/brief stored).
//
// ctx, given by the page: side, rival (faction ids), name(faction), hexes(), hex(c, r), at (the fleet's sector),
// home (the side's home port), own(h), hostile(h), plain(h), port(h), groupAt(h), fleet, marines(), rng(), near(h, n)
// (sectors within n of h), dist(a, b).
window.FCMissions = (function () {
'use strict';
const ALL = {};
function define(s) {
  for (const k of ['id', 'name', 'glyph', 'where', 'brief', 'objective'])
    if (s[k] == null) throw new Error(`mission script ${s.id || '?'} has no ${k}`);
  ALL[s.id] = Object.assign({ kind: 'contract', reward: 0, turns: 3, needs: () => null, battle: () => null, result: () => ({}) }, s);
  return ALL[s.id];
}
const strengthCount = (m) => ({ Light: 1, Moderate: 2, Heavy: 3 })[m.strength] || 1;
const foeOf = (ctx, h) => (ctx.hostile(h) ? h.owner : ctx.rival || 'vos');

// ---- Contracts: the seven kinds of the game flow's first draft (mockup-work.md, mockup 1), now as scripts ---------------
define({ id: 'patrol', name: 'Patrol', glyph: 'P', reward: 300,
  where: (ctx) => ctx.hexes().filter((h) => ctx.plain(h) && (ctx.own(h) || h.owner === 'neutral')),
  objective: () => 'Sweep the sector and engage what you find.',
  brief: (ctx) => `Raiders have been seen on the trade lanes. Take a turn through the sector and see them off. Your ${ctx.name(ctx.side)} patrol pay is the reward.`,
  battle: (ctx, m) => ({ enemy: 'vos', count: strengthCount(m) }),
  result: (ctx, m, win) => (win ? { credits: m.reward, standing: { [ctx.side]: 2 } } : {}) });
define({ id: 'station', name: 'Enemy station assault', glyph: 'S', reward: 1200,
  where: (ctx) => ctx.hexes().filter((h) => h.station && ctx.hostile(h)),
  objective: () => 'Destroy the station.',
  brief: (ctx, m) => `${ctx.hex(m.c, m.r).station} anchors the ${ctx.name(ctx.hex(m.c, m.r).owner)} line here. Break its shields and destroy it.`,
  battle: (ctx, m) => ({ enemy: ctx.hex(m.c, m.r).owner, count: strengthCount(m) + 1 }),
  result: (ctx, m, win) => (win ? { credits: m.reward, standing: { [ctx.hex(m.c, m.r).owner]: -10, [ctx.side]: 5 } } : {}) });
define({ id: 'planet', name: 'Planetary assault', glyph: 'A', reward: 1000,
  where: (ctx) => ctx.hexes().filter((h) => h.planet && ctx.hostile(h)),
  objective: () => 'Destroy the orbital defences.',
  brief: (ctx, m) => `The orbital guns over ${ctx.hex(m.c, m.r).planet} cover the approach. Silence them.`,
  battle: (ctx, m) => ({ enemy: ctx.hex(m.c, m.r).owner, count: strengthCount(m) }),
  result: (ctx, m, win) => (win ? { credits: m.reward, standing: { [ctx.hex(m.c, m.r).owner]: -8, [ctx.side]: 4 } } : {}) });
define({ id: 'ambush', name: 'Convoy ambush', glyph: 'C', reward: 700,
  where: (ctx) => ctx.hexes().filter((h) => ctx.plain(h) && ctx.hostile(h)),
  objective: () => 'Destroy two of the three freighters.',
  brief: (ctx, m) => `A ${ctx.name(foeOf(ctx, ctx.hex(m.c, m.r)))} supply convoy passes through here. Destroy two of its three freighters before they jump.`,
  battle: (ctx, m) => ({ enemy: foeOf(ctx, ctx.hex(m.c, m.r)), count: strengthCount(m) + 1 }),
  result: (ctx, m, win) => (win ? { credits: m.reward } : {}) });
define({ id: 'defend', name: 'Convoy defense', glyph: 'D', reward: 600,
  where: (ctx) => ctx.hexes().filter((h) => ctx.plain(h) && ctx.own(h)),
  objective: () => 'Keep two of the three freighters alive.',
  brief: (ctx) => `Our freighters are expected through this sector and raiders are waiting for them. Keep two of the three alive.`,
  battle: (ctx, m) => ({ enemy: ctx.rival || 'vos', count: strengthCount(m) }),
  result: (ctx, m, win) => (win ? { credits: m.reward, standing: { [ctx.side]: 3 } } : { standing: { [ctx.side]: -2 } }) });
define({ id: 'assassinate', name: 'Assassination', glyph: 'X', reward: 900,
  where: (ctx) => ctx.hexes().filter((h) => ctx.groupAt(h)?.commander),
  objective: () => 'Destroy the commander\'s ship.',
  brief: (ctx, m) => `${ctx.groupAt(ctx.hex(m.c, m.r))?.name || 'An enemy commander'} is in this sector. Destroy the flagship; the escorts do not matter.`,
  battle: (ctx, m) => ({ enemy: ctx.groupAt(ctx.hex(m.c, m.r))?.fac || ctx.rival || 'vos', count: 3 }),
  result: (ctx, m, win) => (win ? { credits: m.reward } : {}) });
define({ id: 'capture', name: 'Capture a ship', glyph: 'K', reward: 800,
  where: (ctx) => ctx.hexes().filter((h) => ctx.groupAt(h)?.size === 1 && ctx.groupAt(h).fac !== ctx.side),
  objective: () => 'Board and take the ship. Needs marines.',
  brief: (ctx, m) => `${ctx.groupAt(ctx.hex(m.c, m.r))?.name || 'A lone ship'} is alone here. Drop its shields and board it.`,
  needs: (ctx) => (ctx.marines() > 0 ? null : 'Boarding needs marines aboard.'),
  battle: (ctx, m) => ({ enemy: ctx.groupAt(ctx.hex(m.c, m.r))?.fac || 'vos', count: 1 }),
  result: (ctx, m, win) => (win ? { credits: m.reward, prize: ctx.groupAt(ctx.hex(m.c, m.r))?.fac || 'vos' } : {}) });

// ---- Story: a new captain's first command, an example of a chain (a mission that offers the next when it is done) -------
// The owner's own mission scripts replace this when they arrive.
define({ id: 'shakedown', name: 'Shakedown cruise', glyph: '1', kind: 'story', reward: 250,
  where: (ctx) => ctx.near(ctx.home, 3).filter((h) => ctx.plain(h) && ctx.dist(h, ctx.home) >= 2 && h.terrain === 'open'),
  objective: () => 'Fly to the marker beacon and back on your own engines.',
  brief: (ctx) => `Your first command is fresh from the yard. Take her out to the marker beacon and let the crew find her faults before anyone else does. No battle expected.`,
  battle: () => null,
  result: (ctx) => ({ credits: 250, next: ['first_blood'], log: 'Shakedown done: the yard fixed what the crew found.' }) });
define({ id: 'first_blood', name: 'First blood', glyph: '2', kind: 'story', reward: 500,
  where: (ctx) => ctx.near(ctx.home, 5).filter((h) => ctx.plain(h) && ctx.dist(h, ctx.home) >= 3),
  objective: () => 'Drive off the raider watching your home port.',
  brief: (ctx) => `A single raider has been shadowing traffic out of ${ctx.home.planet || ctx.home.station}. You are the nearest ship. Drive it off.`,
  battle: () => ({ enemy: 'vos', count: 1, name: 'First blood' }),
  result: (ctx, m, win) => (win ? { credits: 500, standing: { [ctx.side]: 5, vos: -5 }, log: 'The raider is gone; the port has noticed you.' }
    : { next: ['first_blood'], log: 'The raider got away. It will be back.' }) });

return { define, get: (id) => ALL[id], ids: () => Object.keys(ALL), contracts: () => Object.values(ALL).filter((s) => s.kind === 'contract').map((s) => s.id) };
})();
