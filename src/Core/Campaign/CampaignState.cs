// CampaignState: everything a campaign is, as plain data that saves to JSON and loads back (the game keeps it in user://,
// the mockup in localStorage). No rules here: Campaign.cs changes it. HexGrid is the map's geometry, and Mulberry32 the
// campaign's random numbers: the same generator as the campaign mockup's rng(), so a campaign built from the same seed
// draws the same map in the page and in the game.
using System;
using System.Collections.Generic;

namespace FleetCommand.Campaign;

/// <summary>The campaign's seeded random numbers: mulberry32, as the mockup's rng() (docs/mockups/game-flow.html). Its
/// whole state is one 32-bit number, saved with the campaign so a loaded game draws on from where it stopped.</summary>
public sealed class Mulberry32
{
    public uint State { get; private set; }
    public Mulberry32(uint state) { State = state; }

    /// <summary>A double in [0, 1).</summary>
    public double Next()
    {
        unchecked
        {
            State += 0x6d2b79f5;
            uint t = State;
            t = (t ^ (t >> 15)) * (t | 1);
            t ^= t + (t ^ (t >> 7)) * (t | 61);
            return (t ^ (t >> 14)) / 4294967296.0;
        }
    }

    /// <summary>A whole number in [0, n).</summary>
    public int Below(int n) => n <= 1 ? 0 : (int)Math.Floor(Next() * n);
}

/// <summary>Flat-topped hexes in odd-q offset columns (the campaign design's map): positions, distance and neighbours.</summary>
public static class HexGrid
{
    public const double Sqrt3 = 1.7320508075688772;
    /// <summary>A sector's centre on the map plane, in hex radii: x across, z down the map.</summary>
    public static (double X, double Z) Pos(int c, int r) => (1.5 * c, Sqrt3 * (r + 0.5 * (c & 1)));

    private static (int, int, int) Cube(int c, int r) { int x = c, z = r - (c - (c & 1)) / 2; return (x, -x - z, z); }

    public static int Dist(int ac, int ar, int bc, int br)
    {
        var (ax, ay, az) = Cube(ac, ar);
        var (bx, by, bz) = Cube(bc, br);
        return Math.Max(Math.Abs(ax - bx), Math.Max(Math.Abs(ay - by), Math.Abs(az - bz)));
    }

    private static readonly (int, int)[] Odd = { (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (0, -1) };
    private static readonly (int, int)[] Even = { (1, -1), (1, 0), (0, 1), (-1, 0), (-1, -1), (0, -1) };

    /// <summary>The sectors next to (c, r) inside a cols x rows map, in a fixed order.</summary>
    public static IEnumerable<(int C, int R)> Neighbours(int c, int r, int cols, int rows)
    {
        foreach (var (dc, dr) in (c & 1) == 1 ? Odd : Even)
        {
            int nc = c + dc, nr = r + dr;
            if (nc >= 0 && nr >= 0 && nc < cols && nr < rows) yield return (nc, nr);
        }
    }

    /// <summary>The sector whose hex holds a point of the map plane (the inverse of Pos), whether or not it is on the map.</summary>
    public static (int C, int R) At(double x, double z)
    {
        double q = 2.0 / 3 * x, r = -1.0 / 3 * x + Sqrt3 / 3 * z;
        double cx = q, cz = r, cy = -cx - cz;
        double rx = Math.Round(cx), ry = Math.Round(cy), rz = Math.Round(cz);
        double dx = Math.Abs(rx - cx), dy = Math.Abs(ry - cy), dz = Math.Abs(rz - cz);
        if (dx > dy && dx > dz) rx = -ry - rz;
        else if (dy <= dz) rz = -rx - ry;
        int col = (int)rx, row = (int)rz + (col - (col & 1)) / 2;
        return (col, row);
    }

    /// <summary>The heading from one sector to the next, radians about the vertical (a standee's bow is +z).</summary>
    public static double Heading(int ac, int ar, int bc, int br)
    {
        var (ax, az) = Pos(ac, ar);
        var (bx, bz) = Pos(bc, br);
        return Math.Atan2(bx - ax, bz - az);
    }
}

public sealed class Sector
{
    public int C { get; set; }
    public int R { get; set; }
    public string Owner { get; set; } = "neutral";
    public string Terrain { get; set; } = "open";
    public string? Planet { get; set; }
    public string? Station { get; set; }
    public bool IsPort => Planet != null || Station != null;
    public string? PortName => Planet ?? Station;
    /// <summary>The fog of war: true once the sector has been seen (revealed); an unexplored sector is drawn black.</summary>
    public bool Known { get; set; }
    /// <summary>Who held the sector when it was last seen: a revealed sector shows its empire as last known.</summary>
    public string KnownOwner { get; set; } = "neutral";
}

/// <summary>A faction's fleet on the map, moved by its faction every few turns toward a goal near its home.</summary>
public sealed class FactionFleet
{
    public string Name { get; set; } = "";
    public string Fac { get; set; } = "";
    public int C { get; set; }
    public int R { get; set; }
    public int Size { get; set; }
    public bool Commander { get; set; }
    public int HomeC { get; set; }
    public int HomeR { get; set; }
    /// <summary>It moves on turns where (turn + Every) % Every is 0.</summary>
    public int Every { get; set; } = 1;
    public double Dir { get; set; }
    public int? GoalC { get; set; }
    public int? GoalR { get; set; }
}

/// <summary>A freighter running between two of its faction's ports, one sector a turn, turning back at each end. One that
/// was destroyed or taken is off the route for Out turns (rules.freighter_refit_turns), then sails again from where she was.</summary>
public sealed class Freighter
{
    public string Fac { get; set; } = "";
    public string Name { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public List<int[]> Route { get; set; } = new();
    public int I { get; set; }
    public int Step { get; set; } = 1;
    public double Dir { get; set; }
    public int Out { get; set; }
    /// <summary>Her sector now.</summary>
    public (int C, int R) Here() => (Route[I][0], Route[I][1]);
}

/// <summary>One ship of the player's fleet: its hull, fittings by hardpoint group, supplies, and hull left (percent).</summary>
public sealed class CampShip
{
    public int Id { get; set; }
    public string Hull { get; set; } = "";
    public string Maker { get; set; } = "";
    public string Name { get; set; } = "";
    public Dictionary<string, List<string?>> Fit { get; set; } = new();
    public Dictionary<string, int> Supplies { get; set; } = new();
    public string Behaviour { get; set; } = "Escort";
    public double HullPct { get; set; } = 100;
    /// <summary>A freighter taken with her cargo: what her cargo pays when cashed out at a port (Campaign.CashOut), credits;
    /// 0 for every other ship. A ship with a haul sails with the fleet but stays out of its battles.</summary>
    public int Haul { get; set; }
}

/// <summary>A mission on offer (or accepted): its script, sector, turns left (null for a story mission), strength, reward
/// and the values its setup rolled (strings and numbers kept apart so they save exactly).</summary>
public sealed class Offer
{
    public int Id { get; set; }
    public string Script { get; set; } = "";
    public int C { get; set; }
    public int R { get; set; }
    public int? Turns { get; set; }
    public string Strength { get; set; } = "Light";
    public double Reward { get; set; }
    public Dictionary<string, string> Text { get; set; } = new();
    public Dictionary<string, double> Num { get; set; } = new();
    /// <summary>The faction fleet this mission goes with, when it follows one.</summary>
    public string? Group { get; set; }

    public object? Value(string k) => Text.TryGetValue(k, out string? s) ? s : Num.TryGetValue(k, out double d) ? d : null;
    public void Set(string k, object v)
    {
        Text.Remove(k);
        Num.Remove(k);
        if (v is double d) Num[k] = d; else Text[k] = (string)v;
    }
}

public sealed class Officer
{
    public string Name { get; set; } = "";
    public string Station { get; set; } = "";
    public string Skill { get; set; } = "";
    public int Cost { get; set; }
}

/// <summary>A finished mission's record for the log and the debrief: the grade, what it paid, prestige and standing.</summary>
public sealed class Debrief
{
    public string Mission { get; set; } = "";
    public string Grade { get; set; } = "";
    public bool Win { get; set; }
    public string How { get; set; } = "";
    public int Credits { get; set; }
    public int Prestige { get; set; }
    public int PrestigeNow { get; set; }
    public List<(string Fac, int Delta, int Now)> Standing { get; set; } = new();
    public List<(string Text, bool Primary, bool Met)> Objectives { get; set; } = new();
    public List<(string Ship, string Fate)> Ships { get; set; } = new();
    public string? Log { get; set; }
}

public sealed class CampaignState
{
    public int Version { get; set; } = 1;
    public string Side { get; set; } = "";
    public uint Seed { get; set; }
    public uint Rng { get; set; }
    public int Cols { get; set; }
    public int Rows { get; set; }
    /// <summary>Every sector, column-major: index c * Rows + r.</summary>
    public List<Sector> Sectors { get; set; } = new();
    public List<FactionFleet> Groups { get; set; } = new();
    public List<Freighter> Freighters { get; set; } = new();
    public List<CampShip> Fleet { get; set; } = new();
    public int AtC { get; set; }
    public int AtR { get; set; }
    public int HomeC { get; set; }
    public int HomeR { get; set; }
    public int Credits { get; set; }
    public int Turn { get; set; } = 1;
    public int Mp { get; set; }
    public List<Offer> Missions { get; set; } = new();
    public List<int> Active { get; set; } = new();
    public List<string> Log { get; set; } = new();
    public List<Officer> Crew { get; set; } = new();
    public List<Officer> OfficerPool { get; set; } = new();
    /// <summary>Systems bought and not fitted, by system id: fitting takes from here for free.</summary>
    public Dictionary<string, int> Stores { get; set; } = new();
    public int Mid { get; set; }
    public int ShipSerial { get; set; }
    public string Captain { get; set; } = "Captain Ardent";
    public int Battles { get; set; }
    public Dictionary<string, int> Standing { get; set; } = new();
    public double Dir { get; set; }
    public int Prestige { get; set; }
    public int? OrderC { get; set; }
    public int? OrderR { get; set; }

    public Sector Hex(int c, int r) => Sectors[c * Rows + r];
}
