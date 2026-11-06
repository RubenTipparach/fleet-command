// AudioSettings: the player's volumes (owner, 2026-11-06: "instead of just button in godot game to go full screen need an
// options menu with audio controls too"). Three buses, Master, Music and Effects, each set in ten boxes as the Options
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
    public static readonly (string Bus, string Label, int Default)[] Buses = { ("Master", "Master volume", 8), ("Music", "Music", 6), ("Effects", "Effects", 7) };
    public const int Boxes = 10;

    /// <summary>Makes the buses (Music and Effects feed Master) and sets each to the player's saved volume.</summary>
    public static void ApplySaved()
    {
        foreach (var (bus, _, _) in Buses)
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

    // boxes to decibels on a loudness curve: each box is an even step to the ear, none is silent
    private static void Apply(string bus, int boxes)
    {
        int i = AudioServer.GetBusIndex(bus);
        if (i < 0) return;
        float v = boxes / (float)Boxes;
        AudioServer.SetBusMute(i, boxes == 0);
        AudioServer.SetBusVolumeDb(i, boxes == 0 ? -80 : Mathf.LinearToDb(v * v));
    }
}
