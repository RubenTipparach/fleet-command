// The Axiom skin (mockup 4, OpenSpec change console-skins): a purely mathematical faction (owner, 2026-11-07: "Id like a
// faction that is purely mathematical. A faction based on fractal patterns and designs."). Name and culture are ours and
// provisional until the owner sets them on the survey. The look has one implementation, docs/mockups/lib/axiom.js
// (window.FCAxiom, inlined before this module), shared with the battle console (openspec/changes/axiom-battle-console);
// this module keeps the skins page's own tokens (its CSS, type, title and plate placement, world view and palette rows)
// and registers the skin with this page's registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, H = window.FCAxiom, AX = H.AX;

// ---- the skin ----------------------------------------------------------------------------------------------------
F.register('axiom', {
  name: 'The Axiom', kind: 'Flat and exact, drawn by the mathematics: Koch keys, Sierpinski corners, Cantor rules, escape-time screens', bg: AX.void,
  css: { '--s-ink': AX.proof, '--s-dim': AX.dim, '--s-num': AX.cyan, '--s-body': '"Space Mono", monospace', '--btn-ink': AX.proof, '--btn-shadow': '0 0 2px #05060c, 0 0 6px rgba(63,224,230,0.45)', '--btn-justify': 'center',
    '--btn-font': '700 15px/1 "Space Mono", monospace', '--btn-track': '0.12em', '--btn-case': 'uppercase', '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1px',
    '--tab-font': '400 13px/1 "Space Mono", monospace', '--tab-ink': AX.dim, '--tab-ink-on': AX.gold, '--title-font': '700 14px/1 "Martian Mono", monospace', '--title-ink': AX.cyan,
    '--lbl-font': '400 13px/1 "Space Mono", monospace', '--num-font': '700 24px/1 "Space Mono", monospace',
    // The strips are 72 px; their open band lies between the outer hairline (row 1.5) and the inner rule (row 67.5 of the
    // top strip, 4.5 of the bottom), so each label stands over its bar clear of both.
    '--stt-pt': '7px', '--stt-pb': '10px', '--stb-pt': '10px', '--stb-pb': '7px', '--strip-lamp': '44px' },
  accent: AX.cyan, warn: AX.magenta,
  // The title sits in the frame's top band, right of the corner's Sierpinski triangle, under the fine rule.
  title: (t, m) => { t.style.cssText = `left:${30 - m[3]}px;top:${9 - m[0]}px;letter-spacing:0.08em;`; },
  // The captain's name sits in the plain middle of the name plate's Koch key (54-406), clear of its Koch ends.
  plateText: { left: 62, top: 12, width: 336, height: 28, font: '700 19px/28px "Space Mono", monospace', color: AX.proof, sub: 'FIXED POINT · ITERANT ORU' },
  helmKeys: 'ring', helmR: 100, shield: [AX.cyan, AX.gold, AX.magenta],
  fonts: ['400 20px "Space Mono"', '700 20px "Space Mono"', '700 20px "Martian Mono"'], view: { title: '700 26px "Space Mono"', body: '400 22px "Space Mono"', upper: true, ink: AX.proof, dim: AX.dim },
  build: () => F.fromPainters(H.nine, H.heroSheet, (n) => H.icon(n), false),
  live: H.live,
  world: ['Every true thing is a rule and its iteration; a structure is honoured by how deep its rule has been carried.',
    'Their bodies are not described and shape nothing here: the console answers to their three schools of thought.',
    'Three schools at once: the escape-time school\'s boundary screens, the tilers\' self-similar frames and plates, the branchers\' grown marks.',
    'Hulls laid out by substitution: every part a smaller copy of the whole (provisional).'],
  shapes: 'Koch keys (hexagons with snowflake ends), Sierpinski corners and carpet cells, Hilbert traces, Cantor rules, a dragon-curve crest.',
  light: 'None painted: exact thin lines and flat fills on the void, glowing a little; depth of iteration is colour, indigo to cyan.',
  motion: 'A hovered key iterates one level deeper; a pressed key fills solid in a gold outline; bar cells light one by one.',
  type: '"Martian Mono" titles, wide and even, over "Space Mono" keys and readouts.',
  palette: [['Void', AX.void, 'The ground', 'The unbounded plane every rule is drawn on'], ['Proof white', AX.proof, 'Text, labels', 'Stated truth'],
    ['Iteration indigo', AX.indigo, 'Frames, idle keys', 'A rule applied once or twice: shallow'], ['Iteration cyan', AX.cyan, 'Live lines, readouts, icons', 'A rule carried deep'],
    ['Convergence gold', AX.gold, 'Pressed keys, the chosen tab, stop', 'What converges is true; ready'], ['Divergence magenta', AX.magenta, 'Alerts only', 'What escapes to infinity: danger']],
});
})();
