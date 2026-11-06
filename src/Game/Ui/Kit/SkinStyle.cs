// SkinStyle: everything a console panel draws differently in each skin besides the named kit pieces (CLAUDE.md section
// 7: "a skin is a design language, not a recolour"). The battle console mockup reads the same fields from each skin's
// library (docs/mockups/lib/concord.js and docs/mockups/lib/krai.js; openspec/changes/krai-battle-console/design.md,
// decision 2), so a panel picks a role here and never writes a skin's colour, type or shape itself. A skin's optional
// features (the joined column, placards, symbol bars, rendered keys) are declared here, and KitPieces.Verify refuses a
// skin that declares one without the pieces it needs (a missing piece is a build error, never a fallback).
using System;
using Godot;

namespace FleetCommand.Game.Ui.Kit;

/// <summary>The colour roles the console once wrote as bare colours (the mockup's CC.ink, inkDis, night, ghost, mute,
/// plate and title).</summary>
/// <param name="Ink">Ink on a lit fill.</param>
/// <param name="InkDis">Ink on a disabled key.</param>
/// <param name="Night">The fill of an unarmed key (the warp key offline).</param>
/// <param name="Ghost">The name on an unarmed key.</param>
/// <param name="Mute">A blank key that does nothing (the helm pad's corner).</param>
/// <param name="Plate">The dark behind a panel title and a hover card.</param>
/// <param name="Title">A panel's title.</param>
/// <param name="Sheet">The menu sheet's background.</param>
public readonly record struct Roles(Color Ink, Color InkDis, Color Night, Color Ghost, Color Mute, Color Plate, Color Title, Color Sheet);

/// <summary>Ink on the kit's keys and tabs by state (the mockup's keyInk).</summary>
public readonly record struct KeyInk(Color Idle, Color Hover, Color Pressed, Color TabOn, Color TabOff)
{
    /// <summary>The ink for a key piece's state: "idle", "hover" or "pressed".</summary>
    public Color For(string state) => state switch { "hover" => Hover, "pressed" => Pressed, _ => Idle };
}

/// <summary>The marks the Concord draws in white (the mockup's mark): a weapon box's hover outline, its ready-to-fire
/// outline, and countdown figures.</summary>
public readonly record struct Marks(Color Hover, Color Ready, Color Count);

/// <summary>Space in pixels: top, right, bottom, left.</summary>
public readonly record struct Pad(float T, float R, float B, float L);

/// <summary>A face of the skin's type at a size: the body face or the heading face, its size in pixels and weight.</summary>
public readonly record struct Face(bool Heading, int Size, int Weight);

/// <summary>The skin's type: the font files of its body and heading faces (res:// paths) and how the console's sizes
/// and weights, written for Antonio, map onto them (the mockup's font(spec)).</summary>
public sealed record TypeSet(string Body, string Heading, Func<int, int, Face> Map);

/// <summary>The name plate's live field (the mockup's plate): where the ship and captain's names start, their room,
/// face and colour.</summary>
public readonly record struct PlateText(float X, float Y, float Max, Face Face, Color Col);

/// <summary>A strip's needle meter (the Kelvane's printed meter bays; the mockup's meter): its pivot, its needle length,
/// the sweep from empty (A0) to full (A1) in radians clockwise from +x, at the strip art's own height H, and how fast the
/// needle settles toward its reading, per second.</summary>
public readonly record struct StripMeter(float Cx, float Cy, float H, float R, float A0, float A1, float Settle);

/// <summary>A skin's look for everything that is not a named piece.</summary>
public sealed record SkinStyle
{
    public required Roles R { get; init; }
    public required TypeSet Type { get; init; }
    /// <summary>The ink that reads on a drawn fill (the mockup's ink(fill)).</summary>
    public required Func<Color, Color> InkOn { get; init; }
    public required KeyInk KeyInk { get; init; }
    /// <summary>A panel tab's icon at rest (the Krai's red, apart from the functional icons' orange).</summary>
    public required Color TabIcon { get; init; }
    /// <summary>Glyphs on dark rendered glass (only read when the skin has rendered keys).</summary>
    public required Color GlyphOnDark { get; init; }
    public required Marks Mark { get; init; }
    /// <summary>The content inside a panel frame, clear of its rim (the mockup's pad).</summary>
    public required Pad Pad { get; init; }
    public required PlateText Plate { get; init; }
    /// <summary>Labels centred on keys and tabs, in the open field between their caps (a key that ends in a point).</summary>
    public bool KeyCenter { get; init; }
    /// <summary>How far the top and bottom strips' content moves down to sit in the channel of the skin's strip art, in
    /// pixels, at the console's strip height (data/console/layout.json strip_h_px).</summary>
    public float StripTopDy { get; init; }
    public float StripBottomDy { get; init; }
    /// <summary>The strips' heights in reference pixels when the skin's strip art differs from the layout's strip_h_px
    /// (the mockup's stripTop.h and stripBottom.h); null keeps the layout's.</summary>
    public double? StripTopH { get; init; }
    public double? StripBottomH { get; init; }

    // ---- Features a skin's kit may carry (openspec/changes/krai-battle-console, decision 3) -------------------------
    /// <summary>One plate behind the whole left column ("console.column"), the docked panels formed into it as wells,
    /// so a docked station, ship or weapons panel draws no frame of its own.</summary>
    public bool Column { get; init; }
    /// <summary>Every label set on a placard ("plate.label").</summary>
    public bool LabelPlacard { get; init; }
    /// <summary>A panel's title set on a placard ("plate.title") instead of a dark notch.</summary>
    public bool TitlePlacard { get; init; }
    /// <summary>The strips run on into the column ("strip.top.join", "strip.bottom.join").</summary>
    public bool JoinedStrips { get; init; }
    /// <summary>Every bar a row of the skin's symbol ("bar.cell.on", ".off", ".warn", ".ok").</summary>
    public bool SymbolBars { get; init; }
    /// <summary>Keys drawn as rendered glass lit by state ("key.*" and the wide "keyw.*") instead of filled shapes.</summary>
    public bool RenderedKeys { get; init; }

    // ---- Label fields and the Institute's meters (openspec/changes/kelvane-console-engine) ---------------------------
    /// <summary>A key's measured label field: pixels from each end at the key piece's own height, where its 9-slice caps
    /// run past the field to keep a window's corners whole (the mockup's keyField: the Kelvane's). Null: the caps.</summary>
    public (float L, float R)? KeyField { get; init; }
    /// <summary>A tab's measured label field, the same way (the mockup's tabField). Null: the tab's own end caps.</summary>
    public (float L, float R)? TabField { get; init; }
    /// <summary>Icon tabs (the station and ship rails) drawn on the square rendered key ("key.idle", "key.hover",
    /// "key.on"), the icon inside its glass, not on the long tab piece (the mockup's iconKeys).</summary>
    public bool IconKeys { get; init; }
    /// <summary>How far a panel title's baseline rises to centre it in the title placard's window (the mockup's titleDy).</summary>
    public float TitleDy { get; init; }
    /// <summary>The strips' needle meters, when the skin's strip art carries a printed scale (the mockup's meter): hull
    /// integrity on the top strip, speed on the bottom. Needs <see cref="Needle"/>.</summary>
    public StripMeter? Meter { get; init; }
    /// <summary>Draws a meter's needle at its pivot, with its length and angle (the mockup's live.needle).</summary>
    public Action<CanvasItem, Vector2, float, float>? Needle { get; init; }
}
