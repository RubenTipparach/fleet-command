// Fleet Command mockups: the campaign's missions as scripts (openspec/changes/campaign-missions; owner, 2026-11-05: "you
// can fly around the galaxy map performing missions. Mockup campaign systems first. Then I'll get you some mission scripts
// to implement in our three js mockup env"). One registry, one shape for every mission: the contracts offered on the map
// and the story missions alike are scripts of the shape below, so a new mission is one define() call and no change to the
// page. The page (docs/mockups/game-flow.html) owns the campaign; a script only reads it through `ctx` and answers.
// The mission types and grades are the Fleet Command Missions guide's (docs/design/mission-guide.html); every type it
// marks ready is a script here (owner, 2026-11-05: "let's implement all \"ready\" mission types"), with Capture the prize
// added ("add \"capture the prize\" mission to capture a designated target ship for research").
//
// FCMissions.define(script) adds a script. A script is:
//   id, name, glyph (one letter on the map marker), kind ('contract' expires after `turns`; 'story' never expires),
//   type (the guide's mission type it plays)
//   reward            base credits
//   turns             turns a contract stays on offer (default 3)
//   where(ctx)        the sectors it may be offered in: a list of hexes from ctx.hexes()
//   setup(ctx, m)     optional: fills m.data when the mission is offered (a target's name, whether a call is a trap)
//   brief(ctx, m)     the briefing text; objective(ctx, m) the one-line goal
//   needs(ctx, m)     null, or why the fleet cannot take it yet ("Boarding needs marines aboard.")
//   battle(ctx, m)    null for a mission done on arrival (a survey, a delivery), or the battle it starts:
//                     { name?, groups: [group], objectives: [objective], intel? }
//                       group: { fac, count, role: 'enemy' | 'ally' | 'neutral', label?, tag?: 'flagship' | 'prize' |
//                              'rescue' | 'decoy' }; allies are wingmen beside the fleet, neutrals are ships to protect
//                       objective: { id, text, primary?, byWithdraw? } (primary ones decide win or lose; the rest lift
//                              the grade; byWithdraw: withdrawing meets it, as breaking out of an ambush does)
//                       intel: what the briefing says of the opposition, when it is not the truth (an ambush, a trap)
//                     The older short form { enemy, count, name? } is one enemy group with the script's objective.
//   grade(ctx, m, met, how)  optional: the outcome grade, when the default rule below does not fit the type
//   result(ctx, m, o) what happens after, o = { grade, win, met, how, pay } (pay: the grade's share of the reward):
//                     { credits, prestige, standing: { faction: change }, prize: faction, next: [script id, or { id, c, r, data }],
//                     log: 'text' }; any field may be left out
// m is the offered mission: { id, script, c, r, turns, strength, reward, data }.
//
// ctx, given by the page: side, rival (faction ids), name(faction), hexes(), hex(c, r), at (the fleet's sector),
// home (the side's home port), own(h), hostile(h), plain(h), port(h), groupAt(h), fleet, marines(), rng(), near(h, n)
// (sectors within n of h), dist(a, b), hull(faction, tier) ({ id, cls }), names(faction) (its ship names), routes(faction)
// (the sectors its freighters run through). The factions' fleets move; a script that stores m.data.group (a fleet's name)
// has its mission move with that fleet.
window.FCMissions = (function () {
'use strict';
const ALL = {};
function define(s) {
  for (const k of ['id', 'name', 'glyph', 'where', 'brief', 'objective'])
    if (s[k] == null) throw new Error(`mission script ${s.id || '?'} has no ${k}`);
  ALL[s.id] = Object.assign({ kind: 'contract', type: null, reward: 0, turns: 3, setup: () => {}, needs: () => null, battle: () => null, result: () => ({}) }, s);
  return ALL[s.id];
}

// ---- Outcome grades (the guide's five, from worst to best; the draw is our own addition) -------------------------------
const GRADES = ['crushing', 'defeat', 'draw', 'victory', 'outstanding'];
const GRADE_NAME = { crushing: 'Crushing defeat', defeat: 'Defeat', draw: 'Draw', victory: 'Victory', outstanding: 'Outstanding' };
// The share of a mission's reward each grade pays.
const PAY = { crushing: 0, defeat: 0, draw: 0.25, victory: 1, outstanding: 1.5 };
// Prestige each grade earns, a captain's record kept apart from credits (the guide's question 1; owner, 2026-11-05: "need a
// win or loose screen, and how many prestige + reputation I got"); a story mission's counts double. A script's result may
// give its own `prestige`.
const PRESTIGE = { crushing: -5, defeat: 0, draw: 2, victory: 5, outstanding: 10 };
const prestigeOf = (m, o) => PRESTIGE[o.grade] * (ALL[m.script].kind === 'story' ? 2 : 1);
// How a battle ended: 'end' (fought to its end), 'withdraw' (the fleet disengaged), 'lost' (the fleet was destroyed or
// struck). Every primary objective met is a victory, and every bonus too is outstanding; a withdrawal short of the primary
// is a draw, staying and failing it a defeat, and losing the fleet a crushing defeat.
function defaultGrade(objs, met, how) {
  if (how === 'lost') return 'crushing';
  const ok = (o) => !!met[o.id] || (how === 'withdraw' && !!o.byWithdraw);
  const prim = objs.filter((o) => o.primary), bonus = objs.filter((o) => !o.primary);
  if (prim.every(ok)) return bonus.length && bonus.every(ok) ? 'outstanding' : 'victory';
  return how === 'withdraw' ? 'draw' : 'defeat';
}
// A script's battle in its full form (or null), the short form expanded.
function battleOf(ctx, m) {
  const def = ALL[m.script], b = def.battle(ctx, m);
  if (!b) return null;
  if (b.groups) return { name: b.name || def.name, intel: b.intel || null, groups: b.groups, objectives: b.objectives || [{ id: 'win', text: def.objective(ctx, m), primary: true }] };
  return { name: b.name || def.name, intel: null, groups: [{ fac: b.enemy, count: b.count, role: 'enemy' }],
    objectives: [{ id: 'win', text: def.objective(ctx, m), primary: true }, { id: 'intact', text: 'Lose no ships' }] };
}
// The outcome of a mission: its grade by the script's rule or the default, what that grade pays, whether it is a win.
function score(ctx, m, battle, met, how) {
  const def = ALL[m.script];
  const g = !battle ? 'victory' : def.grade ? def.grade(ctx, m, met, how) : defaultGrade(battle.objectives, met, how);
  const done = { ...met };
  if (battle && how === 'withdraw') for (const o of battle.objectives) if (o.byWithdraw) done[o.id] = true;
  return { grade: g, win: GRADES.indexOf(g) >= GRADES.indexOf('victory'), met: done, how: battle ? how : 'done', pay: PAY[g] };
}
const paid = (m, o) => Math.round(m.reward * o.pay / 10) * 10;  // to the nearest 10 cr

const strengthCount = (m) => ({ Light: 1, Moderate: 2, Heavy: 3 })[m.strength] || 1;
const foeOf = (ctx, h) => (ctx.hostile(h) ? h.owner : ctx.rival || 'vos');
const enemyOf = (ctx) => ctx.rival || 'vos';
const intact = { id: 'intact', text: 'Lose no ships' };

// ---- Fight -------------------------------------------------------------------------------------------------------------
// Patrol (ready): sweep a sector and meet what is there. Disengaging is a draw.
define({ id: 'patrol', name: 'Patrol', glyph: 'P', type: 'Patrol', reward: 300,
  where: (ctx) => ctx.hexes().filter((h) => ctx.plain(h) && (ctx.own(h) || h.owner === 'neutral')),
  objective: () => 'Sweep the sector and engage what you find.',
  brief: (ctx, m) => `Raiders have been seen on the trade lanes. Take a turn through the sector and see them off.${ctx.hex(m.c, m.r).terrain === 'asteroids' ? ' They hide among the rocks here: mind the fields.' : ''} Your ${ctx.name(ctx.side)} patrol pay is the reward.`,
  battle: (ctx, m) => ({ groups: [{ fac: 'vos', count: strengthCount(m), role: 'enemy', label: 'Raiders' }],
    objectives: [{ id: 'clear', text: 'Destroy or drive off the raiders', primary: true }, intact] }),
  result: (ctx, m, o) => (o.win ? { credits: paid(m, o), standing: { [ctx.side]: o.grade === 'outstanding' ? 3 : 2 } }
    : o.grade === 'draw' ? { credits: paid(m, o), log: 'Patrol: outnumbered, the fleet drew off.' } : {}) });

// Fleet action (ready): an equal battle group, fought with wingmen.
define({ id: 'fleet_action', name: 'Fleet action', glyph: 'F', type: 'Fleet action', reward: 900,
  where: (ctx) => ctx.hexes().filter((h) => ctx.plain(h) && h.owner === enemyOf(ctx) && ctx.dist(h, ctx.home) >= 5),
  setup: (ctx, m) => { m.data.flag = ctx.names(enemyOf(ctx))[2]; },
  objective: (ctx, m) => `Destroy the ${m.data.flag} or most of her group.`,
  brief: (ctx, m) => `A ${ctx.name(enemyOf(ctx))} battle group under the ${m.data.flag} holds this sector. Two wingmen of the line join you for the action: meet the group as equals and break it.`,
  battle: (ctx, m) => {
    const n = ctx.fleet.length + 2, foe = enemyOf(ctx);
    return { groups: [{ fac: ctx.side, count: 2, role: 'ally', label: 'Wingmen' }, { fac: foe, count: 1, role: 'enemy', tag: 'flagship', label: m.data.flag },
      { fac: foe, count: n - 1, role: 'enemy' }],
      objectives: [{ id: 'flag', text: `Destroy the ${m.data.flag} or most of her group`, primary: true }, intact, { id: 'wing', text: 'Bring both wingmen home' }] };
  },
  result: (ctx, m, o) => (o.win ? { credits: paid(m, o), standing: { [ctx.side]: 5, [enemyOf(ctx)]: -5 } }
    : o.grade === 'draw' ? { credits: paid(m, o) } : { standing: { [ctx.side]: -2 } }) });

// Ambushed (ready): a quiet run that is not quiet. The briefing does not know; two groups are waiting, ahead and behind.
// Breaking out is the win.
define({ id: 'ambushed', name: 'Dispatch run', glyph: 'R', type: 'Ambushed', reward: 450,
  where: (ctx) => ctx.hexes().filter((h) => ctx.plain(h) && !ctx.own(h) && ctx.dist(h, ctx.home) >= 4),
  objective: () => 'Carry the dispatches through the sector.',
  brief: () => 'Sealed dispatches for the picket beyond this sector. A routine run: through and out the far side.',
  battle: (ctx, m) => {
    const foe = foeOf(ctx, ctx.hex(m.c, m.r));
    return { intel: 'None expected', groups: [{ fac: foe, count: 2, role: 'enemy', label: 'Ahead' }, { fac: foe, count: strengthCount(m), role: 'enemy', label: 'Behind' }],
      objectives: [{ id: 'out', text: 'Break out, or beat both groups', primary: true, byWithdraw: true }, { id: 'home', text: 'Bring every ship home' },
        { id: 'both', text: 'Destroy both groups' }] };
  },
  result: (ctx, m, o) => ({ credits: paid(m, o), standing: o.win ? { [ctx.side]: 2 } : {},
    log: o.win ? 'Dispatch run: it was an ambush, and the fleet broke out.' : 'Dispatch run: ambushed, and the dispatches were lost.' }) });

// Overwhelming fleet (ready): far more than one captain can beat. Each group neutralised counts; four is outstanding.
const WAVE = ['first', 'second', 'third', 'fourth'];
define({ id: 'overwhelming', name: 'Stem the tide', glyph: 'O', type: 'Overwhelming fleet', reward: 1600,
  where: (ctx) => ctx.hexes().filter((h) => ctx.plain(h) && ctx.own(h) && ctx.dist(h, ctx.home) >= 3),
  objective: () => 'Neutralise as many of its four groups as you can.',
  brief: (ctx) => `A ${ctx.name(enemyOf(ctx))} fleet in four groups is pushing into our space, far more than one captain can beat. Two wingmen are with you. Destroy, capture or drive off each group you can: every group stopped here is one that does not reach the yards.`,
  battle: (ctx) => ({ groups: [{ fac: ctx.side, count: 2, role: 'ally', label: 'Wingmen' },
    ...WAVE.map((w, i) => ({ fac: enemyOf(ctx), count: 2, role: 'enemy', label: `Group ${i + 1}` }))],
  objectives: WAVE.map((w, i) => ({ id: `g${i + 1}`, text: `Neutralise the ${w} group` })) }),
  // Groups neutralised: all four outstanding, two or three a victory, one a draw, none a defeat.
  grade: (ctx, m, met, how) => {
    if (how === 'lost') return 'crushing';
    const n = WAVE.filter((w, i) => met[`g${i + 1}`]).length;
    return n === 4 ? 'outstanding' : n >= 2 ? 'victory' : n === 1 || how === 'withdraw' ? 'draw' : 'defeat';
  },
  result: (ctx, m, o) => {
    const n = WAVE.filter((w, i) => o.met[`g${i + 1}`]).length;
    return { credits: Math.round(m.reward * n / 4 * (n === 4 ? 1.5 : 1)), standing: { [ctx.side]: 2 * n },
      log: `Stem the tide: ${n} of 4 groups stopped.` };
  } });

// ---- Raid --------------------------------------------------------------------------------------------------------------
// Capture (ready): a lone ship on the map, boarded and taken.
define({ id: 'capture', name: 'Capture a ship', glyph: 'K', type: 'Capture', reward: 800,
  where: (ctx) => ctx.hexes().filter((h) => ctx.groupAt(h)?.size === 1 && ctx.groupAt(h).fac !== ctx.side),
  setup: (ctx, m) => { m.data.group = ctx.groupAt(ctx.hex(m.c, m.r)).name; },   // the mission goes where the ship goes
  objective: () => 'Board and take the ship. Needs marines.',
  brief: (ctx, m) => `${ctx.groupAt(ctx.hex(m.c, m.r))?.name || 'A lone ship'} is alone here. Drop its shields and board it.`,
  needs: (ctx) => (ctx.marines() > 0 ? null : 'Boarding needs marines aboard.'),
  battle: (ctx, m) => {
    const g = ctx.groupAt(ctx.hex(m.c, m.r));
    return { groups: [{ fac: g?.fac || 'vos', count: 1, role: 'enemy', tag: 'prize', label: g?.name || 'Lone ship' }],
      objectives: [{ id: 'take', text: 'Board and take the ship', primary: true }, { id: 'whole', text: 'Take it with its hull above half' }, { id: 'marines', text: 'Lose no marines' }] };
  },
  result: (ctx, m, o) => (o.win ? { credits: paid(m, o), prize: ctx.groupAt(ctx.hex(m.c, m.r))?.fac || 'vos' } : {}) });

// Capture the prize (owner, 2026-11-05): research wants one named ship of the rival's, taken whole. It runs escorted;
// destroying it voids the contract. Taken, it is towed home to the research yard, which pays the bounty.
define({ id: 'prize', name: 'Capture the prize', glyph: 'Z', type: 'Capture the prize', reward: 400,
  turns: 4,
  where: (ctx) => ctx.hexes().filter((h) => ctx.plain(h) && ctx.hostile(h) && ctx.dist(h, ctx.home) >= 4),
  setup: (ctx, m) => {
    const fac = enemyOf(ctx), hull = ctx.hull(fac, 2 + Math.floor(ctx.rng() * 2)), names = ctx.names(fac);
    m.data = { fac, hull: hull.id, cls: hull.cls, name: names[(2 + Math.floor(ctx.rng() * names.length)) % names.length] };
  },
  objective: (ctx, m) => `Board and take the ${m.data.name}, whole. Needs marines.`,
  brief: (ctx, m) => `Research wants a ${ctx.name(m.data.fac)} ${m.data.cls} to take apart, and the ${m.data.name} is passing through this sector under escort. Strip her shields, board her and tow her home to ${ctx.home.planet || ctx.home.station}. She is worth nothing to us destroyed: sink her and the contract is void.`,
  needs: (ctx) => (ctx.marines() > 0 ? null : 'Boarding needs marines aboard.'),
  battle: (ctx, m) => ({ groups: [{ fac: m.data.fac, count: 1, role: 'enemy', tag: 'prize', label: `Prize: ${m.data.name}` }, { fac: m.data.fac, count: strengthCount(m), role: 'enemy', label: 'Escorts' }],
    objectives: [{ id: 'take', text: `Board and take the ${m.data.name} (not destroy her)`, primary: true }, { id: 'whole', text: 'Take her with her hull above half' },
      { id: 'escort', text: 'Destroy or drive off the escorts' }] }),
  result: (ctx, m, o) => (o.win ? { credits: paid(m, o), next: [{ id: 'prize_home', c: ctx.home.c, r: ctx.home.r, data: { ...m.data, grade: o.grade } }],
    log: `The ${m.data.name} is under tow. Bring her home to the research yard.` }
    : { log: `The ${m.data.name} got away${o.grade === 'crushing' ? ', and the fleet with it barely did' : ''}.` }) });
// Its second half: towing the prize home. Done on arrival; the bounty is larger for a prize taken whole.
define({ id: 'prize_home', name: 'Deliver the prize', glyph: 'Y', kind: 'story', type: 'Capture the prize', reward: 1500,
  where: (ctx) => [ctx.home],
  objective: (ctx, m) => `Tow the ${m.data.name} to the research yard at ${ctx.home.planet || ctx.home.station}.`,
  brief: (ctx, m) => `The ${m.data.name} is under tow. The research yard at ${ctx.home.planet || ctx.home.station} is waiting for her; the bounty is paid on delivery.`,
  result: (ctx, m) => {
    const whole = m.data.grade === 'outstanding';
    return { credits: Math.round(m.reward * (whole ? 1.5 : 1)), standing: { [ctx.side]: whole ? 8 : 5 },
      log: `Research has the ${m.data.name}: the ${ctx.name(m.data.fac)} ${m.data.cls}'s drive and shields are being taken apart.` };
  } });

// ---- Serve -------------------------------------------------------------------------------------------------------------
// Distress call (ready): a freighter calls for help. Sometimes the call is a trap: the briefing cannot tell which.
define({ id: 'distress', name: 'Distress call', glyph: 'H', type: 'Distress call', reward: 500,
  where: (ctx) => ctx.hexes().filter((h) => ctx.plain(h) && (ctx.own(h) || h.owner === 'neutral') && ctx.dist(h, ctx.home) >= 2),
  setup: (ctx, m) => { m.data = { trap: ctx.rng() < 0.4, ship: ['Larkspur', 'Patience', 'Hollow Bell', 'Ninefold', 'Ember Reach'][Math.floor(ctx.rng() * 5)] }; },
  objective: (ctx, m) => `Answer the call and save the freighter ${m.data.ship}.`,
  brief: (ctx, m) => `The freighter ${m.data.ship} is calling for help: raiders on her, her shields failing. You are the nearest ship.`,
  battle: (ctx, m) => (m.data.trap
    ? { intel: 'Raiders, a few', groups: [{ fac: 'vos', count: 1, role: 'enemy', tag: 'decoy', label: `Decoy: ${m.data.ship}` }, { fac: 'vos', count: strengthCount(m) + 1, role: 'enemy', label: 'Waiting in ambush' }],
      objectives: [{ id: 'trap', text: 'Beat the trap', primary: true }, intact, { id: 'decoy', text: 'Take the decoy ship' }] }
    : { intel: 'Raiders, a few', groups: [{ fac: 'neutral', count: 1, role: 'neutral', tag: 'rescue', label: m.data.ship }, { fac: 'vos', count: strengthCount(m), role: 'enemy', label: 'Raiders' }],
      objectives: [{ id: 'save', text: `Keep the ${m.data.ship} alive`, primary: true }, { id: 'drive', text: 'Drive off the raiders' }, { id: 'cargo', text: "Take the raiders' shipment" }] }),
  result: (ctx, m, o) => (m.data.trap
    ? { credits: paid(m, o), standing: o.win ? { vos: -3 } : {}, log: o.win ? `The ${m.data.ship} was a Vos decoy, and the trap was beaten.` : `The ${m.data.ship} was a Vos decoy.` }
    : { credits: paid(m, o), standing: o.win ? { [ctx.side]: 3 } : {}, log: o.win ? `The ${m.data.ship} is safe and on her way.` : `The ${m.data.ship} was lost.` }) });

// ---- The first draft's other contracts (their battles wait on stations, planets and freighters: guide, "needs") -------
define({ id: 'station', name: 'Enemy station assault', glyph: 'S', type: 'Base assault', reward: 1200,
  where: (ctx) => ctx.hexes().filter((h) => h.station && ctx.hostile(h)),
  objective: () => 'Destroy the station.',
  brief: (ctx, m) => `${ctx.hex(m.c, m.r).station} anchors the ${ctx.name(ctx.hex(m.c, m.r).owner)} line here. Break its shields and destroy it.`,
  battle: (ctx, m) => ({ enemy: ctx.hex(m.c, m.r).owner, count: strengthCount(m) + 1 }),
  result: (ctx, m, o) => (o.win ? { credits: paid(m, o), standing: { [ctx.hex(m.c, m.r).owner]: -10, [ctx.side]: 5 } } : {}) });
define({ id: 'planet', name: 'Planetary assault', glyph: 'A', type: 'Planetary assault', reward: 1000,
  where: (ctx) => ctx.hexes().filter((h) => h.planet && ctx.hostile(h)),
  objective: () => 'Destroy the orbital defences.',
  brief: (ctx, m) => `The orbital guns over ${ctx.hex(m.c, m.r).planet} cover the approach. Silence them.`,
  battle: (ctx, m) => ({ enemy: ctx.hex(m.c, m.r).owner, count: strengthCount(m) }),
  result: (ctx, m, o) => (o.win ? { credits: paid(m, o), standing: { [ctx.hex(m.c, m.r).owner]: -8, [ctx.side]: 4 } } : {}) });
define({ id: 'ambush', name: 'Convoy ambush', glyph: 'C', type: 'Convoy raid', reward: 700,
  where: (ctx) => ctx.hexes().filter((h) => ctx.plain(h) && ctx.hostile(h) && ctx.routes(h.owner).includes(h)),
  objective: () => 'Destroy two of the three freighters.',
  brief: (ctx, m) => `A ${ctx.name(foeOf(ctx, ctx.hex(m.c, m.r)))} supply convoy passes through here. Destroy two of its three freighters before they jump.`,
  battle: (ctx, m) => ({ enemy: foeOf(ctx, ctx.hex(m.c, m.r)), count: strengthCount(m) + 1 }),
  result: (ctx, m, o) => (o.win ? { credits: paid(m, o) } : {}) });
define({ id: 'defend', name: 'Convoy defense', glyph: 'D', type: 'Convoy escort', reward: 600,
  where: (ctx) => ctx.routes(ctx.side).filter((h) => ctx.plain(h)),   // on one of your own freight routes
  objective: () => 'Keep two of the three freighters alive.',
  brief: () => 'Our freighters are expected through this sector and raiders are waiting for them. Keep two of the three alive.',
  battle: (ctx, m) => ({ enemy: ctx.rival || 'vos', count: strengthCount(m) }),
  result: (ctx, m, o) => (o.win ? { credits: paid(m, o), standing: { [ctx.side]: 3 } } : { standing: { [ctx.side]: -2 } }) });
define({ id: 'assassinate', name: 'Assassination', glyph: 'X', type: 'Fleet action', reward: 900,
  where: (ctx) => ctx.hexes().filter((h) => ctx.groupAt(h)?.commander),
  setup: (ctx, m) => { m.data.group = ctx.groupAt(ctx.hex(m.c, m.r)).name; },
  objective: () => 'Destroy the commander\'s ship.',
  brief: (ctx, m) => `${ctx.groupAt(ctx.hex(m.c, m.r))?.name || 'An enemy commander'} is in this sector. Destroy the flagship; the escorts do not matter.`,
  battle: (ctx, m) => {
    const g = ctx.groupAt(ctx.hex(m.c, m.r)), fac = g?.fac || ctx.rival || 'vos';
    return { groups: [{ fac, count: 1, role: 'enemy', tag: 'flagship', label: g?.name || 'Flagship' }, { fac, count: 2, role: 'enemy', label: 'Escorts' }],
      objectives: [{ id: 'flag', text: 'Destroy the flagship', primary: true }, intact] };
  },
  result: (ctx, m, o) => (o.win ? { credits: paid(m, o) } : {}) });

// ---- Story: a new captain's first command, an example of a chain (a mission that offers the next when it is done) -------
define({ id: 'shakedown', name: 'Shakedown cruise', glyph: '1', kind: 'story', reward: 250,
  where: (ctx) => ctx.near(ctx.home, 3).filter((h) => ctx.plain(h) && ctx.dist(h, ctx.home) >= 2 && h.terrain === 'open'),
  objective: () => 'Fly to the marker beacon and back on your own engines.',
  brief: () => 'Your first command is fresh from the yard. Take her out to the marker beacon and let the crew find her faults before anyone else does. No battle expected.',
  battle: () => null,
  result: () => ({ credits: 250, next: ['first_blood'], log: 'Shakedown done: the yard fixed what the crew found.' }) });
define({ id: 'first_blood', name: 'First blood', glyph: '2', kind: 'story', type: 'Patrol', reward: 500,
  where: (ctx) => ctx.near(ctx.home, 5).filter((h) => ctx.plain(h) && ctx.dist(h, ctx.home) >= 3),
  objective: () => 'Drive off the raider watching your home port.',
  brief: (ctx) => `A single raider has been shadowing traffic out of ${ctx.home.planet || ctx.home.station}. You are the nearest ship. Drive it off.`,
  battle: () => ({ name: 'First blood', groups: [{ fac: 'vos', count: 1, role: 'enemy', label: 'Raider' }],
    objectives: [{ id: 'off', text: 'Drive off or destroy the raider', primary: true }, { id: 'kill', text: 'Destroy it before it runs' }] }),
  result: (ctx, m, o) => (o.win ? { credits: paid(m, o), standing: { [ctx.side]: 5, vos: -5 }, log: 'The raider is gone; the port has noticed you.' }
    : { next: ['first_blood'], log: 'The raider got away. It will be back.' }) });

return { define, get: (id) => ALL[id], ids: () => Object.keys(ALL), contracts: () => Object.values(ALL).filter((s) => s.kind === 'contract').map((s) => s.id),
  battleOf, score, prestigeOf, GRADES, GRADE_NAME, PAY, PRESTIGE };
})();
