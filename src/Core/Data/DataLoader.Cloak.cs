// DataLoader.Cloak: the scenario fields of the cloak-and-sensors package, read with the rest of a scenario: nebula plumes on
// the battle plane, a scene won by repelling boarders, and per ship its cloak, ECM and ECCM, the energy its scene gives its
// systems, its marines and a boarding captain's orders (the battle console mockup's scene setups, docs/mockups/
// battle-console.html SCENES.nebula, cloak and repel). Every field is optional and checked (CLAUDE.md 6.4).
using System;
using System.Collections.Generic;
using FleetCommand.Sim;

namespace FleetCommand.Data;

/// <summary>A ship's start in the cloak package's terms: cloaked from the start, flown by a cloaking captain, its ECM and
/// ECCM, the energy boxes its scene gives some systems, its marines (null: its class's), a boarding captain and when its
/// first squad goes, its shields down from the start, and its battery's fill (null: the usual start).</summary>
public sealed record CloakStart(bool Cloaked, bool AiCloak, int? Ecm, int Eccm, IReadOnlyDictionary<string, int>? PowerE, int? Marines,
    bool AiBoard, double BoardFirstS, bool ShieldsDown, double? BatteryFrac);

public static partial class DataLoader
{
    private static readonly string[] CloakShipKeys = { "cloaked", "ai_cloak", "ecm", "eccm", "power_e", "marines", "ai_board", "board_first_s", "shields_down", "battery_frac" };

    /// <summary>A ship's cloak-package fields, or null when it has none.</summary>
    public static CloakStart? CloakShipStart(StrictJson s)
    {
        bool any = false;
        foreach (string k in CloakShipKeys) any |= s.Has(k);
        if (!any) return null;
        int? ecm = s.Has("ecm") ? s.Int("ecm") : null;
        int eccm = s.Has("eccm") ? s.Int("eccm") : 0;
        if (ecm is < 0 || eccm < 0) throw new DataError($"{s.Where}: ecm and eccm are 0 or more");
        Dictionary<string, int>? power = null;
        if (s.ObjOrNull("power_e") is { } p)
        {
            power = new Dictionary<string, int>();
            foreach (string k in p.Keys())
            {
                if (Array.IndexOf(Rules.Systems, k) < 0) throw new DataError($"{p.Where}.{k}: no system '{k}' ({string.Join(", ", Rules.Systems)})");
                int v = p.Int(k);
                if (v < 0 || v > Rules.BoxesPerSystem) throw new DataError($"{p.Where}.{k}: 0 to {Rules.BoxesPerSystem} boxes");
                power[k] = v;
            }
            p.Done();
        }
        int? marines = s.Has("marines") ? s.Int("marines") : null;
        if (marines is < 0) throw new DataError($"{s.Where}.marines: 0 or more");
        double first = s.Num("board_first_s", 0);
        if (first < 0) throw new DataError($"{s.Where}.board_first_s: 0 or more");
        double? bat = s.Has("battery_frac") ? s.Num("battery_frac") : null;
        if (bat is < 0 or > 1) throw new DataError($"{s.Where}.battery_frac: between 0 and 1");
        return new CloakStart(s.Bool("cloaked", false), s.Bool("ai_cloak", false), ecm, eccm, power, marines, s.Bool("ai_board", false), first,
            s.Bool("shields_down", false), bat);
    }

    /// <summary>A scenario's nebula plumes ("nebulae": each x_m, z_m and r_m in metres, density above 0 to 1, storm for an
    /// ion storm, and a look of NebulaDef.Looks: "violet" when left out, a storm's "slate"); empty when it has none.</summary>
    public static IReadOnlyList<NebulaDef> CloakNebulae(StrictJson root)
    {
        var list = new List<NebulaDef>();
        if (!root.Has("nebulae")) return list;
        foreach (StrictJson n in root.ObjList("nebulae"))
        {
            double r = n.Num("r_m"), d = n.Num("density");
            if (r <= 0) throw new DataError($"{n.Where}.r_m: must be above 0");
            if (d is <= 0 or > 1) throw new DataError($"{n.Where}.density: above 0 and at most 1");
            bool storm = n.Bool("storm", false);
            string look = n.Has("look") ? n.Str("look") : storm ? NebulaDef.StormLook : "violet";
            if (Array.IndexOf(NebulaDef.Looks, look) < 0) throw new DataError($"{n.Where}.look: one of {string.Join(", ", NebulaDef.Looks)}, not '{look}'");
            list.Add(new NebulaDef(n.Num("x_m"), n.Num("z_m"), r, d, look, storm));
            n.Done();
        }
        return list;
    }
}
