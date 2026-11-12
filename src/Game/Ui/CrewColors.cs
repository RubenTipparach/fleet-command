// CrewColors: each co-op crew member's colour (openspec/changes/bridge-crew-coop), the one place it is chosen, so their name
// in the log and the chat, their pings and their station icons all agree. The admiral is the skin's own light; the others
// take, by member id, colours picked to stay apart from each other and from the contact colours (enemy red, friendly green).
using Godot;

namespace FleetCommand.Game.Ui;

public static class CrewColors
{
    private static readonly Color[] Others =
    {
        new(0.98f, 0.72f, 0.25f), new(0.62f, 0.55f, 1.00f), new(0.30f, 0.85f, 0.95f), new(1.00f, 0.55f, 0.80f),
        new(0.80f, 0.90f, 0.40f), new(1.00f, 0.62f, 0.45f), new(0.70f, 0.80f, 1.00f),
    };

    /// <summary>A member's colour: the admiral's is <paramref name="admiral"/>, the rest by id.</summary>
    public static Color Of(int member, Color admiral) => member <= 1 ? admiral : Others[(member - 2) % Others.Length];
}
