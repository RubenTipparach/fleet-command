// ChatBox: the co-op crew's talk, over the bottom left of the view (openspec/changes/bridge-crew-coop; owner, 2026-11-11:
// "Do you have in game chat? Event logs with player names?", "someway for players to see and interact with crew members in
// game"). The last crew events (chat, pings, the admiral's directives and their answers, crew changes) each with its
// sender's name in their colour, fading after half a minute; Enter or the Chat key opens a line to type in, Enter sends it
// to the whole crew, Esc shuts it. The same box on the crew's waiting screen. What it shows is the lockstep's events
// (Lockstep.Events); it holds none of its own.
using System;
using System.Linq;
using FleetCommand.Coop;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class ChatBox : ConsolePanel
{
    protected override bool InColumn => false;

    private LineEdit _line = null!;
    private const int Lines = 6;
    private const long ShowMs = 30000;

    /// <summary>Whether a line is open for typing.</summary>
    public bool Typing => _line.Visible;

    public override void _Ready()
    {
        _line = new LineEdit { Name = "ChatLine", PlaceholderText = "Say to the crew", CaretBlink = true, Visible = false, MaxLength = 200 };
        _line.AddThemeFontSizeOverride("font_size", 18);
        _line.TextSubmitted += Send;
        _line.GuiInput += e => { if (e is InputEventKey { Pressed: true, Keycode: Key.Escape }) Shut(); };
        AddChild(_line);
    }

    /// <summary>Opens the line to type in (Enter, or the Chat key).</summary>
    public void Open()
    {
        if (X.Coop == null) return;
        _line.Visible = true;
        _line.GrabFocus();
    }

    private void Shut()
    {
        _line.Text = "";
        _line.ReleaseFocus();
        _line.Visible = false;
    }

    private void Send(string text)
    {
        X.Coop?.Chat(text);
        Shut();
    }

    protected override void DrawPanel()
    {
        if (X.Coop is not { } co) return;
        Crew crew = co.Crew;
        long now = co.Clock();
        float w = Size.X - 28, x = 14, lh = 24, bottom = Size.Y - 48;
        var shown = co.Events.Where(e => Typing || now - e.Received < ShowMs).TakeLast(Typing ? Lines * 2 : Lines).ToList();
        float y = bottom - shown.Count * lh;
        if (Typing || shown.Count > 0) DrawRect(new Rect2(x - 6, y - 6, w + 12, shown.Count * lh + (Typing ? 52 : 8)), new Color(0, 0, 0, Typing ? 0.6f : 0.35f));
        foreach (CoopEvent e in shown)
        {
            float fade = Typing ? 1 : Math.Clamp(1 - (now - e.Received - (ShowMs - 5000)) / 5000f, 0, 1);
            Color who = new(CrewColors.Of(e.By, C.Sky), fade);
            string say = e.Kind switch { "chat" => e.Text, "directive" => "⟶ " + e.Text, _ => e.Text };
            Color ink = new(e.Kind is "chat" ? C.Text : e.Kind is "directive" or "ack" ? C.Caution : C.Lilac, fade);
            X.Fonts.DrawRich(this, new[] { ($"{crew.NameOf(e.By)}  ", who), (say, ink) }, x, y + 17, 17, w);
            y += lh;
        }
        _line.Position = new Vector2(x, bottom + 4);
        _line.Size = new Vector2(w, 40);
        if (!Typing) Button("chat.open", new Rect2(x, bottom + 6, 96, 32), "Chat", Open, tip: new Tip("Say to the crew (Enter)"));
    }
}
