// Fleet Command mockups: the small drawing helpers every UI kit painter uses (docs/mockups/console-skins.html,
// docs/mockups/battle-console.html, docs/mockups/lib/concord.js). One copy; inlined by tools/mockups/inline_data.py.
window.FCDraw = (function () {
'use strict';
function rng(seed) { let a = seed >>> 0; return () => { a = (a + 0x6d2b79f5) >>> 0; let t = a; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }; }
function hash(s) { let h = 2166136261; for (const ch of s) h = Math.imul(h ^ ch.charCodeAt(0), 16777619); return h >>> 0; }
// A canvas at 2x for a piece of w x h reference pixels; draw() works in reference pixels.
function paint(w, h, draw) {
  const c = document.createElement('canvas'); c.width = w * 2; c.height = h * 2;
  const x = c.getContext('2d'); x.scale(2, 2); x.lineJoin = 'round'; x.lineCap = 'round'; draw(x, w, h); return c;
}
const png = (c) => c.toDataURL('image/png');
const P = Math.PI;
function rr(x, X, Y, W, H, r) { x.beginPath(); x.roundRect(X, Y, W, H, r); }
return { rng, hash, paint, png, P, rr };
})();
