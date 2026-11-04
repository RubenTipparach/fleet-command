// StrictJson: reads one JSON object field by field and refuses anything it was not asked for (CLAUDE.md section 6.4).
// A missing optional field takes the code default, a present zero is zero, an unknown key is an error, a non-finite
// number is an error, and every error names the file and the field's path. It lives in Core so the game and the
// headless tests load data through the same rules.
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace FleetCommand.Data;

/// <summary>A data file that failed validation: the message names its path and field.</summary>
public sealed class DataError : Exception
{
    public DataError(string message) : base(message) { }
}

/// <summary>A view of one JSON object that tracks which keys were read, so unread (unknown) keys can be refused.</summary>
public sealed class StrictJson
{
    private readonly JsonElement _el;
    private readonly HashSet<string> _read = new();
    /// <summary>The file this object came from and its path inside it, for error messages.</summary>
    public string Where { get; }

    private StrictJson(JsonElement el, string where)
    {
        if (el.ValueKind != JsonValueKind.Object) throw new DataError($"{where}: expected an object");
        _el = el;
        Where = where;
        _read.Add("_doc");
    }

    /// <summary>Parses a whole file's text; <paramref name="file"/> is its path, used in errors.</summary>
    public static StrictJson Parse(string text, string file)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(text, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow });
            return new StrictJson(doc.RootElement.Clone(), file);
        }
        catch (JsonException e)
        {
            throw new DataError($"{file}: not valid JSON ({e.Message})");
        }
    }

    /// <summary>The keys of this object in file order, except _doc.</summary>
    public IEnumerable<string> Keys()
    {
        foreach (JsonProperty p in _el.EnumerateObject())
            if (p.Name != "_doc") yield return p.Name;
    }

    public bool Has(string key) => _el.TryGetProperty(key, out _);

    private JsonElement Get(string key)
    {
        _read.Add(key);
        if (!_el.TryGetProperty(key, out JsonElement v)) throw new DataError($"{Where}.{key}: missing");
        return v;
    }

    /// <summary>A required finite number.</summary>
    public double Num(string key)
    {
        JsonElement v = Get(key);
        if (v.ValueKind != JsonValueKind.Number) throw new DataError($"{Where}.{key}: expected a number");
        if (!v.TryGetDouble(out double d) || !double.IsFinite(d)) throw new DataError($"{Where}.{key}: not a finite number");
        return d;
    }

    /// <summary>An optional finite number, <paramref name="fallback"/> when absent.</summary>
    public double Num(string key, double fallback) => Has(key) ? Num(key) : Mark(key, fallback);

    /// <summary>A required whole number.</summary>
    public int Int(string key)
    {
        double d = Num(key);
        if (d != Math.Floor(d) || d < int.MinValue || d > int.MaxValue) throw new DataError($"{Where}.{key}: expected a whole number");
        return (int)d;
    }

    public string Str(string key)
    {
        JsonElement v = Get(key);
        if (v.ValueKind != JsonValueKind.String) throw new DataError($"{Where}.{key}: expected a string");
        return v.GetString() ?? "";
    }

    /// <summary>An optional string, <paramref name="fallback"/> when absent.</summary>
    public string Str(string key, string fallback) { if (Has(key)) return Str(key); _read.Add(key); return fallback; }

    /// <summary>An optional true or false, <paramref name="fallback"/> when absent.</summary>
    public bool Bool(string key, bool fallback)
    {
        if (!Has(key)) { _read.Add(key); return fallback; }
        JsonElement v = Get(key);
        if (v.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new DataError($"{Where}.{key}: expected true or false");
        return v.GetBoolean();
    }

    /// <summary>A required array of strings.</summary>
    public string[] StrList(string key)
    {
        JsonElement v = Get(key);
        if (v.ValueKind != JsonValueKind.Array) throw new DataError($"{Where}.{key}: expected an array");
        var list = new List<string>();
        int i = 0;
        foreach (JsonElement e in v.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.String) throw new DataError($"{Where}.{key}[{i}]: expected a string");
            list.Add(e.GetString() ?? "");
            i++;
        }
        return list.ToArray();
    }

    /// <summary>A nested object, itself strict.</summary>
    public StrictJson Obj(string key) => new(Get(key), $"{Where}.{key}");

    /// <summary>An optional nested object, null when absent.</summary>
    public StrictJson? ObjOrNull(string key) => Has(key) ? Obj(key) : MarkNull(key);

    /// <summary>A required array of objects.</summary>
    public List<StrictJson> ObjList(string key)
    {
        JsonElement v = Get(key);
        if (v.ValueKind != JsonValueKind.Array) throw new DataError($"{Where}.{key}: expected an array");
        var list = new List<StrictJson>();
        int i = 0;
        foreach (JsonElement e in v.EnumerateArray()) list.Add(new StrictJson(e, $"{Where}.{key}[{i++}]"));
        return list;
    }

    /// <summary>A required array of finite numbers.</summary>
    public double[] NumList(string key)
    {
        JsonElement v = Get(key);
        if (v.ValueKind != JsonValueKind.Array) throw new DataError($"{Where}.{key}: expected an array");
        var list = new List<double>();
        int i = 0;
        foreach (JsonElement e in v.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.Number || !double.IsFinite(e.GetDouble())) throw new DataError($"{Where}.{key}[{i}]: expected a finite number");
            list.Add(e.GetDouble());
            i++;
        }
        return list.ToArray();
    }

    /// <summary>A child object by key, for maps whose keys are ids (classes, weapons).</summary>
    public StrictJson Child(string key) => Obj(key);

    /// <summary>Throws if any key of this object was never read: an unknown key is an error, not ignored.</summary>
    public void Done()
    {
        foreach (JsonProperty p in _el.EnumerateObject())
            if (!_read.Contains(p.Name)) throw new DataError($"{Where}.{p.Name}: unknown key");
    }

    private double Mark(string key, double v) { _read.Add(key); return v; }
    private StrictJson? MarkNull(string key) { _read.Add(key); return null; }
}
