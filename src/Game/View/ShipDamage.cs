// ShipDamage: battle damage on one approved ship in the 3D view (owner, 2026-11-04: "add in explosions when they get hit, and
// if a part gets hit enough break it appart like in the damage demo, also add the damage scars stuff too"). The rules are
// the ship mockups' (mockup 6's Damage tab, on docs/mockups/lib/ship-fx.js and ship-break.js): every hit that lands makes a
// fireball where it struck the plating facing the shooter, or a blue flare when the shield stops it; it leaves a scorch mark
// (lances a clean burn, torpedoes and bolts a burn with an ember ring that cools over 20 s), and a torpedo a hole that shows
// the decks behind it (hull.gdshader with damage.gdshaderinc). A part hit hard enough on a damaged hull is blown off: an
// outer cell of the main hull or a pylon, nacelle or command block, with a big blast, its edges charred and burning, the
// faces where it broke closed by cut faces with rooms behind them (cutface.gdshader). When the ship is destroyed it comes
// apart as the mockup's core breach does: for 3.6 s outer pieces blow off one by one, then the main blast throws the rest.
// The cells are the mockup's own fracture (exported by tools/mockups/export_ships.mjs into the manifest's "break"): each is a
// copy of the body whose hull shader keeps only that cell's skin, so no mesh is cut here. View only: the simulation never
// reads it, and its randomness (where on the plating a shot lands) is the view's own, seeded per ship.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed class ShipDamage
{
    /// <summary>How long a scorch mark's ember ring takes to cool, and a broken edge's, seconds (the mockups' 20 s and 30 s).</summary>
    public const float ScorchCoolS = 20f, EdgeCoolS = 30f;
    /// <summary>A part comes off once the hull is below this share of its whole, and the part has taken this share of the hull's hit points.</summary>
    public const double ChunkBelowHull = 0.6, ChunkHpShare = 0.14;
    /// <summary>The core breach: outer pieces blow off between First and Last seconds, the main blast at Main; Share of the main hull's cells go early.</summary>
    public const float BreachFirstS = 0.5f, BreachLastS = 3.0f, BreachMainS = 3.6f, BreachShare = 0.55f;
    /// <summary>Hull points of damage a scar of each kind marks at most (24 of each, as the mockups).</summary>
    public const int MaxScars = 24;

    private static Shader? _cutShader;

    private readonly Node3D _ship;      // the glb's root, scaled to the class's battle length
    private readonly Node _fx;          // where effects go (the battle view)
    private readonly Ship _sim;
    private readonly float _upm;        // scene units per metre
    private readonly float _lenM;       // the ship's length in its model (mockup metres)
    private readonly float _lenU;       // and in the view, scene units
    private readonly Vector3 _c;        // the export's re-centring (glb frame)
    private readonly Vector3 _core;     // the reactor, mockup ship space
    private readonly List<ShaderMaterial> _mats = new();
    private readonly List<(Vector3 P, Vector3 N)> _skin = new();   // points on the plating, mockup ship space
    private readonly Vector3 _centre;
    private readonly Random _rng;
    private readonly List<Body> _bodies = new();
    private readonly List<Part> _parts = new();
    private readonly List<Piece> _clusters = new();
    private int _groups;
    /// <summary>How many cells a chunk of the breaking hull holds, and how often (owner, 2026-11-04: "dont break up all
    /// pieces, some can stay in tact as 2-3 piece clusters"): one, two or three neighbouring cells.</summary>
    public static readonly (int Cells, float Weight)[] ChunkSizes = { (1, 0.2f), (2, 0.25f), (3, 0.25f), (5, 0.3f) };
    /// <summary>The share of a hull's cells that stay together as one big hulk (owner, 2026-11-04: "the ship parts look too
    /// even"): the first chunk grows this large, so a broken ship leaves a hulk, some chunks and some small pieces.</summary>
    public const float HulkShare = 0.4f;
    private readonly Vector4[] _scorch = new Vector4[MaxScars];
    private readonly float[] _heat = new float[MaxScars];
    private readonly Vector4[] _holes = new Vector4[MaxScars];
    private readonly Vector4[] _holeN = new Vector4[MaxScars];
    private int _scorchN, _holeN0;
    private float _breachT = -1;
    private readonly List<(float At, Action Do)> _breach = new();

    /// <summary>The whole ship is gone to pieces (the main blast has gone off).</summary>
    public bool Broken { get; private set; }

    private sealed class Piece
    {
        public required Node3D Node;
        public required Vector3 Centre;          // mockup ship space
        public Vector3 Vel, Spin;                 // metres a second in the ship's frame; radians a second
        public bool Off;
        public float Age;
        public double Hit;                        // hull points of damage it has taken
        public int Group;                         // the chunk it left the ship in (0 still on the ship)
        public readonly List<Piece> Members = new(); // a cluster's cells, which fly as one chunk
        public readonly List<ShaderMaterial> Mats = new();
        public readonly List<Node3D> Faces = new();
    }
    private sealed class Body
    {
        public required Node3D Node;
        public required JsonElement Def;
        public Vector3[] Seeds = Array.Empty<Vector3>();
        public readonly List<Piece> Cells = new();
        public bool Swapped;
    }
    private sealed class Part
    {
        public required Piece P;
        public required JsonElement Def;
    }

    public ShipDamage(Node3D ship, Node fx, Ship sim, string file, float unitsPerMetre)
    {
        _ship = ship; _fx = fx; _sim = sim; _upm = unitsPerMetre;
        _rng = new Random(sim.Id.Aggregate(17, (h, ch) => h * 31 + ch));
        JsonElement entry = ShipSkin.ShipEntry(file);
        JsonElement o = entry.GetProperty("origin_offset_m");
        _c = new Vector3(F(o[0]), F(o[1]), F(o[2]));
        _lenM = F(entry.GetProperty("length_m"));
        _lenU = _lenM * _upm;
        JsonElement brk = entry.GetProperty("break");
        _core = V(brk.GetProperty("core"));
        Walk(_ship, n =>
        {
            if (n is not MeshInstance3D mi || mi.Mesh == null) return;
            for (int i = 0; i < mi.Mesh.GetSurfaceCount(); i++)
                if (mi.GetSurfaceOverrideMaterial(i) is ShaderMaterial sm && ShipSkin.IsHull(sm.Shader))
                {
                    if (!_mats.Contains(sm)) _mats.Add(sm);
                    Sample(mi, i);
                }
        });
        _centre = _skin.Count > 0 ? _skin.Aggregate(Vector3.Zero, (a, s) => a + s.P) / _skin.Count : Vector3.Zero;
        foreach (JsonElement b in brk.GetProperty("bodies").EnumerateArray())
            if (Find(b.GetProperty("node").GetString()!) is { } node)
                _bodies.Add(new Body { Node = node, Def = b, Seeds = b.GetProperty("seeds").EnumerateArray().Select(V).ToArray() });
        foreach (JsonElement p in brk.GetProperty("pieces").EnumerateArray())
            if (Find(p.GetProperty("node").GetString()!) is { } node)
            {
                var piece = new Piece { Node = node, Centre = V(p.GetProperty("center")) };
                Walk(node, n => { if (n is MeshInstance3D mi && mi.Mesh != null) for (int i = 0; i < mi.Mesh.GetSurfaceCount(); i++) if (mi.GetSurfaceOverrideMaterial(i) is ShaderMaterial sm && ShipSkin.IsHull(sm.Shader) && !piece.Mats.Contains(sm)) piece.Mats.Add(sm); });
                _parts.Add(new Part { P = piece, Def = p });
            }
    }

    // ---- A hit -------------------------------------------------------------------------------------------------------

    /// <summary>A hit has landed on this ship: its effect, its scar, and maybe a part blown off.</summary>
    public void OnHit(HitEvent e)
    {
        if (Broken || _skin.Count == 0) return;
        // the shooter's direction, in mockup ship space
        Vector3 from = new Vector3((float)(e.FromX / BattleView.MPerU), 0, (float)(e.FromZ / BattleView.MPerU)) - _ship.GlobalPosition;
        Vector3 dirG = (_ship.GlobalTransform.Basis.Inverse() * from).Normalized();
        Vector3 dir = Turn(dirG);
        var (p, n) = Struck(dir);
        Vector3 at = World(p), outward = (World(p + n) - at).Normalized();
        float s = Math.Max(0.6f, _lenM / 150f);
        if (e.HullDamage <= 0)
        {
            Fx.ShieldFlare(_fx, World(p + dir * (6f + 0.12f * _lenM)), Math.Max(0.04f, 0.05f * _lenU), (World(p + dir) - at).Normalized());
            return;
        }
        bool beam = e.Kind == "beam", big = e.Kind == "torpedo";
        Fx.Fireball(_fx, at, Math.Max(0.04f, (beam ? 0.03f : big ? 0.07f : 0.05f) * _lenU), outward);
        Fx.Flash(_fx, at + outward * 0.02f, 0.9f * _lenU, beam ? 1.5f : 3f, beam ? 0.25f : 0.45f);
        Scorch(p, beam ? 1.6f : big ? 5f : 3.2f, beam ? 0f : 1f);
        if (big) Hole(p, n, (2.6f + (float)_rng.NextDouble() * 1.8f) * s);
        if (e.Destroyed) { StartBreach(); return; }
        Wear(p, e.HullDamage);
    }

    // Where a shot from dir lands: the point the damage range aimed it at, if one waits for a shot from this side, else
    // one of the points of plating that face it most.
    private (Vector3 P, Vector3 N) Struck(Vector3 dir)
    {
        if (_aims.Count > 0) { var a = _aims[0]; _aims.RemoveAt(0); return (a.P, a.N); }
        var scored = _skin.Select(k => (k, Sc: (k.P - _centre).Dot(dir) + 4f * k.N.Dot(dir))).OrderByDescending(x => x.Sc).ToList();
        int top = Math.Max(1, scored.Count / 12);
        var pick = scored[_rng.Next(top)].k;
        return pick;
    }

    // ---- The damage range's aim --------------------------------------------------------------------------------------

    // the points the range's clicks aimed at, in order, each with the seconds it has waited (a shot that never lands, as
    // one fired into raised shields, lets its aim go after AimWaitS)
    private readonly List<(Vector3 P, Vector3 N, float Age)> _aims = new();
    private const float AimWaitS = 4f;

    /// <summary>The point of plating under a screen position (the damage range's click to fire): the sampled plating point
    /// nearest it on screen, of those facing the camera, within 40 px; null when the click missed the ship. Global space.</summary>
    public Vector3? PlatingAt(Camera3D cam, Vector2 screen)
    {
        if (Broken) return null;
        float best = 40;
        Vector3? hit = null;
        foreach (var (p, n) in _skin)
        {
            Vector3 at = World(p);
            if (cam.IsPositionBehind(at) || (World(p + n) - at).Dot(cam.GlobalPosition - at) <= 0) continue;
            float d = cam.UnprojectPosition(at).DistanceTo(screen);
            if (d < best) { best = d; hit = at; }
        }
        return hit;
    }

    /// <summary>The next shot from that side lands at this point of plating (global space; the nearest sampled point).</summary>
    public void Aim(Vector3 global)
    {
        Vector3 local = Turn(_ship.ToLocal(global) + _c);
        var pick = _skin.OrderBy(k => k.P.DistanceSquaredTo(local)).FirstOrDefault();
        if (_skin.Count > 0) _aims.Add((pick.P, pick.N, 0));
    }

    private void Scorch(Vector3 p, float r, float heat)
    {
        int i = _scorchN++ % MaxScars;
        _scorch[i] = new Vector4(p.X, p.Y, p.Z, r * _lenM / 150f + 1.2f);
        _heat[i] = heat;
        Upload();
    }

    private void Hole(Vector3 p, Vector3 n, float r)
    {
        if (_holeN0 >= MaxScars) return;
        _holes[_holeN0] = new Vector4(p.X, p.Y, p.Z, r);
        _holeN[_holeN0] = new Vector4(n.X, n.Y, n.Z, 0);
        _holeN0++;
        Upload();
    }

    private void Upload(IEnumerable<ShaderMaterial>? only = null)
    {
        foreach (ShaderMaterial m in only ?? _mats)
        {
            m.SetShaderParameter("scorch", _scorch);
            m.SetShaderParameter("scorch_heat", _heat);
            m.SetShaderParameter("scorch_count", Math.Min(MaxScars, _scorchN));
            m.SetShaderParameter("holes", _holes);
            m.SetShaderParameter("hole_n", _holeN);
            m.SetShaderParameter("hole_count", _holeN0);
        }
    }

    // A part that has taken enough on a damaged hull comes off: an outer cell of a body, or a pylon, nacelle or command block.
    private void Wear(Vector3 p, double dmg)
    {
        Piece? near = null; float best = float.MaxValue; Body? nb = null;
        foreach (Body b in _bodies)
            for (int i = 0; i < b.Seeds.Length; i++) { float d = b.Seeds[i].DistanceTo(p); if (d < best) { best = d; near = null; nb = b; _cellIdx = i; } }
        foreach (Part pt in _parts) { if (pt.P.Off) continue; float d = pt.P.Centre.DistanceTo(p); if (d < best) { best = d; near = pt.P; nb = null; } }
        if (nb != null)
        {
            Swap(nb);
            near = nb.Cells[_cellIdx];
            if (near.Off) return;
            if (near.Centre.DistanceTo(_core) < _lenM * 0.18f) return;   // the core's own cell holds until the breach
        }
        if (near == null) return;
        near.Hit += dmg;
        if (_sim.Hull / _sim.Class.HullHp < ChunkBelowHull && near.Hit >= ChunkHpShare * _sim.Class.HullHp) BlowOff(near, 0.7f);
    }
    private int _cellIdx;

    // ---- Coming apart ------------------------------------------------------------------------------------------------

    // The body is swapped for its cells the first time it starts to come apart; the cells sit where the body was.
    private void Swap(Body b)
    {
        if (b.Swapped) return;
        b.Swapped = true;
        var centres = b.Def.GetProperty("centers").EnumerateArray().Select(V).ToArray();
        Node3D parent = (Node3D)b.Node.GetParent();
        float[] open = new float[16];
        for (int i = 0; i < b.Seeds.Length; i++)
        {
            Vector3 g = Game(b.Seeds[i]);
            var pivot = new Node3D { Name = b.Node.Name + "_cell" + i, Position = g };
            parent.AddChild(pivot);
            var copy = (Node3D)b.Node.Duplicate();
            copy.Position = b.Node.Position - g;
            copy.Visible = true;
            pivot.AddChild(copy);
            var cell = new Piece { Node = pivot, Centre = centres.Length > i ? centres[i] : b.Seeds[i] };
            int ci = i;
            // a hull surface keeps only this cell's skin; anything else (glows, windows, lamps) stays with the cell it is nearest
            Walk(copy, n =>
            {
                if (n is MeshInstance3D mi && mi.Mesh != null)
                {
                    bool hull = false;
                    for (int k = 0; k < mi.Mesh.GetSurfaceCount(); k++)
                        if (mi.GetSurfaceOverrideMaterial(k) is ShaderMaterial sm && _mats.Contains(sm))
                        {
                            var dup = (ShaderMaterial)sm.Duplicate();
                            dup.SetShaderParameter("cell", ci);
                            dup.SetShaderParameter("seeds", Pad(b.Seeds));
                            dup.SetShaderParameter("seed_n", b.Seeds.Length);
                            dup.SetShaderParameter("open_cells", open);
                            mi.SetSurfaceOverrideMaterial(k, dup);
                            cell.Mats.Add(dup);
                            hull = true;
                        }
                    if (!hull) KeepCell(mi, b.Seeds, ci);
                }
                else if (n is Node3D e && e.Name.ToString().StartsWith("light_"))
                {
                    bool strobe = e.Name.ToString().StartsWith("light_strobe");
                    if (strobe || NearestSeed(b.Seeds, Mock(e, Vector3.Zero)) != ci) e.Visible = false;
                }
            });
            foreach (JsonElement f in b.Def.GetProperty("faces").EnumerateArray())
                if (f.GetProperty("cell").GetInt32() == i) cell.Faces.Add(Face(copy, b, f));
            _mats.AddRange(cell.Mats);
            b.Cells.Add(cell);
        }
        Upload(b.Cells.SelectMany(k => k.Mats));
        b.Node.Visible = false;
    }

    // A detail mesh of a breaking body (windows, markings, glows, trim) keeps only its triangles nearest this cell's seed,
    // as the mockup's subsetGeo (docs/mockups/lib/ship-break.js) cuts every mesh of a body: kept whole, a row of windows
    // stayed in one cell and hung across the gap where its neighbours had blown away (owner, 2026-11-05: "what are these
    // dashed lines?"). The hull's own surfaces are clipped by its shader instead. Done once, when the body first breaks.
    private void KeepCell(MeshInstance3D mi, Vector3[] seeds, int cell)
    {
        Mesh src = mi.Mesh;
        var cut = new ArrayMesh();
        for (int k = 0; k < src.GetSurfaceCount(); k++)
        {
            var arr = src.SurfaceGetArrays(k);
            Vector3[] vs = arr[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            int[] idx = arr[(int)Mesh.ArrayType.Index].VariantType == Variant.Type.Nil ? Enumerable.Range(0, vs.Length).ToArray() : arr[(int)Mesh.ArrayType.Index].AsInt32Array();
            var keep = new List<int>();
            for (int t = 0; t + 2 < idx.Length; t += 3)
            {
                Vector3 c = (vs[idx[t]] + vs[idx[t + 1]] + vs[idx[t + 2]]) / 3;
                if (NearestSeed(seeds, Mock(mi, c)) == cell) keep.AddRange(new[] { idx[t], idx[t + 1], idx[t + 2] });
            }
            if (keep.Count == 0) continue;
            arr[(int)Mesh.ArrayType.Index] = keep.ToArray();
            cut.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arr);
            int s = cut.GetSurfaceCount() - 1;
            cut.SurfaceSetMaterial(s, mi.GetSurfaceOverrideMaterial(k) ?? src.SurfaceGetMaterial(k));
        }
        if (cut.GetSurfaceCount() == 0) { mi.Visible = false; return; }
        mi.Mesh = cut;
        for (int s = 0; s < cut.GetSurfaceCount(); s++) mi.SetSurfaceOverrideMaterial(s, null);
    }

    // A cut face: the quad the exporter wrote, in the copy's frame, closing cell a toward cell b; shown once either has gone.
    private Node3D Face(Node3D copy, Body b, JsonElement f)
    {
        _cutShader ??= GD.Load<Shader>("res://src/Game/View/cutface.gdshader");
        Vector3 place = PlaceInShip(copy) ;
        var corners = f.GetProperty("corners").EnumerateArray().Select(V).Select(m => Game(m) - place).ToArray();
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = corners;
        arrays[(int)Mesh.ArrayType.Index] = corners.Length == 4 ? new[] { 0, 2, 1, 2, 3, 1 } : Enumerable.Range(0, corners.Length).ToArray();
        var am = new ArrayMesh();
        am.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        var m = new ShaderMaterial { Shader = _cutShader };
        Vector3 at = Turn(place + _c);
        m.SetShaderParameter("mockup_offset", at);
        m.SetShaderParameter("seeds", Pad(b.Seeds));
        m.SetShaderParameter("seed_n", b.Seeds.Length);
        m.SetShaderParameter("cell_a", f.GetProperty("cell").GetInt32());
        m.SetShaderParameter("cell_b", f.GetProperty("other").GetInt32());
        if (b.Def.TryGetProperty("sdf", out JsonElement sdf))   // a grown hull: its field is the inside test (cutface.gdshader body_d)
        {
            m.SetShaderParameter("use_sdf", true);
            m.SetShaderParameter("sdf_tex", SdfTexture(sdf));
            m.SetShaderParameter("sdf_lo", V(sdf.GetProperty("lo")));
            m.SetShaderParameter("sdf_hi", V(sdf.GetProperty("hi")));
            m.SetShaderParameter("sdf_scale", F(sdf.GetProperty("scale")));
        }
        JsonElement st = b.Def.GetProperty("stations"), ch = b.Def.GetProperty("ch");
        var stv = new Vector4[48]; var chv = new float[48];
        int n = Math.Min(48, st.GetArrayLength());
        for (int i = 0; i < n; i++) { JsonElement r = st[i]; stv[i] = new Vector4(F(r[0]), F(r[1]), F(r[2]), F(r[3])); chv[i] = F(ch[i]); }
        m.SetShaderParameter("st", stv);
        m.SetShaderParameter("ch", chv);
        m.SetShaderParameter("st_n", n);
        m.SetShaderParameter("ox", F(b.Def.GetProperty("ox")));
        m.SetShaderParameter("across", V(f.GetProperty("across")));
        m.SetShaderParameter("depth_dir", V(f.GetProperty("depth")));
        m.SetShaderParameter("mid", V(f.GetProperty("mid")));
        m.SetShaderParameter("heat", 1f);
        var mi = new MeshInstance3D { Name = "cut_" + f.GetProperty("other").GetInt32(), Mesh = am, MaterialOverride = m, Visible = false, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        mi.SetMeta("other", f.GetProperty("other").GetInt32());
        copy.AddChild(mi);
        return mi;
    }

    private static readonly Dictionary<string, ImageTexture3D> _sdfs = new();
    /// <summary>A body's signed distance field as a 3D texture, one R8 image a z slice, from its .bin (the manifest's sdf: file,
    /// dims; tools/blender/hull_sdf.py), loaded once.</summary>
    private static ImageTexture3D SdfTexture(JsonElement sdf)
    {
        string file = sdf.GetProperty("file").GetString()!;
        if (_sdfs.TryGetValue(file, out ImageTexture3D? t)) return t;
        byte[] bytes = FileAccess.GetFileAsBytes("res://assets/ships/" + file);
        int nx = sdf.GetProperty("dims")[0].GetInt32(), ny = sdf.GetProperty("dims")[1].GetInt32(), nz = sdf.GetProperty("dims")[2].GetInt32();
        if (bytes.Length != nx * ny * nz) throw new InvalidOperationException($"sdf {file}: {bytes.Length} bytes, the manifest says {nx} x {ny} x {nz}");
        var slices = new Godot.Collections.Array<Image>();
        for (int z = 0; z < nz; z++) slices.Add(Image.CreateFromData(nx, ny, false, Image.Format.R8, bytes[(z * nx * ny)..((z + 1) * nx * ny)]));
        t = new ImageTexture3D();
        t.Create(Image.Format.R8, nx, ny, nz, false, slices);
        return _sdfs[file] = t;
    }

    private void BlowOff(Piece p, float speed)
    {
        if (p.Off) return;
        float s = _lenM / 150f;
        Vector3 out3 = p.Centre - _core; out3.Y *= 1.4f; out3 = out3.LengthSquared() > 1e-4f ? out3.Normalized() : Vector3.Up;
        Vector3 jitter = new Vector3(R(-1, 1), R(-1, 1), R(-1, 1)) * s;
        Detach(p, Turn(out3) * (4 + 5 * (float)_rng.NextDouble()) * s * speed * 3f + jitter, new Vector3(R(-0.25f, 0.25f), R(-0.25f, 0.25f), R(-0.25f, 0.25f)));
        Vector3 w = World(p.Centre);
        Fx.Blast(_fx, w, 0.11f * _lenU, 2f * _lenU, 0.9f);
        Burn(p);
    }

    private void Detach(Piece p, Vector3 vel, Vector3 spin)
    {
        p.Off = true; p.Vel = vel; p.Spin = spin; p.Age = 0;
        if (p.Group == 0) p.Group = ++_groups;
        // a cluster's cells go with it: each torn from the ship, none from the others in its chunk
        foreach (Piece c in p.Members) { c.Group = p.Group; Detach(c, Vector3.Zero, Vector3.Zero); }
        foreach (ShaderMaterial m in p.Mats) m.SetShaderParameter("cut_heat", 1f);
        // the cells round a break open toward it: faces show and edges char only between cells in different chunks
        foreach (Body b in _bodies)
        {
            if (!b.Cells.Contains(p)) continue;
            float[] open = new float[16], group = new float[16];
            for (int k = 0; k < b.Cells.Count; k++) { open[k] = b.Cells[k].Off ? 1 : 0; group[k] = b.Cells[k].Group; }
            for (int ci = 0; ci < b.Cells.Count; ci++)
            {
                Piece c = b.Cells[ci];
                foreach (ShaderMaterial m in c.Mats) { m.SetShaderParameter("open_cells", open); m.SetShaderParameter("cell_group", group); if (!c.Off) m.SetShaderParameter("cut_heat", 1f); }
                foreach (Node3D f in c.Faces)
                {
                    int o = (int)f.GetMeta("other");
                    f.Visible = (c.Off || b.Cells[o].Off) && b.Cells[o].Group != c.Group;
                }
            }
        }
        // a part (pylon, nacelle, command block) is charred at its cut, and so is the stump it leaves
        foreach (Part pt in _parts.Where(q => q.P == p))
        {
            var cuts = pt.Def.GetProperty("cuts").EnumerateArray().Take(4).ToArray();
            var cv = new Vector4[4]; var cc = new Vector4[4];
            for (int i = 0; i < cuts.Length; i++)
            {
                Vector3 n = V(cuts[i].GetProperty("n")), c = V(cuts[i].GetProperty("c"));
                cv[i] = new Vector4(n.X, n.Y, n.Z, F(cuts[i].GetProperty("d")));
                cc[i] = new Vector4(c.X, c.Y, c.Z, F(cuts[i].GetProperty("r")));
            }
            foreach (ShaderMaterial m in p.Mats) { m.SetShaderParameter("cuts", cv); m.SetShaderParameter("cut_c", cc); m.SetShaderParameter("cut_count", cuts.Length); }
        }
        // its lamps go out, and a baked hull's own lights with them (baked_hull.gdshader lit)
        Walk(p.Node, n => { if (n is Light3D l) l.Visible = false; });
        foreach (ShaderMaterial m in p.Mats) if (m.Shader == ShipSkin.BakedHullShader) m.SetShaderParameter("lit", 0f);
    }

    // A broken piece burns: flames from its broken side for about 20 s.
    private void Burn(Piece p)
    {
        float s = Math.Max(0.6f, _lenM / 150f);
        Vector3 local = Game(p.Centre) - PlaceInShip(p.Node);
        // its own flame trail (owner, 2026-11-04: "my ship parts itself, should have that flame trail"); a big piece two
        Fx.Flame(p.Node, local, 0.07f * _lenU, 24f);
        Vector3 off = new Vector3(R(-1, 1), R(-0.4f, 0.4f), R(-1, 1)) * (0.08f * _lenM);
        if (_bodies.Any(b => b.Cells.Contains(p)) || _lenM > 120) Fx.Flame(p.Node, local + off, 0.05f * _lenU, 18f);
    }

    private void StartBreach()
    {
        if (_breachT >= 0) return;
        _breachT = 0;
        foreach (Body b in _bodies) { Swap(b); Cluster(b); }
        var early = _bodies.Count > 0 ? Chunks(_bodies[0]).OrderByDescending(c => c.Centre.DistanceTo(_core)).ToList() : new List<Piece>();
        var pick = early.Take((int)Math.Ceiling(early.Count * BreachShare)).Concat(_parts.Select(pt => pt.P).Where(q => !q.Off)).ToList();
        for (int i = 0; i < pick.Count; i++)
        {
            Piece q = pick[i];
            float at = BreachFirstS + (BreachLastS - BreachFirstS) * (pick.Count > 1 ? i / (float)(pick.Count - 1) : 0) + R(-0.15f, 0.15f);
            _breach.Add((at, () => BlowOff(q, 1f)));
        }
        _breach.Sort((a, b) => a.At.CompareTo(b.At));
        Fx.Fireball(_fx, World(_core), 0.1f * _lenU, Vector3.Up);
    }

    // The hull's cells still on the ship grouped into chunks of one to three neighbours (ChunkSizes), each chunk of more
    // than one a node its cells are moved under, so the chunk flies and tumbles as one.
    private void Cluster(Body b)
    {
        var next = new Dictionary<int, HashSet<int>>();
        foreach (JsonElement f in b.Def.GetProperty("faces").EnumerateArray())
        {
            int a = f.GetProperty("cell").GetInt32(), o = f.GetProperty("other").GetInt32();
            if (!next.ContainsKey(a)) next[a] = new HashSet<int>();
            next[a].Add(o);
        }
        var left = Enumerable.Range(0, b.Cells.Count).Where(i => !b.Cells[i].Off).OrderBy(_ => _rng.Next()).ToList();
        var taken = new HashSet<int>();
        foreach (int start in left)
        {
            if (taken.Contains(start)) continue;
            float roll = (float)_rng.NextDouble(), acc = 0; int want = 1;
            foreach (var (n, w) in ChunkSizes) { acc += w; if (roll <= acc) { want = n; break; } }
            if (_clusters.Count == 0 && b == _bodies[0]) want = Math.Max(3, (int)Math.Round(left.Count * HulkShare));   // the hulk
            var group = new List<int> { start }; taken.Add(start);
            while (group.Count < want)
            {
                int add = group.SelectMany(g => next.TryGetValue(g, out var ns) ? ns : new HashSet<int>()).FirstOrDefault(k => !taken.Contains(k) && !b.Cells[k].Off, -1);
                if (add < 0) break;
                group.Add(add); taken.Add(add);
            }
            if (group.Count < 2) continue;
            var cells = group.Select(i => b.Cells[i]).ToList();
            Vector3 centre = cells.Aggregate(Vector3.Zero, (sum, c) => sum + c.Centre) / cells.Count;
            var node = new Node3D { Name = b.Node.Name + "_chunk" + _clusters.Count, Position = Game(centre) };
            b.Node.GetParent().AddChild(node);
            foreach (Piece c in cells) c.Node.Reparent(node, true);
            var chunk = new Piece { Node = node, Centre = centre };
            chunk.Members.AddRange(cells);
            _clusters.Add(chunk);
        }
    }

    // What of a body can still come off: its chunks, and its cells in no chunk.
    private IEnumerable<Piece> Chunks(Body b) =>
        _clusters.Where(k => !k.Off && k.Members.All(b.Cells.Contains)).Concat(b.Cells.Where(c => !c.Off && !_clusters.Any(k => k.Members.Contains(c))));

    private void MainBlast()
    {
        Broken = true;
        float s = _lenM / 150f;
        foreach (Piece p in _bodies.SelectMany(Chunks).Concat(_parts.Select(pt => pt.P)).ToList())
        {
            if (p.Off) continue;
            Vector3 out3 = p.Centre - _core; out3.Y *= 1.6f; float d = Math.Max(1f, out3.Length()); out3 = out3.Normalized();
            // half the mockup's pace times 2.5 (owner, 2026-11-04: "the whole explosion 2x slower")
            float sp = (6 + 10 * (float)_rng.NextDouble()) * s * (1 + 40 / (d + 30)) * 1.25f / MathF.Sqrt(Math.Max(1, p.Members.Count));   // big pieces drift, small ones fly
            Detach(p, Turn(out3) * sp + new Vector3(R(-1, 1), R(-1, 1), R(-1, 1)) * s, new Vector3(R(-0.18f, 0.18f), R(-0.18f, 0.18f), R(-0.18f, 0.18f)) / MathF.Sqrt(Math.Max(1, p.Members.Count)));
            Burn(p);
        }
        Vector3 w = World(_core);
        // twice the first size (owner, 2026-11-04: "2x nova side"), growing slower than the ship so a dreadnought's does not
        // white out the whole view
        Fx.Nova(_fx, w, 1.8f * Mathf.Pow(_lenU, 0.7f));
    }

    /// <summary>Steps the damage on by dt seconds of battle time: embers cool, the breach runs, broken pieces drift and tumble.</summary>
    public void Step(float dt)
    {
        if (dt <= 0) return;
        for (int i = _aims.Count - 1; i >= 0; i--) { var a = _aims[i]; if ((a.Age += dt) > AimWaitS) _aims.RemoveAt(i); else _aims[i] = a; }
        bool cooled = false;
        for (int i = 0; i < MaxScars; i++) if (_heat[i] > 0) { _heat[i] = Math.Max(0, _heat[i] - dt / ScorchCoolS); cooled = true; }
        if (cooled) foreach (ShaderMaterial m in _mats) m.SetShaderParameter("scorch_heat", _heat);
        if (_breachT >= 0 && !Broken)
        {
            _breachT += dt;
            while (_breach.Count > 0 && _breach[0].At <= _breachT) { var e = _breach[0]; _breach.RemoveAt(0); e.Do(); }
            if (_rng.NextDouble() < dt * 3)
            {
                var live = _bodies.SelectMany(b => b.Cells).Where(c => !c.Off).ToList();
                if (live.Count > 0) Fx.Fireball(_fx, World(live[_rng.Next(live.Count)].Centre), 0.06f * _lenU, Vector3.Up);
            }
            if (_breachT >= BreachMainS) MainBlast();
        }
        foreach (Piece p in _bodies.SelectMany(b => b.Cells).Concat(_parts.Select(pt => pt.P)).Concat(_clusters))
        {
            if (!p.Off) continue;
            p.Age += dt;
            p.Node.Position += p.Vel * dt;
            if (p.Spin.LengthSquared() > 0) p.Node.Rotate(p.Spin.Normalized(), p.Spin.Length() * dt);
            float heat = Math.Max(0, 1 - p.Age / EdgeCoolS);
            foreach (ShaderMaterial m in p.Mats) m.SetShaderParameter("cut_heat", heat);
            foreach (Node3D f in p.Faces) if (f is MeshInstance3D mi && mi.MaterialOverride is ShaderMaterial fm) fm.SetShaderParameter("heat", heat);
        }
    }

    // ---- Frames ------------------------------------------------------------------------------------------------------

    // mockup ship space (+X port, +Z bow) to the glb's frame (+X starboard, -Z bow, re-centred), and back
    private Vector3 Game(Vector3 m) => Turn(m) - _c;
    private static Vector3 Turn(Vector3 v) => new(-v.X, v.Y, -v.Z);
    private Vector3 World(Vector3 m) => _ship.ToGlobal(Game(m));
    private Vector3 Mock(Node3D n, Vector3 local) { Transform3D xf = Rel(n); return Turn(xf * local + _c); }

    private Transform3D Rel(Node3D n)
    {
        Transform3D xf = Transform3D.Identity;
        for (Node? p = n; p != null && p != _ship; p = p.GetParent()) if (p is Node3D p3) xf = p3.Transform * xf;
        return xf;
    }
    private Vector3 PlaceInShip(Node3D n) => Rel(n).Origin;

    // Points on the plating of a hull surface, every few vertices, with their normals, in mockup ship space.
    private void Sample(MeshInstance3D mi, int surface)
    {
        var arr = mi.Mesh.SurfaceGetArrays(surface);
        var vs = arr[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var ns = arr[(int)Mesh.ArrayType.Normal].AsVector3Array();
        Transform3D xf = Rel(mi);
        int step = Math.Max(1, vs.Length / 300);
        for (int i = 0; i < vs.Length; i += step)
            _skin.Add((Turn(xf * vs[i] + _c), ns.Length > i ? Turn((xf.Basis * ns[i]).Normalized()) : Vector3.Up));
    }

    private Node3D? Find(string name) => _ship.FindChild(name, true, false) as Node3D;
    private static int NearestSeed(Vector3[] seeds, Vector3 p) { int best = 0; float d = float.MaxValue; for (int i = 0; i < seeds.Length; i++) { float k = seeds[i].DistanceTo(p); if (k < d) { d = k; best = i; } } return best; }
    private static Vector3[] Pad(Vector3[] s) { var o = new Vector3[16]; Array.Copy(s, o, Math.Min(16, s.Length)); return o; }
    private float R(float a, float b) => a + (b - a) * (float)_rng.NextDouble();
    private static float F(JsonElement e) => (float)e.GetDouble();
    private static Vector3 V(JsonElement e) => new(F(e[0]), F(e[1]), F(e[2]));
    private static void Walk(Node n, Action<Node> f) { f(n); foreach (Node c in n.GetChildren()) Walk(c, f); }
}
