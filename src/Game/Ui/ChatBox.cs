// ChatBox: the comms box, over the bottom left of the view in every battle (owner, 2026-11-11: "All comms messages should be
// in the chat box", "Standardize this across all the game"): the battle's log, each station's report and in co-op the crew's
// talk, fading after half a minute. In co-op also the crew's talk (openspec/changes/bridge-crew-coop; owner, 2026-11-11:
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

    // where the box draws something this frame (its lines' backing and its Chat key): the only place it takes the pointer, so
    // it never covers a control under it (the crew sheet's manifest, the view)
    private Rect2 _live;

    public override bool _HasPoint(Vector2 point) => _live.HasPoint(point);

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
        Lockstep? co = X.Coop;
        Crew? crew = co?.Crew;
        long now = co?.Clock() ?? 0;
        float w = Size.X - 28, x = 14, lh = 24, bottom = Size.Y - 48;
        // in a battle, every comms message: the battle's log, which holds the crew's chat, pings, directives and crew notes in
        // their senders' names beside every station's report (owner, 2026-11-11: "All comms messages should be in the chat
        // box"); off a battle, the crew's events
        var lines = new System.Collections.Generic.List<(string Who, Color WhoCol, string Station, string Text, Color Ink, float Fade)>();
        if ((co == null ? X.Battle : co.Battle) is { } bt)
        {
            foreach (Sim.LogLine l in bt.Log.Where(l => Typing || bt.T - l.T < ShowMs / 1000.0).TakeLast(Typing ? Lines * 2 : Lines))
            {
                float fade = Typing ? 1 : (float)Math.Clamp(1 - (bt.T - l.T - (ShowMs / 1000.0 - 5)) / 5, 0, 1);
                int by = l.By == null || crew == null ? 0 : crew.Members.FirstOrDefault(m => m.Name == l.By)?.Id ?? 0;
                bool talk = l.Who is "Chat" or "Orders" or "Crew" or "Ping";
                Color ink = l.Who == "Chat" ? C.Text : l.Who == "Orders" ? C.Caution : Sim.Battle.IsWarning(l) ? C.Caution : talk ? C.Lilac : C.Text;
                lines.Add((l.By ?? "", CrewColors.Of(by, C.Sky), talk ? "" : l.Who, l.Text, ink, fade));
            }
        }
        else if (co != null && crew != null)
            foreach (CoopEvent e in co.Events.Where(e => Typing || now - e.Received < ShowMs).TakeLast(Typing ? Lines * 2 : Lines))
            {
                float fade = Typing ? 1 : Math.Clamp(1 - (now - e.Received - (ShowMs - 5000)) / 5000f, 0, 1);
                lines.Add((crew.NameOf(e.By), CrewColors.Of(e.By, C.Sky), "", e.Text, e.Kind == "chat" ? C.Text : e.Kind is "directive" or "ack" ? C.Caution : C.Lilac, fade));
            }
        lines.RemoveAll(l => l.Fade < 0.05f);   // a line all but faded leaves the box
        float y = bottom - lines.Count * lh;
        var back = new Rect2(x - 6, y - 6, w + 12, lines.Count * lh + (Typing ? 52 : 8));
        if (Typing || lines.Count > 0) Draw2D.Rect(this, back, new Color(0, 0, 0, Typing ? 0.6f : 0.35f));
        _live = Typing ? back : co != null ? new Rect2(x, bottom + 6, 96, 32) : new Rect2();
        foreach (var (who, whoCol, station, text, ink, fade) in lines)
        {
            var parts = new System.Collections.Generic.List<(string, Color)>();
            if (who.Length > 0) parts.Add((who + "  ", new Color(whoCol, fade)));
            if (station.Length > 0) parts.Add((station.ToUpperInvariant() + ": ", new Color(C.Lilac, fade)));
            parts.Add((text, new Color(ink, fade)));
            X.Fonts.DrawRich(this, parts.ToArray(), x, y + 17, 17, w);
            y += lh;
        }
        _line.Position = new Vector2(x, bottom + 4);
        _line.Size = new Vector2(w, 40);
        if (co != null && !Typing) Button("chat.open", new Rect2(x, bottom + 6, 96, 32), "Chat", Open, tip: new Tip("Say to the crew (Enter)"));
    }
}
