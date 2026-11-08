// The paper chart's export agrees with the game (openspec/changes/paper-map; CLAUDE.md section 6.5: "when two things must
// agree, validate the real artifacts"). tools/mockups/export_chart.mjs writes the bank of drawn pieces the page draws from
// (assets/scenes/chart/manifest.json and drawn/): a cloud and a storm in three variants for every colour of the nebula
// palette, three suns, twelve rocks and the cloud's shadow. The game stands them on its own map by the page's rules
// (src/Game/Flow/ChartRoom.cs BuildDrawn), choosing a cloud by its palette index (CampaignMap.Palette), so the bank must hold
// every drawing those rules can ask for, and the page's palette and the game's must be the same length. A palette change
// without a re-export fails here.
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace FleetCommand.Tests;

public sealed class ChartTests
{
    private static string Root => Path.GetDirectoryName(TestData.DataDir)!;

    private static JsonElement Manifest() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "assets", "scenes", "chart", "manifest.json"))).RootElement;

    /// <summary>The colours in the game's nebula palette, read from the game's own source (CampaignMap.Palette).</summary>
    private static int GamePaletteCount()
    {
        string src = File.ReadAllText(Path.Combine(Root, "src", "Game", "Flow", "CampaignMap.cs"));
        Match m = Regex.Match(src, @"string\[\] Palette =\s*\{(?<body>[^}]*)\}");
        Assert.True(m.Success, "CampaignMap.Palette not found");
        return Regex.Matches(m.Groups["body"].Value, "\"#[0-9a-fA-F]{6}\"").Count;
    }

    [Fact]
    public void The_pages_nebula_palette_and_the_games_are_the_same_length()
    {
        Assert.Equal(GamePaletteCount(), Manifest().GetProperty("palette").GetInt32());
    }

    [Fact]
    public void The_bank_holds_every_drawing_the_placement_rules_can_ask_for_and_each_file_is_there()
    {
        JsonElement drawn = Manifest().GetProperty("drawn");
        var want = Enumerable.Range(0, GamePaletteCount())
            .SelectMany(mi => Enumerable.Range(0, 3).SelectMany(v => new[] { $"cloud:{mi}:{v}", $"storm:{mi}:{v}" }))
            .Concat(Enumerable.Range(0, 3).Select(k => "sun:" + k))
            .Concat(Enumerable.Range(0, 12).Select(k => "rock:" + k))
            .Append("cloud-shadow");
        foreach (string id in want)
        {
            Assert.True(drawn.TryGetProperty(id, out JsonElement d), $"the bank has no {id}: re-run tools/mockups/export_chart.mjs");
            string file = Path.Combine(Root, "assets", "scenes", "chart", d.GetProperty("file").GetString()!);
            Assert.True(File.Exists(file), $"{id}'s drawing {file} is missing");
        }
    }
}
