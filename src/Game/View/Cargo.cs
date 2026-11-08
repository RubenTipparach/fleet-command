// Cargo: a freighter's containers in the 3D view (mockup 24, the freighters; owner, 2026-11-07: "implement in game"). Every
// freighter carries the same container (assets/containers/container.glb, the low box tools/blender/build_container.py bakes
// its maps onto), so it is one mesh drawn once per container as a MultiMesh under the ship's glb root, each placed, turned,
// scaled and tinted as the page placed it (the ship's manifest entry "cargo", written by tools/mockups/export_ships.mjs from
// the page's own containers). The container's look is its baked maps (diffuse, tangent normals, AO, roughness) on a standard
// material, its colour each container's tint. It hangs beside the hull's pieces, never inside one, added after ShipDamage has
// walked the ship, so the breakup does not copy it; a ship that has come apart shows none (its containers lost with it).
// View only: the simulation never reads it.
using System.Text.Json;
using Godot;

namespace FleetCommand.Game.View;

public sealed class Cargo
{
    private const string Dir = "res://assets/containers/";
    private static Mesh? _mesh;
    private static StandardMaterial3D? _mat;
    private readonly MultiMeshInstance3D _node;

    private Cargo(MultiMeshInstance3D node) { _node = node; }

    /// <summary>How many containers the ship carries.</summary>
    public int Count => _node.Multimesh.InstanceCount;

    /// <summary>The containers of a ship (glb: its glb root, scaled to the battle; file: its path under assets/ships/), or null
    /// for a ship that carries none.</summary>
    public static Cargo? Load(Node3D glb, string file)
    {
        if (!ShipSkin.ShipEntry(file).TryGetProperty("cargo", out JsonElement c)) return null;
        JsonElement items = c.GetProperty("items");
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = Mesh(), InstanceCount = items.GetArrayLength() };
        int i = 0;
        foreach (JsonElement k in items.EnumerateArray())
        {
            JsonElement p = k.GetProperty("p"), q = k.GetProperty("q"), rgb = k.GetProperty("rgb");
            var basis = new Basis(new Quaternion(F(q[0]), F(q[1]), F(q[2]), F(q[3])).Normalized()).Scaled(Vector3.One * F(k.GetProperty("s")));
            mm.SetInstanceTransform(i, new Transform3D(basis, new Vector3(F(p[0]), F(p[1]), F(p[2]))));
            mm.SetInstanceColor(i, new Color(F(rgb[0]), F(rgb[1]), F(rgb[2])));
            i++;
        }
        var node = new MultiMeshInstance3D { Name = "cargo", Multimesh = mm, MaterialOverride = Material() };
        glb.AddChild(node);
        return new Cargo(node);
    }

    /// <summary>Shows the containers on a whole ship and none on one that has come apart.</summary>
    public void Show(bool broken) => _node.Visible = !broken;

    // the container's low box, from its glb
    private static Mesh Mesh()
    {
        if (_mesh != null) return _mesh;
        var scene = GD.Load<PackedScene>(Dir + "container.glb") ?? throw new System.InvalidOperationException($"container not found: {Dir}container.glb");
        Node root = scene.Instantiate();
        MeshInstance3D? mi = Find(root);
        _mesh = mi?.Mesh ?? throw new System.InvalidOperationException($"container has no mesh: {Dir}container.glb");
        root.Free();
        return _mesh;
    }

    // its baked maps on a standard material: the diffuse is grey, so each container's tint (sRGB, as the page's colours) is its paint
    private static StandardMaterial3D Material() => _mat ??= new StandardMaterial3D
    {
        AlbedoTexture = GD.Load<Texture2D>(Dir + "container_diffuse.png"),
        VertexColorUseAsAlbedo = true,
        VertexColorIsSrgb = true,
        NormalEnabled = true,
        NormalTexture = GD.Load<Texture2D>(Dir + "container_normal.png"),
        AOEnabled = true,
        AOTexture = GD.Load<Texture2D>(Dir + "container_ao.png"),
        AOTextureChannel = BaseMaterial3D.TextureChannel.Red,
        Roughness = 1f,
        RoughnessTexture = GD.Load<Texture2D>(Dir + "container_rough.png"),
        RoughnessTextureChannel = BaseMaterial3D.TextureChannel.Red,
        Metallic = 0.12f,   // as the page's container (docs/mockups/freighter-concepts.html, CONTAINER)
    };

    private static MeshInstance3D? Find(Node n)
    {
        if (n is MeshInstance3D m) return m;
        foreach (Node c in n.GetChildren()) if (Find(c) is { } f) return f;
        return null;
    }

    private static float F(JsonElement e) => (float)e.GetDouble();
}
