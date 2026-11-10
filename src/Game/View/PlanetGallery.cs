// PlanetGallery: every kind of planet and asteroid drawn alone, a still of each saved (the planet-shaders skill's check;
// run as the game with -- --planet-gallery=<folder>, in a window). Each planet is built by PlanetBody, the battle's own
// builder, in a sun from the left and a little in front, so what the stills show is what a battle draws; then the rock
// kinds three seeds each in a row. Prints GALLERY <file> per still and GALLERY_END, then quits. A tool: the battle never
// opens it.
using System.Threading.Tasks;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class PlanetGallery : Node3D
{
    /// <summary>The folder the stills go into.</summary>
    public required string Out;
    private Camera3D _cam = null!;
    private DirectionalLight3D _sun = null!;

    public override void _Ready() => _ = Run();

    private async Task Run()
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(Out.StartsWith("res://") || Out.StartsWith("user://") ? Out : "res://" + Out));
        var env = new Environment { BackgroundMode = Environment.BGMode.Color, BackgroundColor = new Color("#04060c"), AmbientLightSource = Environment.AmbientSource.Color,
            AmbientLightColor = new Color("#26324a"), AmbientLightEnergy = 0.25f, TonemapMode = Environment.ToneMapper.Filmic };
        AddChild(new WorldEnvironment { Environment = env });
        _sun = new DirectionalLight3D { LightEnergy = 1.2f, LightColor = new Color(1, 0.96f, 0.9f) };
        AddChild(_sun);
        _sun.LookAt(new Vector3(1, -0.2f, -0.45f), Vector3.Up);   // the sun on the left and a little in front: a third of each disc in night
        _cam = new Camera3D { Fov = 35, Current = true };
        AddChild(_cam);
        Vector3 toSun = _sun.GlobalTransform.Basis.Z.Normalized();
        foreach (string kind in FleetCommand.Data.PlanetDef.Kinds)
        {
            PlanetBody p = PlanetBody.Build(kind, kind, PlanetLooks.SeedOf("gallery-" + kind));
            AddChild(p.Root);
            float reach = p.Look.Rings is { } r ? r.Outer : 1.1f;
            Aim(new Vector3(0, reach * 0.55f, reach * 3.1f));
            for (int i = 0; i < 30; i++) { p.Sync(toSun, _sun.LightEnergy, 2); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            Save($"planet_{kind}.png");
            // and up close, low over the limb: the detail as the camera comes down
            Aim(new Vector3(-0.35f, 0.5f, 1.32f), new Vector3(-0.2f, 0.55f, 0.6f));
            for (int i = 0; i < 20; i++) { p.Sync(toSun, _sun.LightEnergy, 0.3f); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            Save($"planet_{kind}_close.png");
            // and as a crescent, the sun round behind: the terminator, where low light picks out the relief
            Aim(new Vector3(0, 0.25f, 2.5f), new Vector3(0.42f, 0.05f, 0));
            Vector3 low = new Vector3(0.9f, 0.22f, -0.38f).Normalized();
            for (int i = 0; i < 20; i++) { p.Sync(low, _sun.LightEnergy, 1.5f); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            Save($"planet_{kind}_crescent.png");
            p.Root.QueueFree();
        }
        _sun.LookAt(_sun.GlobalPosition + new Vector3(0.7f, -0.45f, -0.55f), Vector3.Up);   // the rocks lit from the front left
        var rocks = new Node3D();
        AddChild(rocks);
        int row = 0;
        foreach (string kind in Asteroids.Kinds)
        {
            for (int s = 0; s < 3; s++)
            {
                MeshInstance3D rock = Asteroids.Make(row * 10 + s, kind);
                rock.Position = new Vector3((s - 1) * 2.6f, (1.5f - row) * 2.3f, 0);
                rock.Scale = new Vector3(1, 0.8f, 0.9f);
                rock.RotationDegrees = new Vector3(s * 37, row * 53 + s * 71, s * 19);
                rocks.AddChild(rock);
            }
            row++;
        }
        Aim(new Vector3(0, 0, 13));
        for (int i = 0; i < 20; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Save("asteroids.png");
        GD.Print("GALLERY_END");
        GetTree().Quit();
    }

    private void Aim(Vector3 at, Vector3? look = null)
    {
        _cam.Position = at;
        _cam.LookAt(look ?? Vector3.Zero, Vector3.Up);
    }

    private void Save(string name)
    {
        string path = (Out.StartsWith("res://") || Out.StartsWith("user://") ? Out : "res://" + Out).TrimEnd('/') + "/" + name;
        GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath(path));
        GD.Print($"GALLERY {path}");
    }
}
