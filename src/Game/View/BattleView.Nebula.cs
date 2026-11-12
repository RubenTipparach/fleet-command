// BattleView.Nebula: a battle's nebula plumes and ion storms in 3D (openspec/changes/nebula-plumes; owner, 2026-11-10:
// "write up some shaders for nebula too", "entering a column of nebula plume can hide you from enemies", "some nebulas are
// ion storms, which will have cool lightning", "which can strike ships, and cause a temprorary shut down of the reactor
// core, the ship loosing power for a few seconds completely", "make a cloud mesh for top and bottom of nebula and fade out
// the edges, so it looks like the nebula columns are connected to a larger structure above and below the map"). Each plume
// of the scenario (Rules.Nebulae, the list the rules test) is a NebulaPlume, the one builder, in its look, a column rising
// between the two decks of a NebulaDeck. An ion storm has lightning of two kinds:
// - the cloud's own flicker, the view's: bolts between two points of the storm's head and sheet lightning inside it, as
//   often as the look says (plumes.json flashes_per_min), drawn from the view's own fork of the scenario's seed and
//   timed on the battle's clock, so it stops with a pause and the simulation never hears of it;
// - the strikes that trip a core, the simulation's (Battle.Lightning): each one a heavier bolt from the storm's head down
//   onto its ship, and while the ship's core is down (Ship.CoreDown) its lamps and their glows are dark.
// A bolt is a jagged ribbon with forks, facing the camera, bright past the glow's threshold so it blooms; its flash lights
// the cloud round it through the plume's shader (NebulaPlume.Sync). The view only reads the battle.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class BattleView
{
    /// <summary>How long a cloud bolt and a strike on a ship last, battle seconds, and how bright their flash is in the
    /// cloud (the plume shader's flash strength) at its peak.</summary>
    public const float BoltLifeS = 0.42f, StrikeLifeS = 0.7f, StrikeFlash = 6f;
    /// <summary>A bolt's least width (the scene's units) and its width as a share of its distance from the camera, so a
    /// bolt across the battle is still a line and not a hair.</summary>
    public const float BoltWidthU = 0.045f, BoltWidthK = 0.0016f;

    private sealed class Bolt
    {
        public required NebulaPlume Plume;
        public MeshInstance3D? Node;          // none for sheet lightning: a flash inside the cloud, no bolt to see
        public StandardMaterial3D? Mat;
        public required Vector3 At;           // the flash's place, in the plume's units
        public required double Born;          // battle seconds
        public required float LifeS, Strength;
    }

    private Battle? _nebulaFor;
    private Node3D? _nebula;
    private readonly List<NebulaPlume> _plumes = new();
    private NebulaDeck? _decks;
    private NebulaPuffs? _puffs;
    private NebulaShells? _shells;
    private readonly List<Bolt> _bolts = new();
    private readonly HashSet<LightningHit> _seenHits = new();
    private double[] _nextFlashS = Array.Empty<double>();
    private Rng _boltRng = new(0);
    private readonly Dictionary<string, List<Node3D>> _lamps = new();
    private readonly HashSet<string> _dark = new();

    // A new battle: its plumes, each in its look and cut from its own place in the noise.
    private void BuildNebulae(Battle b)
    {
        _nebulaFor = b;
        _nebula?.QueueFree();
        _plumes.Clear(); _bolts.Clear(); _seenHits.Clear(); _lamps.Clear(); _dark.Clear();
        _nebula = new Node3D { Name = "Nebulae" };
        _env.FogEnabled = false;
        AddChild(_nebula);
        Rng looks = Rng.Fork(b.Scenario.Seed, "plume-look");
        _boltRng = Rng.Fork(b.Scenario.Seed, "plume-lightning");
        Vector3 toSun = _sun.GlobalTransform.Basis.Z.Normalized();
        foreach (NebulaDef n in b.Rules.Nebulae)
        {
            NebulaPlume p = NebulaPlume.Build(n, new Vector3((float)looks.Next(), (float)looks.Next(), (float)looks.Next()), MPerU, toSun);
            _nebula.AddChild(p.Node);
            _plumes.Add(p);
        }
        _nextFlashS = _plumes.Select(p => b.T + FlashWait(p)).ToArray();
        // the decks the columns rise between, cut from their own place in the noise; or the whole nebula as puffs
        _decks = null;
        _puffs = null;
        _shells = null;
        if (_plumes.Count == 0) return;
        var deckSeed = new Vector3((float)looks.Next(), (float)looks.Next(), (float)looks.Next());
        string style = DeckLook.Get().Style;
        var field = new DeckField(_plumes, b.Planets.Select(p => (p.X, p.Z, p.RadiusM)), b.Scenario.Seed, MPerU);
        if (style == "volume")
        {
            _decks = NebulaDeck.Build(field, deckSeed);
            _nebula.AddChild(_decks.Node);
        }
        else if (style == "none")
            foreach (NebulaPlume p in _plumes) p.Node.Visible = false;
        else if (style == "shells")
        {
            // the plumes stay for the lightning's places, unseen
            foreach (NebulaPlume p in _plumes) p.Node.Visible = false;
            _shells = NebulaShells.Build(_plumes, field, b.Scenario.Seed);
            _nebula.AddChild(_shells.Node);
        }
        else
        {
            // the plumes stay for the lightning's places, unseen
            foreach (NebulaPlume p in _plumes) p.Node.Visible = false;
            _puffs = NebulaPuffs.Build(_plumes, field, b.Scenario.Seed, style == "billboards");
            _nebula.AddChild(_puffs.Node);
        }
        // the haze of the nebula the battle is fought in: the distance fades into it, in the plumes' own colour
        (Color fog, float density) = NebulaDeck.Fog(_plumes, MPerU);
        _env.FogEnabled = true;
        _env.FogMode = Godot.Environment.FogModeEnum.Exponential;
        _env.FogLightColor = fog;
        _env.FogDensity = density;
        _env.FogSkyAffect = DeckLook.Get().FogSky;
        _env.FogSunScatter = 0;
        _env.FogAerialPerspective = 0;
        foreach (NebulaPlume p in _plumes) p.SetFog(fog, density);
        _decks?.SetFog(fog, density);
    }

    // where a storm's lightning plays, in its radii: the column's head, below the deck it runs into (a narrow column is many
    // radii tall, and a bolt up in the deck would be out of sight)
    private static float StormHead(NebulaPlume p) => Math.Min(p.HeadR * 0.9f, 2.4f);

    /// <summary>Every storm of the battle flickers now (the still tool's: a still's battle stands still, so it would wait
    /// for no flicker of its own).</summary>
    public void Flicker()
    {
        if (_nebulaFor == null) return;
        foreach (NebulaPlume p in _plumes.Where(x => x.Def.Storm)) CloudLightning(p, _nebulaFor.T);
    }

    // seconds to a storm's next flicker: its look's rate, the gaps uneven as real lightning's are
    private double FlashWait(NebulaPlume p) =>
        p.Def.Storm && p.Look.FlashesPerMin > 0 ? -Math.Log(1 - _boltRng.Next()) * 60 / p.Look.FlashesPerMin : double.PositiveInfinity;

    // Each frame: the storms' flicker, a bolt for each new strike, every bolt's fade, each plume's sun and flashes, and
    // the lamps of the ships whose cores are down.
    private void SyncNebulae(Battle b)
    {
        if (_nebulaFor != b) BuildNebulae(b);
        for (int i = 0; i < _plumes.Count; i++)
        {
            // a frame is never more than a few flickers behind (a paused or hidden window comes back to one, not a barrage)
            for (int k = 0; k < 3 && b.T >= _nextFlashS[i]; k++) { CloudLightning(_plumes[i], _nextFlashS[i]); _nextFlashS[i] += FlashWait(_plumes[i]); }
            if (b.T >= _nextFlashS[i]) _nextFlashS[i] = b.T + FlashWait(_plumes[i]);
        }
        foreach (LightningHit h in b.Lightning)
            if (_seenHits.Add(h)) StrikeBolt(h);
        _seenHits.RemoveWhere(h => !b.Lightning.Contains(h));

        for (int i = _bolts.Count - 1; i >= 0; i--)
        {
            Bolt bolt = _bolts[i];
            double age = b.T - bolt.Born;
            if (age < 0 || age > bolt.LifeS) { bolt.Node?.QueueFree(); _bolts.RemoveAt(i); continue; }
            float e = Restrike(age);
            if (bolt.Mat != null) bolt.Mat.AlbedoColor = new Color(2.6f * e, 2.6f * e, 2.6f * e, Math.Min(1f, e * 1.5f));
        }
        Vector3 sun = _sun.GlobalTransform.Basis.Z.Normalized();
        Vector3 cam = _cam.GlobalPosition;
        foreach (NebulaPlume p in _plumes)
        {
            // drawn far to near by the nearest side of each column, not its middle: two wide columns one behind the other
            // overlap on the screen, and the farther drawn over the nearer shows as a hard seam
            Vector3 at = p.Node.GlobalPosition;
            float nearest = Math.Max(new Vector2(cam.X - at.X, cam.Z - at.Z).Length() - p.RadiusU, 0);
            p.Node.SortingOffset = cam.DistanceTo(at) - nearest;
            var flashes = new Vector4[NebulaPlume.Flashes];
            int n = 0;
            foreach (Bolt bolt in _bolts.Where(x => x.Plume == p).OrderByDescending(x => x.Strength * Restrike(b.T - x.Born)).Take(NebulaPlume.Flashes))
                flashes[n++] = new Vector4(bolt.At.X, bolt.At.Y, bolt.At.Z, bolt.Strength * Restrike(b.T - bolt.Born));
            p.Sync(sun, _sun.LightEnergy, b.T, flashes);
        }
        _decks?.Sync(sun, _sun.LightEnergy, b.T);
        _puffs?.Sync(sun, _sun.LightEnergy, b.T, cam);
        _shells?.Sync(sun, _sun.LightEnergy);
        foreach (Ship s in b.Ships)
        {
            bool down = s.Alive && s.CoreDown;
            if (!down && !_dark.Contains(s.Id)) continue;
            if (!_models.TryGetValue(s.Id, out Node3D? m)) continue;
            if (!_lamps.TryGetValue(s.Id, out List<Node3D>? lamps)) { lamps = new List<Node3D>(); Lamps(m, lamps); _lamps[s.Id] = lamps; }
            // after ShipSkin.Flash in the frame, so a strobe stays dark too; lit again, the strobes take up their own flash
            foreach (Node3D l in lamps) if (IsInstanceValid(l)) l.Visible = !down;
            if (down) _dark.Add(s.Id); else _dark.Remove(s.Id);
        }
    }

    // a model's lamps and their glows (ShipSkin names them)
    private static void Lamps(Node n, List<Node3D> into)
    {
        foreach (Node c in n.GetChildren())
        {
            if (c is Node3D l && (c.Name == "lamp" || c.Name == "glow")) into.Add(l);
            Lamps(c, into);
        }
    }

    /// <summary>How bright a bolt is at an age in seconds, 0 to 1: the first stroke and two fainter return strokes, each
    /// dying in a twentieth of a second, as lightning flickers.</summary>
    public static float Restrike(double ageS)
    {
        float a = (float)ageS;
        if (a < 0) return 0;
        float e = Mathf.Exp(-a / 0.05f);
        if (a >= 0.11f) e = Math.Max(e, 0.75f * Mathf.Exp(-(a - 0.11f) / 0.05f));
        if (a >= 0.22f) e = Math.Max(e, 0.5f * Mathf.Exp(-(a - 0.22f) / 0.08f));
        return e;
    }

    private float Sym() => (float)(_boltRng.Next() * 2 - 1);

    // A storm's own flicker: a bolt between two points of its head, or (one time in three) sheet lightning inside it.
    private void CloudLightning(NebulaPlume p, double born)
    {
        float top = StormHead(p), ang = (float)(_boltRng.Next() * Math.Tau), rad = 0.75f * Mathf.Sqrt((float)_boltRng.Next());
        var a = new Vector3(Mathf.Cos(ang) * rad, top * (0.3f + 0.55f * (float)_boltRng.Next()), Mathf.Sin(ang) * rad);
        float strength = 3f + 3f * (float)_boltRng.Next();
        if (_boltRng.Next() < 0.34)
        {
            _bolts.Add(new Bolt { Plume = p, At = a, Born = born, LifeS = BoltLifeS, Strength = strength * 0.8f });
            return;
        }
        // across the head more than up it, and a little down
        Vector3 dir = new Vector3(Sym(), Sym() * 0.45f - 0.2f, Sym()).Normalized();
        Vector3 z = a + dir * (0.35f + 0.45f * (float)_boltRng.Next());
        z.Y = Mathf.Clamp(z.Y, 0.05f, top * 0.9f);
        AddBolt(p, a, z, 1f, strength, BoltLifeS, 2, born);
    }

    // The simulation's strike on a ship: a heavy bolt from the head of the storm it stands in down onto it.
    private void StrikeBolt(LightningHit h)
    {
        NebulaPlume? p = _plumes.Where(x => x.Def.Storm).MinBy(x => Math.Sqrt((x.Def.XM - h.XM) * (x.Def.XM - h.XM) + (x.Def.ZM - h.ZM) * (x.Def.ZM - h.ZM)) / x.Def.RM);
        if (p == null) return;
        float lift = _models.ContainsKey(h.ShipId) ? LiftOf(h.ShipId) : 0;
        Vector3 ship = p.Local(new Vector3((float)(h.XM / MPerU), lift, (float)(h.ZM / MPerU)));
        Vector3 from = new Vector3(ship.X * 0.6f + Sym() * 0.25f, StormHead(p) * (0.6f + 0.25f * (float)_boltRng.Next()), ship.Z * 0.6f + Sym() * 0.25f);
        AddBolt(p, from, ship, 1.7f, StrikeFlash, StrikeLifeS, 4, h.T);
    }

    // A bolt from one point of a plume to another (its own units): a jagged trunk with forks, as a ribbon facing the
    // camera, a wide dim halo in the lightning's colour under a thin white core; its flash is at the far end.
    private void AddBolt(NebulaPlume p, Vector3 fromLocal, Vector3 toLocal, float widthX, float strength, float lifeS, int forks, double born)
    {
        Vector3 a = p.World(fromLocal), z = p.World(toLocal), cam = _cam.GlobalPosition;
        List<Vector3> trunk = Jagged(a, z, 6, 0.16f);
        float w = Math.Max(BoltWidthU, cam.DistanceTo((a + z) / 2) * BoltWidthK) * widthX;
        Color halo = new Color(p.Look.Flash, 0.3f), core = new Color(1f, 1f, 1f, 1f);
        var im = new ImmediateMesh();
        Ribbon(im, trunk, w * 4, halo, cam, 0);
        Ribbon(im, trunk, w, core, cam, 0);
        for (int f = 0; f < forks; f++)
        {
            // a fork leaves the trunk somewhere along its first three quarters and wanders off, mostly onward
            int at = 1 + _boltRng.Below(trunk.Count * 3 / 4);
            Vector3 dir = ((z - a).Normalized() + new Vector3(Sym(), Sym(), Sym()) * 0.9f).Normalized();
            List<Vector3> fork = Jagged(trunk[at], trunk[at] + dir * a.DistanceTo(z) * (0.18f + 0.3f * (float)_boltRng.Next()), 4, 0.2f);
            Ribbon(im, fork, w * 2.4f, halo, cam, 0.8f);
            Ribbon(im, fork, w * 0.6f, core, cam, 0.8f);
        }
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            VertexColorUseAsAlbedo = true, CullMode = BaseMaterial3D.CullModeEnum.Disabled, DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
            DisableReceiveShadows = true, AlbedoColor = new Color(2.6f, 2.6f, 2.6f, 1f),
        };
        var node = new MeshInstance3D { Name = "Bolt", Mesh = im, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        _nebula!.AddChild(node);
        _bolts.Add(new Bolt { Plume = p, Node = node, Mat = mat, At = toLocal, Born = born, LifeS = lifeS, Strength = strength });
    }

    // A jagged line from a to z: the midpoint of every stretch thrown aside, by a share of the whole length that halves
    // each time, so it has big kinks and small ones.
    private List<Vector3> Jagged(Vector3 a, Vector3 z, int levels, float sway)
    {
        var pts = new List<Vector3> { a, z };
        float amp = a.DistanceTo(z) * sway;
        for (int l = 0; l < levels; l++)
        {
            var next = new List<Vector3>(pts.Count * 2);
            for (int i = 0; i < pts.Count - 1; i++)
            {
                next.Add(pts[i]);
                next.Add((pts[i] + pts[i + 1]) / 2 + new Vector3(Sym(), Sym(), Sym()) * amp);
            }
            next.Add(pts[^1]);
            pts = next;
            amp *= 0.52f;
        }
        return pts;
    }

    // One line as a strip of the given width facing the camera; taper thins it toward its far end (0 none, 1 to a point).
    private static void Ribbon(ImmediateMesh im, List<Vector3> pts, float width, Color col, Vector3 cam, float taper)
    {
        im.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip);
        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 along = (pts[Math.Min(i + 1, pts.Count - 1)] - pts[Math.Max(i - 1, 0)]).Normalized();
            Vector3 side = along.Cross(cam - pts[i]).Normalized() * width / 2 * (1 - taper * i / (pts.Count - 1));
            im.SurfaceSetColor(col);
            im.SurfaceAddVertex(pts[i] - side);
            im.SurfaceSetColor(col);
            im.SurfaceAddVertex(pts[i] + side);
        }
        im.SurfaceEnd();
    }
}
