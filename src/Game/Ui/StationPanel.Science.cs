// StationPanel.Science: the Science station, as the battle console mockup's stScience draws it: Scan target and the scan's
// bar, Active or Passive sensors, ECM and ECCM as rows of energy boxes out of the sensors' energy, the Cloak and the Sweep
// that finds cloaked ships, Scan planet with its survey's bar while a planet is selected (Battle.ScanPlanet), and the
// scanned target's detail (its shields by facing, its weakest, its weapons). Every
// figure is the battle's own: the sweep's reach is Battle.SweepKm, the boxes' cap what Battle.SetEw allows (CLAUDE.md
// 6.1). Drawn only with the kit, so every skin has it.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    partial void SciencePage(Rect2 b, ref bool drawn)
    {
        drawn = true;
        Ship s = Me;
        Ship? t = B.TargetOf(s);
        bool hostile = t != null && t.Side != s.Side;
        float x = b.Position.X, w = b.Size.X, y = b.Position.Y;
        bool scanning = s.ScanId != null && s.ScanId == s.Target;
        Button("sci.scan", new Rect2(x, y, w, 48), scanning ? (s.ScanP >= 1 ? "Scan complete" : "Scanning") : "Scan target", () => B.Scan(),
            on: scanning && s.ScanP < 1, dis: t == null, tip: new Tip("Scan the locked target for its shields and weapons"));
        y += 54;
        y += Bar(x, y, w, "Scan", scanning ? s.ScanP : 0, false) + 10;
        Lbl("Sensors", x, y + 13);
        y += 20;
        Tabs("sens", new Rect2(x, y, w, 40), new[] { ("active", "Active"), ("passive", "Passive") }, s.Passive ? "passive" : "active", id => B.SetPassive(id == "passive"));
        y += 48;
        int cells = B.Data.Systems.Cloak.Sensors.EwCells, sens = s.Power["sensors"];
        Lbl("ECM", x, y + 19);
        CellRow("ecm", new Rect2(x + 60, y, w - 60, 26), cells, s.Ecm, n => B.SetEw(false, n), Math.Max(0, sens - s.Eccm));
        y += 32;
        Lbl("ECCM", x, y + 19);
        CellRow("eccm", new Rect2(x + 60, y, w - 60, 26), cells, s.Eccm, n => B.SetEw(true, n), Math.Max(0, sens - s.Ecm));
        y += 34;
        // the cloak and the sweep that finds cloaked ships (openspec/changes/cloaking)
        {
            float hw = (w - 6) / 2;
            bool fitted = B.Fitted(s);
            double km = B.SweepKm(s);
            var cr = B.Data.Systems.Cloak.Cloak;
            string cl = s.CloakOn ? (s.CloakK >= 1 ? "Cloaked" : $"Cloaking {Pct(s.CloakK)}") : s.CloakK > 0 ? $"Decloak {Pct(1 - s.CloakK)}" : "Cloak";
            Button("sci.cloak", new Rect2(x, y, hw, 40), cl, () => B.ToggleCloak(), on: s.CloakOn, dis: !fitted,
                tip: new Tip(fitted ? $"Cloak: {cr.CostE:0} E, no shields or fire while cloaked" : "No cloak fitted"));
            Button("sci.sweep", new Rect2(x + hw + 6, y, hw, 40), s.SweepT > 0 ? $"Sweep {Math.Ceiling(s.SweepT)} s" : $"Sweep {km:0} km", () => B.Sweep(),
                dis: !B.CanSweep(s), tip: new Tip($"Reveal cloaked ships within {km:0} km (ECCM adds {cr.PerEccmKm:0} km a point)"));
            y += 46;
        }
        // the planet selected (Battle.PlanetDest): Scan planet and the survey's bar, from within the survey reach
        if (B.Planets.FirstOrDefault(p => p.Id == B.PlanetDest) is { } pl)
        {
            float kw = MathF.Round(w * 0.44f);
            bool surveying = B.PlanetScanId == pl.Id, near = B.InSurveyReach(pl);
            double reach = B.Data.Systems.Field.PlanetScanKm;
            string key = pl.Scanned ? "Surveyed" : surveying ? (near ? "Surveying" : $"Close to {reach:0} km") : "Scan planet";
            Button("sci.planet", new Rect2(x, y, kw, 44), key, () => B.ScanPlanet(), on: surveying, dis: pl.Scanned,
                tip: new Tip(pl.Scanned ? $"{pl.Name}: {pl.Installations.Count(i => i.Known)} sites found" : $"Survey {pl.Name} for what stands on its surface (inside {reach:0} km)"));
            Bar(x + kw + 8, y + 2, w - kw - 8, $"{pl.Name} · {Battle.PlanetKm(s, pl):0} km", pl.ScanP, !near && !pl.Scanned, pl.Scanned);
            y += 50;
        }
        if (t == null || !hostile) { Lbl("No target", x, y + 14); return; }
        if (!B.Scanned(t)) { Lbl($"{Aiming.ContactName(B, t)}: scan for detail", x, y + 14, w); return; }
        int weak = Array.IndexOf(t.Shields, t.Shields.Min());
        var ws = new Dictionary<string, int>();
        foreach (Mount m in t.Mounts) ws[m.Weapon.Name] = ws.GetValueOrDefault(m.Weapon.Name) + 1;
        Text($"{t.Name} · {t.Class.Name}", x, y + 16, 18, C.Text, HorizontalAlignment.Left, w);
        Lbl("Shields " + string.Join(" · ", t.Shields.Select((v, i) => $"{Rules.Facings[i]} {Math.Round(v)}")), x, y + 38, w);
        Lbl($"Weakest {Rules.Facings[weak]} · " + string.Join(" · ", ws.Select(kv => $"{kv.Value}x {kv.Key}")), x, y + 58, w);
    }
}
