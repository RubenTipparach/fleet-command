// AudioSettings: the player's volumes (owner, 2026-11-06: "instead of just button in godot game to go full screen need an
// options menu with audio controls too"). The player's buses, Master and Effects, each set in ten boxes as the Options
// sheet shows them (the game-flow mockup's options screen), remembered in user://settings.cfg beside the window mode. The
// buses are made here when the project has none, so every player of a sound (the music, the interface's clicks, the
// battle's sounds to come) names a bus and this class alone sets how loud it is.
using System;
using Godot;

namespace FleetCommand.Game.Ui;

public static class AudioSettings
{
    private const string Path = "user://settings.cfg", Section = "audio";
    /// <summary>The buses the player sets, in the order the sheet lists them, with their first volume in boxes of 10.</summary>
    public static readonly (string Bus, string Label, int Default)[] Buses = { ("Master", "Master volume", 8), ("Effects", "Effects", 8) };
    /// <summary>Every bus made. The game has no music (owner, 2026-11-10: "Remove music from the game. I didn't approve any of
    /// those tracks", "Don't just mute the music. Remove music from the game"): no player, no bus; the tracks stay in
    /// assets/audio/music, loaded by nothing.</summary>
    private static readonly string[] Made = { "Master", "Effects" };
    public const int Boxes = 10;

    /// <summary>Makes the buses (Effects feeds Master) and sets each to the player's saved volume.</summary>
    public static void ApplySaved()
    {
        foreach (string bus in Made)
            if (AudioServer.GetBusIndex(bus) < 0)
            {
                AudioServer.AddBus();
                int i = AudioServer.BusCount - 1;
                AudioServer.SetBusName(i, bus);
                AudioServer.SetBusSend(i, "Master");
            }
        foreach (var (bus, _, _) in Buses) Apply(bus, Get(bus));
    }

    /// <summary>A bus's volume in boxes, 0 to 10.</summary>
    public static int Get(string bus)
    {
        var cfg = new ConfigFile();
        int def = Array.Find(Buses, b => b.Bus == bus).Default;
        return cfg.Load(Path) != Error.Ok ? def : Math.Clamp(cfg.GetValue(Section, bus, def).AsInt32(), 0, Boxes);
    }

    /// <summary>Sets a bus's volume in boxes and remembers it.</summary>
    public static void Set(string bus, int boxes)
    {
        boxes = Math.Clamp(boxes, 0, Boxes);
        Apply(bus, boxes);
        var cfg = new ConfigFile();
        cfg.Load(Path);
        cfg.SetValue(Section, bus, boxes);
        cfg.Save(Path);
    }

    // boxes to decibels: the boxes are the amplitude's share (8 of 10 is -1.9 dB, 1 of 10 -20 dB), none silent but the
    // empty row. A squared curve set the defaults 20 dB down between the two buses, and the bridge bed and the keys could
    // not be heard (owner, 2026-11-10: "I don't hear any clicks or bridge hums")
    private static void Apply(string bus, int boxes)
    {
        int i = AudioServer.GetBusIndex(bus);
        if (i < 0) return;
        AudioServer.SetBusMute(i, boxes == 0);
        AudioServer.SetBusVolumeDb(i, boxes == 0 ? -80 : Mathf.LinearToDb(boxes / (float)Boxes));
    }
}
