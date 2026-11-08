// SystemsDef: the numbers of the mockup's systems ported one package at a time (comms and transport, tractor and craft,
// reactor and repair, cloak and sensors, tactical and fleet), each package's own part of this class in its own file and
// read from its own file, data/battle/systems/<package>.json, so the packages never edit the same lines. Tuning lives in
// data (CLAUDE.md 6.4): each part validates its file and an unknown key is an error.
using System;

namespace FleetCommand.Data;

public sealed partial class SystemsDef
{
    /// <summary>Reads every package's file; <paramref name="json"/> parses a file under data/ by its relative path.</summary>
    public static SystemsDef Load(Func<string, StrictJson> json)
    {
        var d = new SystemsDef();
        d.LoadComms(json("battle/systems/comms.json"));
        d.LoadTractor(json("battle/systems/tractor.json"));
        d.LoadReactor(json("battle/systems/reactor.json"));
        d.LoadCloak(json("battle/systems/cloak.json"));
        d.LoadCloakPlans(json);
        d.LoadTactical(json("battle/systems/tactical.json"));
        d.LoadField(json("battle/systems/field.json"));
        return d;
    }

    // a number with a default that must be above 0, or 0 or more (the packages' readers share these)
    private static double Pos(StrictJson o, string k, double d) { double v = o.Num(k, d); if (v <= 0) throw new DataError($"{o.Where}.{k}: must be above 0"); return v; }
    private static double Non(StrictJson o, string k, double d) { double v = o.Num(k, d); if (v < 0) throw new DataError($"{o.Where}.{k}: must be 0 or more"); return v; }

    partial void LoadComms(StrictJson o);
    partial void LoadTractor(StrictJson o);
    partial void LoadReactor(StrictJson o);
    partial void LoadCloak(StrictJson o);
    /// <summary>The cloak package's deck plans, read from the files its numbers name (data/ships/).</summary>
    partial void LoadCloakPlans(Func<string, StrictJson> json);
    partial void LoadTactical(StrictJson o);
    partial void LoadField(StrictJson o);
}
