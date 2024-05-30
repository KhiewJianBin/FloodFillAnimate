using System;
using System.Globalization;
using UnityEngine;

public static class ColorExtension
{
    public static bool IsEqualTo(this Color32 color, Color32 other)
    {
        return Mathf.Approximately(color.r, other.r) && Mathf.Approximately(color.g, other.g) && Mathf.Approximately(color.b, other.b);
    }
    public static bool IsEqualTo(this Color color, Color other)
    {
        return Mathf.Approximately(color.r, other.r) && Mathf.Approximately(color.g, other.g) && Mathf.Approximately(color.b, other.b);
    }

    public static float Compare_Binary(Color colorMain, Color colorOther)
    {
        return (colorMain == colorOther) ? 0 : 1;
    }
    public static float Compare_Euclidean(Color colorMain, Color colorOther)
    {
        return Vector4.Magnitude(colorMain - colorOther)/ 1.7321f; //sqrt(3)
    }

    public static float Compare_LumaRec601(Color colorMain, Color colorOther)
    {
        var lumaY_colorMain = 0.299f * colorMain.r + 0.587f * colorMain.g + 0.114f * colorMain.b;
        var lumaY_colorOther = 0.299f * colorOther.r + 0.587f * colorOther.g + 0.114f * colorOther.b;
        return lumaY_colorMain - lumaY_colorOther;
    }
    public static float Compare_LumaRec709(Color colorMain, Color colorOther)
    {
        var lumaY_colorMain = 0.2126f * colorMain.r + 0.7152f * colorMain.g + 0.0722f * colorMain.b;
        var lumaY_colorOther = 0.2126f * colorOther.r + 0.7152f * colorOther.g + 0.0722f * colorOther.b;
        return lumaY_colorMain - lumaY_colorOther;
    }

    public static float Compare_DeltaE_CIE76(Color colorMain, Color colorOther)
    {
        throw new NotImplementedException();
    }

    public static void RGBToXYZ()
    {
        throw new NotImplementedException();
        //http://www.brucelindbloom.com/index.html?Eqn_DeltaE_CIE76.html
    }
    public static LabColor XYZToLAB()
    {
        throw new NotImplementedException();
        //http://www.brucelindbloom.com/index.html?Eqn_DeltaE_CIE76.html

        // conversion algorithm described here: http://www.brucelindbloom.com/index.html?Eqn_XYZ_to_Lab.html
        //double Xr = _targetWhitePoint.X, Yr = _targetWhitePoint.Y, Zr = _targetWhitePoint.Z;

        //double xr = sourceColor.X / Xr, yr = sourceColor.Y / Yr, zr = sourceColor.Z / Zr;

        //var fx = f(xr);
        //var fy = f(yr);
        //var fz = f(zr);

        //var L = 116 * fy - 16;
        //var a = 500 * (fx - fy);
        //var b = 200 * (fy - fz);

        //var targetColor = new LabColor(in L, in a, in b);
        //return targetColor;

        //private static double f(double cr)
        //{
        //    var fc = cr > Epsilon ? Pow(cr, 1 / 3d) : (Kappa * cr + 16) / 116d;
        //    return fc;
        //}
    }
}

public struct LabColor
{
    /// <summary>
    /// L* (Lightness 0 - 100)
    /// </summary>
    public readonly float L;

    /// <summary>
    /// a* (Red/Green from -100 to 100)
    /// </summary>
    public readonly float a;

    /// <summary>
    /// b* (Blue/Yellow from -100 to 100)
    /// </summary>
    public readonly float b;

    /// <param name="l">L* (Lightness 0 - 100)</param>
    /// <param name="a">a* (Red/Green from -100 to 100)</param>
    /// <param name="b">b* (Blue/Yellow from -100 to 100)</param>
    public LabColor(float L, float a, float b)
    {
        this.L = L;
        this.a = a;
        this.b = b;
    }

    public override string ToString() => string.Format(CultureInfo.InvariantCulture, "Lab [L={0:0.##}, a={1:0.##}, b={2:0.##}]", L, a, b);
}