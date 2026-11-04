// TestData: loads the real files in data/ for the tests (CLAUDE.md section 6.5: validate the real artifacts).
using System;
using System.IO;
using FleetCommand.Data;
using FleetCommand.Sim;

namespace FleetCommand.Tests;

public static class TestData
{
    /// <summary>The repository's data/ folder, found by walking up from the test binary.</summary>
    public static string DataDir
    {
        get
        {
            string? d = AppContext.BaseDirectory;
            while (d != null && !File.Exists(Path.Combine(d, "build.config"))) d = Path.GetDirectoryName(d);
            if (d == null) throw new InvalidOperationException("repository root (build.config) not found above " + AppContext.BaseDirectory);
            return Path.Combine(d, "data");
        }
    }

    public static string Read(string rel) => File.ReadAllText(Path.Combine(DataDir, rel));

    private static GameData? _data;
    public static GameData Data => _data ??= DataLoader.Load(Read);

    /// <summary>The golden seed's battle: the Concord's Raiders scenario (the battle console mockup's skirmish).</summary>
    public static Scenario Raiders => Data.ScenarioById("concord_raiders");

    /// <summary>A fresh Raiders battle from the real data.</summary>
    public static Battle NewBattle() => new(Data, Raiders);
}
