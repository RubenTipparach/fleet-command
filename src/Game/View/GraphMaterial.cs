// GraphMaterial: a material on a Material Maker graph's committed maps (assets/textures/materials/<graph>/: albedo, normal,
// ORM), and the dressing of an exported glb's stand-in materials by name. One implementation for every scene exported from
// a mockup with its materials named in a manifest (tools/mockups/export_faction.mjs, tools/mockups/export_chart.mjs): the
// glb carries plain materials under names, and the game builds each from the graph's maps, so the maps have one copy.
using System;
using Godot;

namespace FleetCommand.Game.View;

public static class GraphMaterial
{
    /// <summary>A material on a graph's maps: albedo tinted (sRGB), tangent normals at <paramref name="normalScale"/>, ORM
    /// for occlusion, roughness and metalness (its factors 1, as the page's are), tiled <paramref name="uv"/> times across
    /// the glb's uvs, with an optional clearcoat.</summary>
    public static OrmMaterial3D Make(string graph, Color tint, Vector2 uv, float normalScale = 1, float clearcoat = 0, float clearcoatRoughness = 0.3f)
    {
        string g = $"res://assets/textures/materials/{graph}/";
        var mat = new OrmMaterial3D
        {
            AlbedoTexture = GD.Load<Texture2D>(g + "albedo.png"), AlbedoColor = tint,
            NormalEnabled = true, NormalTexture = GD.Load<Texture2D>(g + "normal.png"), NormalScale = normalScale,
            OrmTexture = GD.Load<Texture2D>(g + "orm.png"), Roughness = 1, Metallic = 1,
            Uv1Scale = new Vector3(uv.X, uv.Y, 1), TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
        };
        if (clearcoat > 0) { mat.ClearcoatEnabled = true; mat.Clearcoat = clearcoat; mat.ClearcoatRoughness = clearcoatRoughness; }
        return mat;
    }

    /// <summary>Puts each surface's material in place by the name the glb gives it, turning shadows on; a name the pick has no
    /// answer for keeps its own.</summary>
    public static void Dress(Node n, Func<string, Material?> pick)
    {
        if (n is MeshInstance3D mi && mi.Mesh != null)
        {
            mi.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
            for (int i = 0; i < mi.Mesh.GetSurfaceCount(); i++)
                if (mi.Mesh.SurfaceGetMaterial(i) is { } m && pick(m.ResourceName) is { } r) mi.SetSurfaceOverrideMaterial(i, r);
        }
        foreach (Node c in n.GetChildren()) Dress(c, pick);
    }
}
