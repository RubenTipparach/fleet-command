# Proposal: hull-texture-designs

## Why

The owner, 2026-11-06: "Alright we kind of have new requirements for ship texture designs. Let's implement custom texture
designs to both the krai and concord. Concord could also use a bit of detail on their ships too to avoid them looking to
much like primitives. Pitch a few different texture designs. Start with frigate of each and propose4-5 different
texturing approaches for each. Goal is to follow culture and ideology".

The Concord line (mockup 6) and the Krai line (mockup 7) are approved and in the game, but their hulls are still dressed
by a detail tile projected in ship space with a stripe or a war stripe over it. The Ship Design Bible's texture chapter
(15, T1-T8) and CLAUDE.md section 10 already ask for more: every ship unwrapped on its own, its main colour map painted
onto its own UVs and never a repeating tile, layered armour, distinct features, nothing repeated. Both task lists say the
Ssarith Hunt goes first, and the Hunt is now done (mockup 12, change `ssarith-ships-engine`). The Concord's lofts also
read as primitives up close: smooth octagons with seams, little to catch the light.

## What Changes

- **Mockup 35**, `docs/mockups/hull-texture-designs.html`: the Concord's Petrel frigate and the Krai frigate, the very
  models the game flies (inlined from `assets/ships/`), each wearing five texture designs drawn live over their own
  geometry, one at a time or all five side by side, in studio and night light.
  - Five Concord designs and five Krai designs, each built from a named tradition inside its faction's accepted culture
    (CLAUDE.md section 1: name the tradition, never the body). The design document gives each one's ideology, palette,
    plating, markings and night look.
  - Every Concord design breaks the hull into plates of its own (strakes, sections, panels), with panel lines, seams and
    hatches that catch the light, so the lofts stop reading as primitives before any geometry is added.
- **A pitch, not the bake.** The page paints each design in the shader, in ship space, so ten designs can be compared on
  one page. The picked designs are then made the binding way: each ship unwrapped on its own, a modelled detail layer
  (`hull_detail.json` for the faction), baked and painted onto its own UVs by `painted_bake.py --ship` (the method that
  made the Hunt), and exported to the game.
- Not in this change: the bake and the game. Those follow once the owner picks (CLAUDE.md section 4: approved for build,
  then approved for implementation).

## Impact

- New: `openspec/changes/hull-texture-designs/`, `docs/mockups/hull-texture-designs.html`, its shots and its artifact
  record; mockup-work row 24 and a tracker entry.
- Read only: `assets/ships/concord/petrel_level.glb`, `assets/ships/krai/frigate.glb`, the faction palettes.
