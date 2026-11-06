// Prewarm: the loading the game does once at start, behind the loading screen (owner, 2026-11-05: "implement loading
// progress bar if its kinda slow, implement loading if you have shaders you need to prewarm"). It loads every ship glb
// on Godot's loader threads, the schematics and emblems, then draws each ship in its hull shader, every effect shader
// and the map's material kinds once in a small off-screen viewport, so their pipelines are compiled before a battle or
// the map first shows them and nothing hitches the first time it appears. It reports its progress, 0 to 1, with what it is
// doing; the loading screen draws that.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FleetCommand.Data;
using FleetCommand.Game.Ui;
using FleetCommand.Game.View;
using Godot;

namespace FleetCommand.Game.Flow;

public static class Prewarm
{
    /// <summary>Every ship glb the game flies or stands on the map (each class's model, and every exported ship).</summary>
    public static List<string> ShipPaths(GameData data)
    {
        var list = data.Classes.Values.Where(c => c.Model != null).Select(c => "res://" + c.Model).ToList();
        foreach (string fac in new[] { "concord", "krai" })
            foreach (string f in DirAccess.GetFilesAt($"res://assets/ships/{fac}"))
                if (f.EndsWith(".glb")) list.Add($"res://assets/ships/{fac}/{f}");
        return list.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    private static readonly string[] Textures =
    {
        "res://assets/emblems/concord/b-e27-angle.png", "res://assets/emblems/krai/kc-angle.png", "res://assets/emblems/ssarith/sd-angle.png",
    };

    /// <summary>Loads and warms everything; <paramref name="report"/> gets the progress (0 to 1) and the step's name.</summary>
    public static async Task Run(Node host, GameData data, Action<float, string> report)
    {
        SceneTree tree = host.GetTree();
        async Task Frame() => await host.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        // 1. the ships and pictures, on the loader's threads (0 to 0.6)
        List<string> ships = ShipPaths(data);
        var all = ships.Concat(Textures.Where(p => ResourceLoader.Exists(p))).ToList();
        foreach (string p in all) ResourceLoader.LoadThreadedRequest(p);
        var progress = new Godot.Collections.Array();
        while (true)
        {
            float sum = 0;
            int done = 0;
            foreach (string p in all)
            {
                ResourceLoader.ThreadLoadStatus st = ResourceLoader.LoadThreadedGetStatus(p, progress);
                bool fin = st is ResourceLoader.ThreadLoadStatus.Loaded or ResourceLoader.ThreadLoadStatus.Failed or ResourceLoader.ThreadLoadStatus.InvalidResource;
                sum += fin ? 1 : progress.Count > 0 ? (float)progress[0] : 0;
                if (fin) done++;
            }
            report(0.6f * sum / Math.Max(1, all.Count), $"Loading ships and insignia ({done} of {all.Count})");
            if (done == all.Count) break;
            await Frame();
        }
        var scenes = ships.Select(p => ResourceLoader.LoadThreadedGet(p) as PackedScene).Where(s => s != null).Cast<PackedScene>().ToList();
        foreach (string p in Textures.Where(p => ResourceLoader.Exists(p))) ResourceLoader.LoadThreadedGet(p);

        // 2. the schematics (0.6 to 0.7)
        report(0.62f, "Loading ship schematics");
        await Frame();
        Schematics.Has("concord/albatross");
        report(0.7f, "Preparing shaders");
        await Frame();

        // 3. the shaders: everything drawn once, off screen (0.7 to 1); a headless run draws nothing, so there is nothing to
        // compile and no frame would ever be drawn to wait for
        if (DisplayServer.GetName() == "headless") { report(1, "Ready"); return; }
        var vp = new SubViewport { Size = new Vector2I(256, 256), OwnWorld3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, Name = "Prewarm" };
        host.AddChild(vp);
        var sky = new Sky { SkyMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/View/nebula_sky.gdshader") }, RadianceSize = Sky.RadianceSizeEnum.Size128 };
        vp.AddChild(new WorldEnvironment { Environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Sky, Sky = sky, GlowEnabled = true } });
        vp.AddChild(new Camera3D { Position = new Vector3(0, 6, 22), Current = true, Fov = 60 });
        vp.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-50, -30, 0) });
        vp.AddChild(new OmniLight3D { Position = new Vector3(0, 4, 4), OmniRange = 30 });
        int i = 0;
        foreach (PackedScene ps in scenes)
        {
            Node3D ship = ps.Instantiate<Node3D>();
            string path = ps.ResourcePath;
            ship.Scale = Vector3.One * 0.05f;
            ship.Position = new Vector3(-12 + 4 * (i % 7), -3 + 4 * (i / 7), 0);
            try { ShipSkin.Apply(ship, path["res://assets/ships/".Length..], ship.Scale.X); }
            catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException) { /* a ship the manifest does not dress keeps its own materials */ }
            vp.AddChild(ship);
            i++;
        }
        // every effect shader on a quad, and the map's kinds of material
        foreach (string f in DirAccess.GetFilesAt("res://src/Game/View"))
        {
            if (!f.EndsWith(".gdshader") || f == "nebula_sky.gdshader") continue;
            var sh = GD.Load<Shader>("res://src/Game/View/" + f);
            vp.AddChild(new MeshInstance3D { Mesh = new QuadMesh(), MaterialOverride = new ShaderMaterial { Shader = sh }, Position = new Vector3(-12 + 3 * (i % 8), 6, 2) });
            i++;
        }
        foreach (Material m in CampaignMap.MaterialKinds())
        {
            vp.AddChild(new MeshInstance3D { Mesh = new SphereMesh(), MaterialOverride = m, Position = new Vector3(-12 + 3 * (i % 8), -6, 2) });
            i++;
        }
        for (int k = 0; k < 6; k++)
        {
            report(0.7f + 0.3f * (k + 1) / 6, "Preparing shaders");
            await Frame();
        }
        await host.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        vp.QueueFree();
        report(1, "Ready");
    }
}
