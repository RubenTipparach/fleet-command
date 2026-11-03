// Geometry: the plane the battle is fought on. x to the east, z to the south (Godot's -z is north, "up" on the map), in
// metres; headings in radians clockwise from north. One copy of these helpers (CLAUDE.md section 6.1): the simulation,
// the view and the console all turn bearings and facings through them.
using System;

namespace FleetCommand.Sim;

public static class Geometry
{
    public const double Deg = Math.PI / 180.0;

    /// <summary>An angle wrapped into [-pi, pi).</summary>
    public static double Wrap(double a)
    {
        a = (a + Math.PI) % (2 * Math.PI);
        if (a < 0) a += 2 * Math.PI;
        return a - Math.PI;
    }

    /// <summary>A heading in whole degrees, 0 to 359, as the helm reads it out.</summary>
    public static int HeadingDeg(double h) => ((int)Math.Round(h / Deg) % 360 + 360) % 360;

    /// <summary>The unit vector of heading h: (x, z).</summary>
    public static (double X, double Z) Forward(double h) => (Math.Sin(h), -Math.Cos(h));

    /// <summary>The unit vector to starboard of heading h.</summary>
    public static (double X, double Z) Right(double h) => (Math.Cos(h), Math.Sin(h));

    /// <summary>The heading from (x0, z0) toward (x1, z1).</summary>
    public static double HeadingTo(double x0, double z0, double x1, double z1) => Math.Atan2(x1 - x0, -(z1 - z0));

    public static double Dist(double x0, double z0, double x1, double z1) => Math.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0));
}
