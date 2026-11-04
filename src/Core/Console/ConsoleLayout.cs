// ConsoleLayout: where every region of the battle console sits for a window, in reference pixels (CLAUDE.md section 7;
// battle-console design section 2). The console scales to the window's height, and to MinW of width at most, so the
// left column's panels keep their reference size and only the strips and the view stretch. Expanded, one panel takes
// the left two thirds and the live battle the right third. Pure arithmetic with no Godot types, so the layout test and
// the game share it.
using System;
using System.Collections.Generic;
using FleetCommand.Data;

namespace FleetCommand.ConsoleModel;

/// <summary>The panels and strips of the console.</summary>
public enum Region { Plate, Station, Ship, Weapons, Radar, Top, Bottom }

public sealed record ConsoleFrame(double Scale, double W, double H, IReadOnlyDictionary<Region, Rect> Regions, Rect View, Region? Expanded);

public static class ConsoleLayout
{
    /// <summary>The panels a captain can expand (the strips cannot).</summary>
    // The radar row expands into the tactical map, which is not built yet.
    public static readonly Region[] Expandable = { Region.Station, Region.Ship, Region.Weapons };

    /// <summary>The scale from reference pixels to window pixels.</summary>
    public static double Scale(LayoutDef l, double windowW, double windowH) => Math.Min(windowH / l.ReferenceH, windowW / l.MinW);

    /// <summary>The console for a window of the given size in reference pixels (window size over Scale). A skin whose
    /// strip art is taller than the layout's strip_h_px gives its own strip heights (the Krai's 60 and 80 px, mockup 8).</summary>
    public static ConsoleFrame Compute(LayoutDef l, double w, double h, Region? expanded, double scale = 1, double? topH = null, double? bottomH = null)
    {
        double th = topH ?? l.StripH, bh = bottomH ?? l.StripH;
        var r = new Dictionary<Region, Rect>();
        double cw = l.ColumnW;
        Rect view;
        if (expanded is { } e && Array.IndexOf(Expandable, e) >= 0)
        {
            double split = Math.Round(w * l.ExpandedShare);
            r[e] = new Rect(l.ExpandedX, l.ExpandedY, split - 2 * l.ExpandedX, h - l.ExpandedY - l.ExpandedX);
            view = new Rect(split, 0, w - split, h);
        }
        else
        {
            expanded = null;
            r[Region.Plate] = l.Plate;
            r[Region.Station] = l.Station;
            r[Region.Ship] = l.Ship;
            r[Region.Weapons] = l.Weapons;
            r[Region.Radar] = l.Radar;
            r[Region.Top] = new Rect(cw, 0, w - cw, th);
            r[Region.Bottom] = new Rect(cw, h - bh, w - cw, bh);
            view = new Rect(cw, th, w - cw, h - th - bh);
        }
        return new ConsoleFrame(scale, w, h, r, view, expanded);
    }
}
