// Kits: the one place a skin id becomes a kit (CLAUDE.md section 7: "One UI kit, swapped per faction"). The composition
// root asks for the skin by id (the --skin argument now; the player's faction later); a new skin is one more entry here.
using System;
using System.Collections.Generic;

namespace FleetCommand.Game.Ui.Kit;

public static class Kits
{
    private static readonly Dictionary<string, Func<IKit>> Registry = new()
    {
        ["concord"] = () => new ConcordKit(),
        ["krai"] = () => new KraiKit(),
    };

    /// <summary>The kit for a skin id ("concord", "krai"); an unknown id throws, naming it and the ids there are.</summary>
    public static IKit For(string skinId) =>
        Registry.TryGetValue(skinId, out Func<IKit>? make) ? make()
        : throw new ArgumentException($"no UI skin '{skinId}' (there are: {string.Join(", ", Registry.Keys)})", nameof(skinId));
}
