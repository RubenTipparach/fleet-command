// SkillIcons: the crew skill icons (change crew-leveling), one picture for each ability a skill changes, drawn by the
// Concord's painter in docs/mockups/lib/skill-icons.js and exported white by tools/mockups/export_skill_icons.mjs to
// assets/ui/skills/<ability>.png; drawn here tinted in the ink a state needs. View only.
using System.Collections.Generic;
using Godot;

namespace FleetCommand.Game.Ui;

public static class SkillIcons
{
    private static readonly Dictionary<string, Texture2D?> Cache = new();

    /// <summary>An ability's icon, or null when it has none.</summary>
    public static Texture2D? Of(string ability)
    {
        if (!Cache.TryGetValue(ability, out Texture2D? t))
            Cache[ability] = t = ResourceLoader.Exists($"res://assets/ui/skills/{ability}.png") ? GD.Load<Texture2D>($"res://assets/ui/skills/{ability}.png") : null;
        return t;
    }

    /// <summary>Draws an ability's icon in a rectangle, in one ink.</summary>
    public static void Draw(CanvasItem c, string ability, Rect2 r, Color ink)
    {
        if (Of(ability) is { } t) c.DrawTextureRect(t, r, false, ink);
    }
}
