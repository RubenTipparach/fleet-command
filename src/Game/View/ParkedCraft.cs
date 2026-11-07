// ParkedCraft: a carrier's craft parked on its landing pads in the 3D view (mockup 23, the Kelvane carrier; owner, 2026-11-06:
// "carrier should have landing pads and fighters onthe inside here"; 2026-11-07: "Carrier should have shuttles upside down as
// well. We are in space afterall"). The pads are the glb's pad_* empties (tools/mockups/export_ships.mjs, from the bake's
// pads), each naming the craft that parks on it and how far up its normal the craft's mockup origin stands; the craft is that
// craft's own approved glb, dressed once by ShipSkin (its lamps as glows only, no live lights) and copied onto each pad, standing up the pad's normal (so one on a
// ceiling hangs upside down) and nosed along the pad. A pad holds a craft while one is in the bay (Ship.Fighters), the craft
// spread evenly over the pads; launched craft leave their pads empty, and a ship that has come apart shows none. They hang
// under the glb's root beside its pieces, never inside one, so the breakup's copies of the hull do not copy them and the
// hull's damage never reads them (they are added after ShipDamage has walked the ship). View only: the simulation never
// reads it.
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace FleetCommand.Game.View;

public sealed class ParkedCraft
{
    private readonly List<Node3D> _craft = new();

    private ParkedCraft() { }

    /// <summary>How many pads the ship has craft for.</summary>
    public int Pads => _craft.Count;

    /// <summary>The craft on a ship's pads (glb: the ship's glb root, as ShipSkin dressed it, scaled to the battle), or null for
    /// a ship with no pad that names a craft.</summary>
    public static ParkedCraft? Park(Node3D glb)
    {
        var pads = new List<(Node3D Pad, Godot.Collections.Dictionary X)>();
        Walk(glb, n =>
        {
            if (n is Node3D e && e.Name.ToString().StartsWith("pad_") && e.HasMeta("extras") && e.GetMeta("extras").AsGodotDictionary() is { } x && x.ContainsKey("craft"))
                pads.Add((e, x));
        });
        if (pads.Count == 0) return null;
        string file = pads[0].X["craft"].AsString();
        var scene = GD.Load<PackedScene>("res://assets/ships/" + file) ?? throw new System.InvalidOperationException($"parked craft not found: res://assets/ships/{file}");
        Node3D template = scene.Instantiate<Node3D>();
        ShipSkin.Apply(template, file, glb.Scale.X);   // once: every copy shares its dressed materials
        var lights = new List<Light3D>();   // a parked craft's lamps show by their glows alone: two dozen craft of live lights would cost a phone its frame
        Walk(template, n => { if (n is Light3D l) lights.Add(l); });
        foreach (Light3D l in lights) { l.GetParent().RemoveChild(l); l.Free(); }
        JsonElement o = ShipSkin.ShipEntry(file).GetProperty("origin_offset_m");
        var origin = new Vector3((float)o[0].GetDouble(), (float)o[1].GetDouble(), (float)o[2].GetDouble());   // the craft's mockup origin, in its glb, is at -origin
        var park = new ParkedCraft();
        var holder = new Node3D { Name = "parked_craft" };
        glb.AddChild(holder);
        foreach (var (pad, x) in pads)
        {
            Vector3 n = Vec(x["normal"]).Normalized(), a = Vec(x["along"]).Normalized();
            var basis = new Basis(n.Cross(-a), n, -a);   // up the normal, the bow (-Z) along the pad
            Vector3 at = InGlb(pad, glb).Origin + n * (float)x["stand_m"].AsDouble();
            var craft = (Node3D)template.Duplicate();
            craft.Name = "craft_" + pad.Name.ToString()["pad_".Length..];
            craft.Transform = new Transform3D(basis, at + basis * origin);
            holder.AddChild(craft);
            park._craft.Add(craft);
        }
        template.QueueFree();
        return park;
    }

    /// <summary>Shows a craft on as many pads as there are craft in the bay, spread evenly, and none on a broken ship.</summary>
    public void Show(int inBay, bool broken)
    {
        int n = broken ? 0 : System.Math.Clamp(inBay, 0, _craft.Count);
        for (int i = 0; i < _craft.Count; i++) _craft[i].Visible = false;
        for (int k = 0; k < n; k++) _craft[k * _craft.Count / n].Visible = true;
    }

    // a node's transform in the glb root's frame
    private static Transform3D InGlb(Node3D n, Node3D glb)
    {
        Transform3D t = Transform3D.Identity;
        for (Node? p = n; p != null && p != glb; p = p.GetParent()) if (p is Node3D p3) t = p3.Transform * t;
        return t;
    }

    private static Vector3 Vec(Variant v) { float[] f = v.AsFloat32Array(); return new Vector3(f[0], f[1], f[2]); }

    private static void Walk(Node n, System.Action<Node> f) { f(n); foreach (Node c in n.GetChildren()) Walk(c, f); }
}
