// Expr: the small expression language mission files are written in (openspec/changes/mission-scripting; owner, 2026-11-05:
// "start building out mission scripting so we can write up mission files outside of godot"). A mission file is data, so
// its few rules (where a contract is offered, how many raiders, whether an objective is met, what a grade pays) are
// expressions in strings, parsed once when the file loads and evaluated against a scope the campaign or the battle gives.
// One implementation: the game, the tests and the mission editor all parse and evaluate through this file.
//
// The language: numbers, 'strings' (single or double quotes), true and false (1 and 0), names (looked up in the scope),
// calls name(a, b), the operators ! - * / % + - < <= > >= == != && || and a ? b : c, and parentheses. A condition is true
// when it is a non-zero number or a non-empty string. Templates are text with {expression} parts, for names in briefings.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using FleetCommand.Data;

namespace FleetCommand.Campaign;

/// <summary>What an expression reads: values by name and functions by name. Unknown names throw a DataError.</summary>
public interface IScope
{
    object Get(string name);
    object Call(string name, IReadOnlyList<object> args);
}

/// <summary>A parsed expression. Values are double or string; true is 1 and false is 0.</summary>
public abstract class Expr
{
    /// <summary>The source text, for errors.</summary>
    public string Text { get; private set; } = "";

    public abstract object Eval(IScope s);

    /// <summary>Every name and function this expression reads, so a loader can refuse a name the scope will not have.</summary>
    public abstract void Names(ICollection<string> vars, ICollection<string> calls);

    /// <summary>The text arguments written in every call of a function (reached('beacon') gives beacon), so a loader can
    /// check they name something that exists.</summary>
    public virtual IEnumerable<string> Calls(string fn) => Array.Empty<string>();

    public double Num(IScope s) => ToNum(Eval(s), Text);
    public bool Bool(IScope s) => Truthy(Eval(s));
    public string Str(IScope s) => Format(Eval(s));

    public static bool Truthy(object v) => v is double d ? d != 0 : v is string t && t.Length > 0;
    public static double ToNum(object v, string where) =>
        v is double d ? d : throw new DataError($"expression '{where}': expected a number, got '{v}'");
    /// <summary>A value as text: whole numbers without a decimal point.</summary>
    public static string Format(object v) => v switch
    {
        double d when d == Math.Floor(d) && Math.Abs(d) < 1e15 => ((long)d).ToString(CultureInfo.InvariantCulture),
        double d => d.ToString("0.##", CultureInfo.InvariantCulture),
        _ => (string)v,
    };

    /// <summary>Parses an expression; a syntax error throws a DataError naming <paramref name="where"/>.</summary>
    public static Expr Parse(string text, string where)
    {
        var p = new Parser(text, where);
        Expr e = p.Ternary();
        p.End();
        e.Text = text;
        return e;
    }

    /// <summary>A constant, for fields written as a JSON number.</summary>
    public static Expr Const(double v) => new Lit(v) { Text = v.ToString(CultureInfo.InvariantCulture) };

    private sealed class Lit : Expr
    {
        private readonly object _v;
        public Lit(object v) { _v = v; }
        public override object Eval(IScope s) => _v;
        public override void Names(ICollection<string> vars, ICollection<string> calls) { }
    }

    private sealed class Name : Expr
    {
        private readonly string _n;
        public Name(string n) { _n = n; }
        public override object Eval(IScope s) => s.Get(_n);
        public override void Names(ICollection<string> vars, ICollection<string> calls) => vars.Add(_n);
    }

    private sealed class Call : Expr
    {
        private readonly string _n;
        private readonly List<Expr> _args;
        public Call(string n, List<Expr> args) { _n = n; _args = args; }
        public override object Eval(IScope s)
        {
            var a = new List<object>(_args.Count);
            foreach (Expr e in _args) a.Add(e.Eval(s));
            return Builtin(_n, a) ?? s.Call(_n, a);
        }
        public override void Names(ICollection<string> vars, ICollection<string> calls)
        {
            if (!Builtins.Contains(_n)) calls.Add(_n);
            foreach (Expr e in _args) e.Names(vars, calls);
        }
        public override IEnumerable<string> Calls(string fn)
        {
            var found = _args.SelectMany(a => a.Calls(fn)).ToList();
            if (_n == fn) found.AddRange(_args.OfType<Lit>().Select(l => Format(l.Eval(null!))));
            return found;
        }
    }

    /// <summary>The functions every scope has: min, max, floor, ceil, round, abs, and round10 (to the nearest 10, as credits are paid).</summary>
    public static readonly HashSet<string> Builtins = new() { "min", "max", "floor", "ceil", "round", "abs", "round10" };

    private static object? Builtin(string n, List<object> a)
    {
        double A(int i) => i < a.Count ? ToNum(a[i], n) : throw new DataError($"{n}(): needs {i + 1} arguments");
        return n switch
        {
            "min" => Math.Min(A(0), A(1)),
            "max" => Math.Max(A(0), A(1)),
            "floor" => Math.Floor(A(0)),
            "ceil" => Math.Ceiling(A(0)),
            "round" => Math.Round(A(0), MidpointRounding.AwayFromZero),
            "abs" => Math.Abs(A(0)),
            "round10" => Math.Round(A(0) / 10, MidpointRounding.AwayFromZero) * 10,
            _ => null,
        };
    }

    private sealed class Unary : Expr
    {
        private readonly char _op;
        private readonly Expr _a;
        public Unary(char op, Expr a) { _op = op; _a = a; }
        public override object Eval(IScope s) => _op == '!' ? (Truthy(_a.Eval(s)) ? 0.0 : 1.0) : -ToNum(_a.Eval(s), _a.Text);
        public override void Names(ICollection<string> vars, ICollection<string> calls) => _a.Names(vars, calls);
        public override IEnumerable<string> Calls(string fn) => _a.Calls(fn);
    }

    private sealed class Binary : Expr
    {
        private readonly string _op;
        private readonly Expr _a, _b;
        public Binary(string op, Expr a, Expr b) { _op = op; _a = a; _b = b; }
        public override object Eval(IScope s)
        {
            if (_op == "&&") return Truthy(_a.Eval(s)) && Truthy(_b.Eval(s)) ? 1.0 : 0.0;
            if (_op == "||") { object l = _a.Eval(s); return Truthy(l) ? l : _b.Eval(s); }
            object x = _a.Eval(s), y = _b.Eval(s);
            if (_op is "==" or "!=")
            {
                bool eq = x is double dx && y is double dy ? dx == dy : Equals(x, y);
                return eq == (_op == "==") ? 1.0 : 0.0;
            }
            if (_op == "+" && (x is string || y is string)) return Format(x) + Format(y);
            double p = ToNum(x, _a.Text), q = ToNum(y, _b.Text);
            return _op switch
            {
                "+" => p + q, "-" => p - q, "*" => p * q,
                "/" => q == 0 ? throw new DataError($"expression '{Text}': division by zero") : p / q,
                "%" => q == 0 ? throw new DataError($"expression '{Text}': division by zero") : p % q,
                "<" => p < q ? 1.0 : 0.0, "<=" => p <= q ? 1.0 : 0.0, ">" => p > q ? 1.0 : 0.0, ">=" => p >= q ? 1.0 : 0.0,
                _ => throw new DataError($"expression '{Text}': unknown operator {_op}"),
            };
        }
        public override void Names(ICollection<string> vars, ICollection<string> calls) { _a.Names(vars, calls); _b.Names(vars, calls); }
        public override IEnumerable<string> Calls(string fn) => _a.Calls(fn).Concat(_b.Calls(fn));
    }

    private sealed class Cond : Expr
    {
        private readonly Expr _c, _a, _b;
        public Cond(Expr c, Expr a, Expr b) { _c = c; _a = a; _b = b; }
        public override object Eval(IScope s) => Truthy(_c.Eval(s)) ? _a.Eval(s) : _b.Eval(s);
        public override void Names(ICollection<string> vars, ICollection<string> calls) { _c.Names(vars, calls); _a.Names(vars, calls); _b.Names(vars, calls); }
        public override IEnumerable<string> Calls(string fn) => _c.Calls(fn).Concat(_a.Calls(fn)).Concat(_b.Calls(fn));
    }

    private sealed class Parser
    {
        private readonly string _t, _where;
        private int _i;
        public Parser(string t, string where) { _t = t; _where = where; }

        private DataError Err(string what) => new($"{_where}: {what} at {_i + 1} in '{_t}'");
        private void Ws() { while (_i < _t.Length && char.IsWhiteSpace(_t[_i])) _i++; }
        private bool Eat(string op)
        {
            Ws();
            if (string.CompareOrdinal(_t, _i, op, 0, op.Length) != 0) return false;
            // never take '<' from '<=', '=' from '==', '!' from '!=', '&' or '|' alone
            if (op.Length == 1 && _i + 1 < _t.Length && "=<>!".Contains(op[0]) && _t[_i + 1] == '=') return false;
            _i += op.Length;
            return true;
        }
        public void End() { Ws(); if (_i < _t.Length) throw Err("unexpected text"); }

        public Expr Ternary()
        {
            Expr c = Or();
            if (!Eat("?")) return c;
            Expr a = Ternary();
            if (!Eat(":")) throw Err("expected ':'");
            return new Cond(c, a, Ternary());
        }
        private Expr Or() { Expr a = And(); while (Eat("||")) a = new Binary("||", a, And()); return a; }
        private Expr And() { Expr a = Eq(); while (Eat("&&")) a = new Binary("&&", a, Eq()); return a; }
        private Expr Eq()
        {
            Expr a = Rel();
            while (true)
            {
                if (Eat("==")) a = new Binary("==", a, Rel());
                else if (Eat("!=")) a = new Binary("!=", a, Rel());
                else return a;
            }
        }
        private Expr Rel()
        {
            Expr a = Add();
            while (true)
            {
                if (Eat("<=")) a = new Binary("<=", a, Add());
                else if (Eat(">=")) a = new Binary(">=", a, Add());
                else if (Eat("<")) a = new Binary("<", a, Add());
                else if (Eat(">")) a = new Binary(">", a, Add());
                else return a;
            }
        }
        private Expr Add()
        {
            Expr a = Mul();
            while (true)
            {
                if (Eat("+")) a = new Binary("+", a, Mul());
                else if (Eat("-")) a = new Binary("-", a, Mul());
                else return a;
            }
        }
        private Expr Mul()
        {
            Expr a = Unary();
            while (true)
            {
                if (Eat("*")) a = new Binary("*", a, Unary());
                else if (Eat("/")) a = new Binary("/", a, Unary());
                else if (Eat("%")) a = new Binary("%", a, Unary());
                else return a;
            }
        }
        private Expr Unary()
        {
            if (Eat("!")) return new Unary('!', Unary());
            if (Eat("-")) return new Unary('-', Unary());
            return Primary();
        }
        private Expr Primary()
        {
            Ws();
            if (_i >= _t.Length) throw Err("expected a value");
            char ch = _t[_i];
            if (ch == '(')
            {
                _i++;
                Expr e = Ternary();
                if (!Eat(")")) throw Err("expected ')'");
                return e;
            }
            if (ch is '\'' or '"')
            {
                int end = _t.IndexOf(ch, _i + 1);
                if (end < 0) throw Err("unclosed string");
                string s = _t.Substring(_i + 1, end - _i - 1);
                _i = end + 1;
                return new Lit(s);
            }
            if (char.IsDigit(ch) || ch == '.')
            {
                int st = _i;
                while (_i < _t.Length && (char.IsDigit(_t[_i]) || _t[_i] == '.')) _i++;
                if (!double.TryParse(_t.AsSpan(st, _i - st), NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) throw Err("bad number");
                return new Lit(d);
            }
            if (char.IsLetter(ch) || ch == '_')
            {
                int st = _i;
                while (_i < _t.Length && (char.IsLetterOrDigit(_t[_i]) || _t[_i] is '_' or '.')) _i++;
                string n = _t.Substring(st, _i - st);
                if (n == "true") return new Lit(1.0);
                if (n == "false") return new Lit(0.0);
                if (!Eat("(")) return new Name(n);
                var args = new List<Expr>();
                if (!Eat(")"))
                {
                    do args.Add(Ternary()); while (Eat(","));
                    if (!Eat(")")) throw Err("expected ')'");
                }
                return new Call(n, args);
            }
            throw Err($"unexpected '{ch}'");
        }
    }
}

/// <summary>Text with {expression} parts, for names and numbers in briefings and labels. "{{" is a literal brace.</summary>
public sealed class Template
{
    private readonly List<object> _parts = new(); // string or Expr
    public string Text { get; }

    private Template(string text) { Text = text; }

    public static Template Parse(string text, string where)
    {
        var t = new Template(text);
        var sb = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '{' && i + 1 < text.Length && text[i + 1] == '{') { sb.Append('{'); i++; continue; }
            if (c == '}' && i + 1 < text.Length && text[i + 1] == '}') { sb.Append('}'); i++; continue; }
            if (c != '{') { sb.Append(c); continue; }
            int end = text.IndexOf('}', i);
            if (end < 0) throw new DataError($"{where}: unclosed '{{' in '{text}'");
            if (sb.Length > 0) { t._parts.Add(sb.ToString()); sb.Clear(); }
            t._parts.Add(Expr.Parse(text.Substring(i + 1, end - i - 1), where));
            i = end;
        }
        if (sb.Length > 0) t._parts.Add(sb.ToString());
        return t;
    }

    public string Eval(IScope s)
    {
        var sb = new StringBuilder();
        foreach (object p in _parts) sb.Append(p is Expr e ? e.Str(s) : (string)p);
        return sb.ToString();
    }

    public void Names(ICollection<string> vars, ICollection<string> calls)
    {
        foreach (object p in _parts) if (p is Expr e) e.Names(vars, calls);
    }
}
