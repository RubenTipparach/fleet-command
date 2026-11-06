// Kits: the one place a skin id becomes a kit (CLAUDE.md section 7: "One UI kit, swapped per faction"). The composition
// root asks for the skin by id (the faction of the ship you command, or the --skin argument); a new skin is one more entry here.
using System;
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Game.Ui.Kit;

public static class Kits
{
    // in the order the checks walk them, the Concord first: the reference every other skin is compared with
    private static readonly (string Id, Func<IKit> Make)[] Registry =
    {
        ("concord", () => new ConcordKit()),
        ("krai", () => new KraiKit()),
        ("ssarith", () => new SsarithKit()),
        ("kelvane", () => new KelvaneKit()),
    };

    /// <summary>Every skin id there is, the Concord first.</summary>
    public static IReadOnlyList<string> Ids { get; } = Registry.Select(k => k.Id).ToArray();

    /// <summary>The kit for a skin id ("concord", "krai", "ssarith", "kelvane"); an unknown id throws, naming it and the ids there are.</summary>
    public static IKit For(string skinId) =>
        Registry.FirstOrDefault(k => k.Id == skinId).Make is { } make ? make()
        : throw new ArgumentException($"no UI skin '{skinId}' (there are: {string.Join(", ", Ids)})", nameof(skinId));
}
