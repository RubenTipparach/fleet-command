// Fleet Command mockups: a faction skin's UI kit as CSS, for pages whose panels are HTML rather than canvas (the game
// flow's menus, campaign map and port panels, docs/mockups/game-flow.html; owner, 2026-11-05: "Can we update campaign UI
// with new skins?"). One implementation of each skin's look (CLAUDE.md section 6.1): the pieces are drawn by the skin's
// own painters (docs/mockups/lib/concord.js, krai.js, ssarith.js, the same `kit` the battle console draws), and this file
// only turns each 9-slice piece into a CSS border-image with the piece's own slice margins. Nothing here draws.
//
// FCKitCss(skin, scope) returns a Promise of a CSS string whose rules all start with `scope` (for example
// 'body[data-skin="krai"]'). The page adds it to a <style> once per skin. Elements it styles, by class:
//   .panel (frame.panel), .panel.inset (frame.inset), .key (button.idle, .hover, .pressed), .tab (tab.idle, tab.active),
//   .bar (bar.track) and its <i> fill (bar.fill, bar.fill.red when .over); plus the colour and type tokens the page's
//   own CSS reads (--ink, --dim, --lamp, --ok, --warn, --display, --body, --ground: the panel's own ground). Inside its frame a panel is the skin's own ground
//   (CC.bg: the Concord's black, behind its flat back-lit vector, as on the battle console).
// Needs docs/mockups/lib/kitdraw.js and the skin's module inlined first.
window.FCKitCss = (function () {
'use strict';
const { paint } = window.FCDraw;
// A piece drawn by its skin at its reference size (paint draws at 2x), as a data URI.
function uri(piece) { return paint(piece.size[0], piece.size[1], piece.draw).toDataURL('image/png'); }
// A 9-slice piece as border-image declarations, its margins scaled by k so a large rendered frame fits a small element.
function nine(piece, k) {
  const [t, r, b, l] = piece.m, px = (v) => `${Math.round(v * k * 10) / 10}px`;
  return `border-style: solid; border-color: transparent; border-width: ${px(t)} ${px(r)} ${px(b)} ${px(l)};
    border-image: url(${uri(piece)}) ${t * 2} ${r * 2} ${b * 2} ${l * 2} fill / ${px(t)} ${px(r)} ${px(b)} ${px(l)} stretch;`;
}
// The scale for a piece so its largest margin is at most `max` px (never above 1).
const fit = (piece, max) => Math.min(1, max / Math.max(1, ...piece.m));
// The scale for a key or tab: the piece drawn at the element's height.
const tall = (piece, h) => h / piece.size[1];
// A flat skin's panel sits on one box of its ground, borders and all, its corners rounded to the frame's outer curve and a
// band of the ground wrapping round the outside (owner, 2026-11-05, of the Concord menu: "can we get a whole box around this?
// include the border too, the black background shoule wrap and curve aorund the outer border"). Only the flat Concord: a
// metal skin's frame is its own ground, and black behind it is forbidden (CLAUDE.md section 7). The value is the frame's
// outer corner radius at the reference size (lib/concord.js, the sweep's R).
const WRAP = { concord: { radius: 24, band: 6 } };
// A key that cannot be pressed is the skin's idle key, unlit: darkened and dulled, solid so the ground behind does not show
// through (owner, 2026-11-05: "disabled butons should be like dark"). The same treatment in every skin, until a skin has a
// button.disabled piece of its own.
const DARK = 'filter: brightness(0.42) saturate(0.45); opacity: 1; cursor: not-allowed; box-shadow: none; transform: none;';

function css(skin, scope) {
  return Promise.resolve(skin.load()).then(() => {
    const K = skin.kit, C = skin.CC, ink = skin.keyInk || {}, fam = skin.family || 'sans-serif';
    for (const id of ['frame.panel', 'frame.inset', 'button.idle', 'button.hover', 'button.pressed', 'tab.idle', 'tab.active', 'bar.track', 'bar.fill', 'bar.fill.red'])
      if (!K[id]) throw new Error(`skin ${skin.id} has no kit piece ${id}`); // a piece a skin lacks is a build error, not a fallback
    const S = scope, panelK = fit(K['frame.panel'], 26), insetK = fit(K['frame.inset'], 12);
    const key = (id, h) => nine(K[id], tall(K[id], h)), wrap = WRAP[skin.id];
    return `
${S} { --ink: ${C.text}; --dim: ${C.lilac}; --faint: ${C.ver || C.lilac}; --lamp: ${C.sky}; --lamp-soft: ${C.sky}2e; --ok: ${C.accord};
  --warn: ${C.alert}; --display: ${fam}; --body: ${fam}; --ground: ${C.bg}; }
${S} .panel { ${nine(K['frame.panel'], panelK)} background: ${C.bg} ${wrap ? 'border-box' : 'padding-box'}; padding: 4px 6px;
  ${wrap ? `border-radius: ${Math.round(wrap.radius * panelK)}px; box-shadow: 0 0 0 ${wrap.band}px ${C.bg};` : 'border-radius: 0; box-shadow: none;'} }
${S} .panel.inset { ${nine(K['frame.inset'], insetK)} background: none; box-shadow: none; padding: 4px; }
${S} .topbar.panel { padding: 0 6px; }
${S} .key { ${key('button.idle', 44)} background: none; box-shadow: none; border-radius: 0; color: ${ink.idle || C.text}; padding: 0 4px; }
${S} .key:hover:not(:disabled) { ${key('button.hover', 44)} background: none; color: ${ink.hover || C.text}; }
${S} .key:active:not(:disabled), ${S} .key.go, ${S} .key.on { ${key('button.pressed', 44)} background: none; color: ${ink.pressed || C.text}; box-shadow: none; transform: none; }
${S} .key.go:hover:not(:disabled) { ${key('button.hover', 44)} background: none; color: ${ink.hover || C.text}; }
${S} .key.danger { filter: hue-rotate(${skin.id === 'concord' ? 140 : 0}deg) saturate(1.3); }
${S} .key.small { ${key('button.idle', 36)} padding: 0 2px; }
${S} .key.small:hover:not(:disabled) { ${key('button.hover', 36)} }
${S} .key.small.go, ${S} .key.small:active:not(:disabled) { ${key('button.pressed', 36)} }
${S} .key:disabled, ${S} .key.go:disabled, ${S} .key.on:disabled { ${key('button.idle', 44)} ${DARK} color: ${C.lilac}; }
${S} .key.small:disabled, ${S} .key.small.go:disabled { ${key('button.idle', 36)} }
${S} .tab { ${key('tab.idle', 40)} background: none; border-radius: 0; color: ${ink.tabOff || C.text}; padding: 0 2px; box-shadow: none; }
${S} .tab[aria-selected="true"] { ${key('tab.active', 40)} background: none; color: ${ink.tabOn || C.text}; box-shadow: none; }
${S} .bar { ${nine(K['bar.track'], 1)} border-top-width: 0; border-bottom-width: 0; height: ${K['bar.track'].size[1]}px; background: none; box-shadow: none;
  border-radius: 0; padding: ${(K['bar.track'].inset || [3, 3])[1]}px 0; box-sizing: border-box; overflow: visible; }
${S} .bar i { ${nine(K['bar.fill'], 1)} border-top-width: 0; border-bottom-width: 0; background: none; box-sizing: border-box; }
${S} .bar.over i { ${nine(K['bar.fill.red'], 1)} border-top-width: 0; border-bottom-width: 0; }
`;
  });
}
return css;
})();
