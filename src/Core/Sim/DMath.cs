// DMath: the simulation's sine, cosine, arc tangent, exponential and logarithm, the same to the last bit on every machine
// (openspec/changes/bridge-crew-coop, design "Lockstep"). Co-op steps one battle on several machines from the same orders,
// so the battle must come out bit for bit the same everywhere; System.Math's transcendental functions call the platform's
// C library, which rounds differently on Windows x64 and macOS ARM (measured: the determinism probe's checksums parted on
// 13 of 51 scenarios between the owner's laptop and the Mac). These are fdlibm's algorithms (Sun Microsystems, 1993: "Permission
// to use, copy, modify, and distribute this software is freely granted, provided that this notice is preserved"), reduced
// to plain IEEE arithmetic (+, -, *, /, and the exactly rounded Sqrt, Floor, Round), which every machine computes alike;
// accurate to about one unit in the last place. Every transcendental call in src/Core goes through here; a test checks
// none goes to System.Math directly.
using System;

namespace FleetCommand.Sim;

public static class DMath
{
    public const double PI = Math.PI;

    // pi/2 in three parts, for the reduction of an angle to [-pi/4, pi/4]
    private const double Pio2_1 = 1.57079632673412561417e+00, Pio2_2 = 6.07710050630396597660e-11, Pio2_3 = 2.02226624871116645580e-21;
    private const double InvPio2 = 6.36619772367581382433e-01;

    private const double S1 = -1.66666666666666324348e-01, S2 = 8.33333333332248946124e-03, S3 = -1.98412698298579493134e-04,
        S4 = 2.75573137070700676789e-06, S5 = -2.50507602534068634195e-08, S6 = 1.58969099521155010221e-10;
    private const double C1 = 4.16666666666666019037e-02, C2 = -1.38888888888741095749e-03, C3 = 2.48015872894767294178e-05,
        C4 = -2.75573143513906633035e-07, C5 = 2.08757232129817482790e-09, C6 = -1.13596475577881948265e-11;

    private static double KSin(double x)
    {
        double z = x * x, v = z * x, r = S2 + z * (S3 + z * (S4 + z * (S5 + z * S6)));
        return x + v * (S1 + z * r);
    }

    private static double KCos(double x)
    {
        double z = x * x, r = z * (C1 + z * (C2 + z * (C3 + z * (C4 + z * (C5 + z * C6)))));
        return 1.0 - (0.5 * z - z * r);
    }

    // x reduced to r in [-pi/4, pi/4] and the quadrant n (0 to 3)
    private static (double R, int N) Reduce(double x)
    {
        double k = Math.Round(x * InvPio2);
        double r = ((x - k * Pio2_1) - k * Pio2_2) - k * Pio2_3;
        int n = (int)(((long)k % 4 + 4) % 4);
        return (r, n);
    }

    public static double Sin(double x)
    {
        if (!double.IsFinite(x)) return double.NaN;
        if (Math.Abs(x) < 7.85398163397448278999e-01) return KSin(x);
        var (r, n) = Reduce(x);
        return n switch { 0 => KSin(r), 1 => KCos(r), 2 => -KSin(r), _ => -KCos(r) };
    }

    public static double Cos(double x)
    {
        if (!double.IsFinite(x)) return double.NaN;
        if (Math.Abs(x) < 7.85398163397448278999e-01) return KCos(x);
        var (r, n) = Reduce(x);
        return n switch { 0 => KCos(r), 1 => -KSin(r), 2 => -KCos(r), _ => KSin(r) };
    }

    private static readonly double[] AtanHi = { 4.63647609000806093515e-01, 7.85398163397448278999e-01, 9.82793723247329054082e-01, 1.57079632679489655800e+00 };
    private static readonly double[] AtanLo = { 2.26987774529616870924e-17, 3.06161699786838301793e-17, 1.39033110312309984516e-17, 6.12323399573676603587e-17 };
    private static readonly double[] AT =
    {
        3.33333333333329318027e-01, -1.99999999998764832476e-01, 1.42857142725034663711e-01, -1.11111104054623557880e-01,
        9.09088713343650656196e-02, -7.69187620504482999495e-02, 6.66107313738753120669e-02, -5.83357013379057348645e-02,
        4.97687799461593236017e-02, -3.65315727442169155270e-02, 1.62858201153657823623e-02,
    };

    public static double Atan(double x)
    {
        if (double.IsNaN(x)) return x;
        double sign = x < 0 ? -1 : 1, ax = Math.Abs(x);
        if (ax >= 7.378697629483821e19) return sign * (AtanHi[3] + AtanLo[3]);
        int id;
        if (ax < 0.4375) { if (ax < 1e-29) return x; id = -1; }
        else if (ax < 1.1875)
        {
            if (ax < 0.6875) { id = 0; ax = (2.0 * ax - 1.0) / (2.0 + ax); }
            else { id = 1; ax = (ax - 1.0) / (ax + 1.0); }
        }
        else if (ax < 2.4375) { id = 2; ax = (ax - 1.5) / (1.0 + 1.5 * ax); }
        else { id = 3; ax = -1.0 / ax; }
        double z = ax * ax, w = z * z;
        double s1 = z * (AT[0] + w * (AT[2] + w * (AT[4] + w * (AT[6] + w * (AT[8] + w * AT[10])))));
        double s2 = w * (AT[1] + w * (AT[3] + w * (AT[5] + w * (AT[7] + w * AT[9]))));
        if (id < 0) return sign * (ax - ax * (s1 + s2));
        double r = AtanHi[id] - ((ax * (s1 + s2) - AtanLo[id]) - ax);
        return sign * r;
    }

    /// <summary>The angle of (x, y) from the x axis, in (-pi, pi], as Math.Atan2(y, x).</summary>
    public static double Atan2(double y, double x)
    {
        if (double.IsNaN(x) || double.IsNaN(y)) return double.NaN;
        if (x == 0) return y > 0 ? PI / 2 : y < 0 ? -PI / 2 : 0;
        if (double.IsInfinity(x) || double.IsInfinity(y))
        {
            if (double.IsInfinity(y)) return (y > 0 ? 1 : -1) * (double.IsInfinity(x) ? (x > 0 ? PI / 4 : 3 * PI / 4) : PI / 2);
            return x > 0 ? 0 * y : (y >= 0 ? PI : -PI);
        }
        double a = Atan(Math.Abs(y / x));
        if (x > 0) return y < 0 ? -a : a;
        return y < 0 ? -(PI - a) : PI - a;
    }

    private const double Ln2Hi = 6.93147180369123816490e-01, Ln2Lo = 1.90821492927058770002e-10, InvLn2 = 1.44269504088896338700e+00;
    private const double P1 = 1.66666666666666019037e-01, P2 = -2.77777777770155933842e-03, P3 = 6.61375632143793436117e-05,
        P4 = -1.65339022054652515390e-06, P5 = 4.13813679705723846039e-08;

    // y times 2^k, exactly
    private static double Scale(double y, int k)
    {
        while (k > 1023) { y *= BitConverter.Int64BitsToDouble(0x7FE0000000000000); k -= 1023; }
        while (k < -1022) { y *= BitConverter.Int64BitsToDouble(0x0010000000000000); k += 1022; }
        return y * BitConverter.Int64BitsToDouble((long)(1023 + k) << 52);
    }

    public static double Exp(double x)
    {
        if (double.IsNaN(x)) return x;
        if (x > 709.782712893383973096) return double.PositiveInfinity;
        if (x < -745.13321910194110842) return 0;
        if (Math.Abs(x) < 3.7252902984e-09) return 1.0 + x;
        double k = Math.Round(x * InvLn2);
        double hi = x - k * Ln2Hi, lo = k * Ln2Lo, r = hi - lo;
        double t = r * r, c = r - t * (P1 + t * (P2 + t * (P3 + t * (P4 + t * P5))));
        double y = 1.0 - ((lo - (r * c) / (2.0 - c)) - hi);
        return Scale(y, (int)k);
    }

    private const double Lg1 = 6.666666666666735130e-01, Lg2 = 3.999999999940941908e-01, Lg3 = 2.857142874366239149e-01,
        Lg4 = 2.222219843214978396e-01, Lg5 = 1.818357216161805012e-01, Lg6 = 1.531383769920937332e-01, Lg7 = 1.479819860511658591e-01;

    public static double Log(double x)
    {
        if (double.IsNaN(x) || x < 0) return double.NaN;
        if (x == 0) return double.NegativeInfinity;
        if (double.IsPositiveInfinity(x)) return x;
        int k = 0;
        if (x < 2.2250738585072014e-308) { x *= 18014398509481984.0; k = -54; }   // a subnormal, scaled by 2^54
        long bits = BitConverter.DoubleToInt64Bits(x);
        k += (int)((bits >> 52) & 0x7FF) - 1023;
        double m = BitConverter.Int64BitsToDouble((bits & 0x000FFFFFFFFFFFFFL) | 0x3FF0000000000000L);   // in [1, 2)
        if (m > 1.41421356237309504880) { m *= 0.5; k++; }
        double f = m - 1.0, s = f / (2.0 + f), z = s * s, w = z * z;
        double t1 = w * (Lg2 + w * (Lg4 + w * Lg6)), t2 = z * (Lg1 + w * (Lg3 + w * (Lg5 + w * Lg7)));
        double R = t2 + t1, hfsq = 0.5 * f * f;
        return k * Ln2Hi - ((hfsq - (s * (hfsq + R) + k * Ln2Lo)) - f);
    }

    /// <summary>x to the power y: exact for a whole power (by squaring), else exp(y log x) for x above 0.</summary>
    public static double Pow(double x, double y)
    {
        if (y == 0) return 1;
        if (y == Math.Floor(y) && Math.Abs(y) <= 1 << 30)
        {
            long n = (long)Math.Abs(y);
            double r = 1, b = x;
            while (n > 0) { if ((n & 1) != 0) r *= b; b *= b; n >>= 1; }
            return y < 0 ? 1 / r : r;
        }
        if (x < 0) return double.NaN;
        if (x == 0) return y > 0 ? 0 : double.PositiveInfinity;
        return Exp(y * Log(x));
    }
}
