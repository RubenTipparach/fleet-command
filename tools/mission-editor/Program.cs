// The mission editor: write, check and try out Fleet Command's mission files (data/missions/*.json) outside Godot
// (openspec/changes/mission-scripting). Avalonia UI, chosen for Windows and macOS (owner, 2026-11-05: "pick a ui package
// that works good for windows/mac"): one C# app on .NET 8, the game's own language and runtime, so the editor loads,
// validates and previews a mission through FleetCommand.Core, the code the game runs, and an author sees the game's own
// errors and the game's own grading. Run: dotnet run --project tools/mission-editor (from the repository).
//
// The window: the mission files down the left (New makes one from a template); the file's main fields as a form and the
// whole file as JSON in the middle, kept in step both ways; the checks and a preview on the right (the briefing, the battle
// as planned, and what winning, withdrawing or losing grades and pays, on a new campaign of the side and seed chosen); the
// names each expression may use at the bottom, read from the loader's own lists so they cannot drift.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using FleetCommand.Campaign;

namespace FleetCommand.MissionEditor;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppBuilder b = AppBuilder.Configure<App>().WithInterFont().LogToTrace();
        // a --shot run draws off screen (Skia, no window on the desktop), so it works with no display at all
        b = args.Any(a => a.StartsWith("--shot=")) ? b.UseSkia().UseHeadless(new Avalonia.Headless.AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }) : b.UsePlatformDetect();
        b.StartWithClassicDesktopLifetime(args);
    }
}

public sealed class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
        {
            var w = new EditorWindow(Repo.Find());
            d.MainWindow = w;
            // --shot=path.png [--file=name.json] [--end=win]: a picture of the window as drawn, then quit (for the docs)
            string? shot = d.Args?.FirstOrDefault(a => a.StartsWith("--shot="))?[7..];
            if (shot != null) w.Shoot(shot, d.Args!.FirstOrDefault(a => a.StartsWith("--file="))?[7..], d.Args!.FirstOrDefault(a => a.StartsWith("--end="))?[6..], () => d.Shutdown());
        }
        base.OnFrameworkInitializationCompleted();
    }
}

/// <summary>Where the repository's data/ is: found by walking up from the working folder or the app to build.config.</summary>
public static class Repo
{
    public static string Find()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (string? d = start; d != null; d = Path.GetDirectoryName(d))
                if (File.Exists(Path.Combine(d, "build.config")) && Directory.Exists(Path.Combine(d, "data", "missions"))) return d;
        throw new InvalidOperationException("Run the mission editor from inside the Fleet Command repository (no build.config and data/missions found).");
    }
}

public sealed class EditorWindow : Window
{
    private readonly string _root;
    private string DataDir => Path.Combine(_root, "data");
    private readonly ListBox _files = new();
    private readonly TextBox _json = Mono(true);
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6) };
    private readonly TextBox _report = Mono(false);
    private readonly ComboBox _side = new() { ItemsSource = new[] { "concord", "krai" }, SelectedIndex = 0, MinWidth = 110 };
    private readonly NumericUpDown _seed = new() { Value = 7, Minimum = 0, Maximum = 999999, Increment = 1, FormatString = "0", MinWidth = 120 };
    private readonly TextBox _data = new() { Watermark = "handed data, e.g. prize_name=Vekk; tier=2", MinWidth = 260 };
    private readonly Dictionary<string, TextBox> _form = new();
    private string? _file;
    private bool _syncing, _dirty;

    private static readonly (string Key, string Label, bool Number)[] Fields =
    {
        ("id", "Id", false), ("name", "Name", false), ("glyph", "Map letter", false), ("kind", "Kind (contract, story, special, encounter or event)", false),
        ("type", "Mission type", false), ("reward_cr", "Reward, cr", true), ("turns", "Turns on offer", true), ("where", "Where (expression)", false),
        ("objective", "Objective", false),
    };

    public EditorWindow(string root)
    {
        _root = root;
        Title = "Fleet Command: mission editor";
        Width = 1500;
        Height = 940;

        _files.SelectionChanged += (_, _) => { if (_files.SelectedItem is string f && f != _file) Open(f); };
        var newBtn = new Button { Content = "New mission" };
        newBtn.Click += (_, _) => NewMission();
        var left = new DockPanel { Width = 230, Margin = new Thickness(8) };
        DockPanel.SetDock(newBtn, Dock.Bottom);
        left.Children.Add(newBtn);
        left.Children.Add(Titled("Mission files", _files));

        var form = new StackPanel { Spacing = 4 };
        foreach (var (key, label, num) in Fields)
        {
            var tb = new TextBox { AcceptsReturn = key == "objective" || key == "where", TextWrapping = TextWrapping.Wrap };
            string k = key;
            bool n = num;
            tb.TextChanged += (_, _) => FormEdited(k, tb.Text ?? "", n);
            _form[key] = tb;
            form.Children.Add(new TextBlock { Text = label, Opacity = 0.75, FontSize = 12 });
            form.Children.Add(tb);
        }
        _json.TextChanged += (_, _) => JsonEdited();
        var save = new Button { Content = "Save" };
        save.Click += (_, _) => Save();
        var revert = new Button { Content = "Reload from disk" };
        revert.Click += (_, _) => { if (_file != null) Open(_file); };
        var middle = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(8) };
        var formScroll = new ScrollViewer { Content = form, MaxHeight = 330 };
        middle.Children.Add(Titled("Main fields", formScroll));
        var jsonBox = Titled("The whole file (JSON)", _json);
        Grid.SetRow(jsonBox, 1);
        middle.Children.Add(jsonBox);
        var bar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { save, revert } };
        Grid.SetRow(bar, 2);
        middle.Children.Add(bar);

        var win = new Button { Content = "Win" };
        var wd = new Button { Content = "Withdraw" };
        var lose = new Button { Content = "Lose" };
        var brief = new Button { Content = "Brief only" };
        brief.Click += (_, _) => Preview(null);
        win.Click += (_, _) => Preview(MissionPreview.End.Win);
        wd.Click += (_, _) => Preview(MissionPreview.End.Withdraw);
        lose.Click += (_, _) => Preview(MissionPreview.End.Lose);
        var opts = new WrapPanel
        {
            Children =
            {
                new TextBlock { Text = "Side", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) }, _side,
                new TextBlock { Text = "Seed", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 6, 0) }, _seed,
            },
        };
        var tryRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 6), Children = { brief, win, wd, lose } };
        var right = new DockPanel { Margin = new Thickness(8), MinWidth = 520 };
        var top = new StackPanel { Children = { _status, opts, _data, tryRow } };
        DockPanel.SetDock(top, Dock.Top);
        right.Children.Add(top);
        right.Children.Add(Titled("Preview (the game's own code, on a new campaign)", _report));

        var names = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, Opacity = 0.8, Margin = new Thickness(8), Text = NameHelp() };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,2*,2*"), RowDefinitions = new RowDefinitions("*,Auto") };
        grid.Children.Add(left);
        Grid.SetColumn(middle, 1);
        grid.Children.Add(middle);
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);
        var help = new ScrollViewer { Content = names, MaxHeight = 150 };
        Grid.SetRow(help, 1);
        Grid.SetColumnSpan(help, 3);
        grid.Children.Add(help);
        Content = grid;

        Refresh();
        if (_files.ItemCount > 0) _files.SelectedIndex = 0;
    }

    /// <summary>Opens a file, previews it to an end, renders the window to a PNG and calls done.</summary>
    public void Shoot(string path, string? file, string? end, Action done)
    {
        Opened += async (_, _) =>
        {
            try
            {
            if (file != null) _files.SelectedItem = file;
            await System.Threading.Tasks.Task.Delay(600);
            if (end != null && Enum.TryParse(end, true, out MissionPreview.End e)) Preview(e);
            await System.Threading.Tasks.Task.Delay(900);
            var size = new PixelSize((int)Width, (int)Height);
            using var bmp = new Avalonia.Media.Imaging.RenderTargetBitmap(size);
            bmp.Render(this);
            bmp.Save(path);
            Console.WriteLine($"SHOT {path} {size.Width}x{size.Height}");
            }
            catch (Exception ex) { Console.WriteLine("SHOT_FAIL " + ex); }
            done();
        };
    }

    private static TextBox Mono(bool edit) => new()
    {
        AcceptsReturn = true, AcceptsTab = edit, IsReadOnly = !edit, TextWrapping = edit ? TextWrapping.NoWrap : TextWrapping.Wrap,
        FontFamily = new FontFamily("Menlo, Consolas, monospace"), FontSize = 13,
    };

    private static Control Titled(string title, Control body)
    {
        var d = new DockPanel();
        var t = new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 4) };
        DockPanel.SetDock(t, Dock.Top);
        d.Children.Add(t);
        d.Children.Add(body);
        return d;
    }

    /// <summary>The names an expression may use where, from the loader's own lists (MissionScript.cs, Scopes).</summary>
    private static string NameHelp() =>
        "Expressions: numbers, 'text', true/false, + - * / %, < <= > >= == !=, && || !, a ? b : c, and min max floor ceil round abs round10.  Text: {expression} inside any text.\n" +
        $"Everywhere: {string.Join(", ", Scopes.Offer)}; {string.Join(", ", Scopes.OfferCalls.Select(c => c + "()"))}; and every name the setup sets or the mission takes.\n" +
        $"In where, per sector: {string.Join(", ", Scopes.Hex)}; {string.Join(", ", Scopes.HexCalls.Select(c => c + "()"))}.\n" +
        $"In an objective's done: {string.Join(", ", Scopes.BattleEnd)}, the battle's group ids and fleet, enemies, allies; {string.Join(", ", Scopes.BattleCalls.Select(c => c + "()"))}.\n" +
        $"In grade and result rules: {string.Join(", ", Scopes.Grade.Concat(Scopes.Result))}; {string.Join(", ", Scopes.GradeCalls.Select(c => c + "('objective', ...)"))}.";

    private void Refresh()
    {
        var list = Directory.GetFiles(Path.Combine(DataDir, "missions"), "*.json").Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToList();
        _files.ItemsSource = list;
        if (_file != null) _files.SelectedItem = Path.GetFileName(_file);
    }

    private void Open(string name)
    {
        _file = name;
        _syncing = true;
        _json.Text = File.ReadAllText(Path.Combine(DataDir, "missions", name));
        _syncing = false;
        JsonEdited();
        _dirty = false;
        Check();
        Preview(null);
    }

    private JsonObject? Parsed()
    {
        try { return JsonNode.Parse(_json.Text ?? "") as JsonObject; }
        catch (JsonException) { return null; }
    }

    private void JsonEdited()
    {
        if (_syncing) return;
        _dirty = true;
        JsonObject? o = Parsed();
        _syncing = true;
        foreach (var (key, _, _) in Fields)
        {
            JsonNode? n = o?[key];
            _form[key].Text = n == null ? "" : n is JsonValue v && v.TryGetValue(out string? s) ? s : n.ToJsonString();
            _form[key].IsEnabled = o != null && (key != "objective" || n is JsonValue);
        }
        _syncing = false;
        Check();
    }

    private void FormEdited(string key, string text, bool number)
    {
        if (_syncing) return;
        JsonObject? o = Parsed();
        if (o == null) return;
        if (text.Length == 0) o.Remove(key);
        else if (number && double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d)) o[key] = d;
        else o[key] = text;
        _syncing = true;
        _json.Text = o.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        _syncing = false;
        _dirty = true;
        Check();
    }

    /// <summary>The file checked by the game's loader as it is typed.</summary>
    private void Check()
    {
        try
        {
            MissionScript m = MissionLoader.Load(_json.Text ?? "", "data/missions/" + (_file ?? "new.json"));
            _status.Text = $"OK: {m.Name} loads.{(_dirty ? " Not saved." : "")}";
            _status.Foreground = Brushes.LightGreen;
        }
        catch (FleetCommand.Data.DataError e)
        {
            _status.Text = e.Message;
            _status.Foreground = Brushes.Salmon;
        }
    }

    private IReadOnlyDictionary<string, object>? HandedData()
    {
        var d = new Dictionary<string, object>();
        foreach (string part in (_data.Text ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = part.IndexOf('=');
            if (eq <= 0) continue;
            string k = part[..eq].Trim(), v = part[(eq + 1)..].Trim();
            d[k] = double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double n) ? n : v;
        }
        return d.Count > 0 ? d : null;
    }

    private void Preview(MissionPreview.End? end)
    {
        if (_file == null) return;
        string rel = "missions/" + _file;
        var files = Directory.GetFiles(Path.Combine(DataDir, "missions"), "*.json").Select(f => "missions/" + Path.GetFileName(f));
        PreviewResult r = MissionPreview.Run(f => File.ReadAllText(Path.Combine(DataDir, f)), files, rel, _json.Text ?? "", (string)_side.SelectedItem!,
            (uint)(_seed.Value ?? 7), HandedData(), end);
        _report.Text = r.Ok ? r.Report : r.Error + (r.Report.Length > 0 ? "\n\n" + r.Report : "");
    }

    private void Save()
    {
        if (_file == null) return;
        Check();
        if (_status.Foreground == Brushes.Salmon) return;
        string text = _json.Text ?? "";
        if (!text.EndsWith('\n')) text += "\n";
        File.WriteAllText(Path.Combine(DataDir, "missions", _file), text);
        _dirty = false;
        Check();
        Refresh();
    }

    private void NewMission()
    {
        string id = "new_mission";
        for (int i = 2; File.Exists(Path.Combine(DataDir, "missions", id + ".json")); i++) id = $"new_mission_{i}";
        var o = new JsonObject
        {
            ["_doc"] = "Mission file (openspec/changes/mission-scripting; the format is docs/design/mission-files.md). Edit with tools/mission-editor or by hand.",
            ["id"] = id, ["name"] = "New mission", ["glyph"] = "N", ["type"] = "Patrol", ["reward_cr"] = 300,
            ["where"] = "plain && own", ["objective"] = "Destroy the raiders.", ["brief"] = "Raiders are in the sector. See them off.",
            ["battle"] = new JsonArray(new JsonObject
            {
                ["groups"] = new JsonArray(new JsonObject { ["id"] = "raiders", ["faction"] = "vos", ["count"] = "strength", ["role"] = "enemy", ["label"] = "Raiders" }),
                ["objectives"] = new JsonArray(
                    new JsonObject { ["id"] = "clear", ["text"] = "Destroy the raiders", ["primary"] = true, ["done"] = "alive(raiders) == 0" },
                    new JsonObject { ["id"] = "intact", ["text"] = "Lose no ships", ["done"] = "destroyed(fleet) == 0" }),
            }),
            ["result"] = new JsonArray(new JsonObject { ["when"] = "win", ["credits"] = "pay" }),
        };
        File.WriteAllText(Path.Combine(DataDir, "missions", id + ".json"), o.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        _file = null;
        Refresh();
        _files.SelectedItem = id + ".json";
    }
}
