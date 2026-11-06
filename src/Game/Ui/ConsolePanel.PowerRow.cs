// ConsolePanel.PowerRow: one system's row of energy boxes, the one control every panel that sets a system's energy draws
// (CLAUDE.md 6.1): the power station's seven rows and the tractor page's pick (the mockup's powerRow, which its shield,
// tractor and power pages share). It lives on the base panel so the station and ship panels draw the same row.
using System;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public abstract partial class ConsolePanel
{
    /// <summary>One system's row of ten boxes: lit to its allocation, locked past what the output leaves.</summary>
    public void PowerRow(string idp, float x, float y, float w, float h, string key, float lw)
    {
        int cur = Me.Power[key];
        Lbl(key, x, y + h / 2 + 5, lw - 4);
        CellRow($"{idp}.{key}", new Rect2(x + lw, y, w - lw - 32, h), Rules.BoxesPerSystem, cur, n => B.SetPower(key, n), Math.Min(Rules.BoxesPerSystem, B.Rules.PowerRoom(Me, cur)));
        Num(cur.ToString(), x + w, y + h / 2 + 8, 0, null, HorizontalAlignment.Right, 20);
    }
}
