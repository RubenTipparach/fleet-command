// StationPanel.TransportSites: the Transport station's Sites view, beside Supplies and Misc on the Cargo tab
// (openspec/changes/mission-systems; owner, 2026-11-08: "Falchion will especially be important for us to beam down or examine
// planetary installations for exploratory or tactical purposes"): the planet selected (Battle.InvPick), how far it is and
// whether it is in reach, then each site a survey has found (Installation.Known) as a row: its name, its kind and state
// (armed with its guard, held, driven back, an away team down and its time), and a Beam down key that sends an away team
// (Battle.BeamDown). A key that cannot send one now is dimmed with the battle's own reason as its tip
// (Battle.CannotBeamDown), so the panel never disagrees with the order (CLAUDE.md 6.1). Rows that do not fit are left out;
// the panel keeps its size.
using System;
using System.Linq;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    private void Sites(Rect2 b)
    {
        Ship s = Me;
        float x = b.Position.X, y = b.Position.Y, w = b.Size.X;
        TransportDest? d = B.InvPick();
        if (d?.Planet is not { } pl) { Lbl("Select a planet: click it in the view", x, y + 18, w, C.Caution); return; }
        double range = B.Comms.Transport.RangeKm;
        X.Fonts.DrawRich(this, new[] { (pl.Name, C.Accord), ($" · {d.Km:0.0} km · ", C.Lilac),
            (d.Km <= range ? "in reach" : $"out of reach ({range:0.0} km)", d.Km <= range ? C.Accord : C.Caution) }, x, y + 18, 18, w);
        y += 28;
        Installation[] known = pl.Installations.Where(i => i.Known).ToArray();
        if (known.Length == 0)
        {
            Lbl(pl.Scanned ? "Nothing built on the surface" : "Not surveyed: scan it from Science", x, y + 16, w, pl.Scanned ? null : C.Caution);
            y += 26;
        }
        float kw = Big ? 170 : 128, rowH = 50;
        foreach (Installation st in known)
        {
            if (y + 44 > b.End.Y) break;
            AwayTeam? team = B.AwayTeams.FirstOrDefault(a => a.Planet == pl.Id && a.Site == st.Id);
            Color col = st.Secured ? C.Accord : st.Failed ? C.Caution : st.Hostile ? C.Alert : C.Sky;
            string state = team != null ? (team.Done ? "team waiting to beam up" : $"team of {team.Marines} down · {Math.Ceiling(team.T)} s")
                : st.Secured ? "held" : st.Failed ? $"driven back · {st.Garrison} on guard" : st.Hostile ? $"armed · {st.Garrison} on guard" : "unguarded";
            DrawCircle(new Vector2(x + 7, y + 13), 5, col, true, -1, true);
            Text(st.Name, x + 18, y + 19, 18, C.Text, HorizontalAlignment.Left, w - kw - 26);
            Lbl($"{st.Kind} · {state}", x + 18, y + 38, w - kw - 26, team != null ? C.Sky : null, HorizontalAlignment.Left, 13);
            var r = new Rect2(x + w - kw, y, kw, 44);
            string id = $"tr.site.{st.Id}", ps = pl.Id, ss = st.Id;
            string? why = B.CannotBeamDown(ps, ss);
            if (why != null) Ui.Reg(id, r, null, null, new Tip(why));   // dimmed, the reason on hover
            Button(id, r, "Beam down", () => B.BeamDown(ps, ss), dis: why != null,
                tip: new Tip($"Beam an away team of up to {B.Data.Systems.Field.Away.TeamMarines} marines down to {st.Name}"));
            y += rowH;
        }
        if (y + 40 <= b.End.Y) PadRow("tr.pad.site", x, y, w, s, "inv");
    }
}
