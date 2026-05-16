using System.Diagnostics.CodeAnalysis;
using Silk.NET.Maths;
namespace Units;

public struct Vector2
{
    Vector2D<float> xyPx;
    Vector2D<float> xy;

    public float X => xyPx.X;
    public UnitType XType;
    public float Y => xyPx.Y;
    public UnitType YType;

    public Vector2(float x, float y) : this(x, y, UnitType.px, UnitType.px)
    {
    }

    public Vector2(float x, float y, UnitType xType = UnitType.px, UnitType yType = UnitType.px)
    {
        xy.X = x;
        XType = x==0 ? UnitType.px : xType;

        xy.Y = y;
        YType = y==0 ? UnitType.px : yType;

        ConvertToPx();
    }

    public void ConvertToPx()
    {
        xyPx = new Vector2D<float>(xy.X * UnitsConverter.Get(XType), xy.Y * UnitsConverter.Get(YType));
    }

    public static Vector2 operator +(Vector2 a, Vector2 b)
    {
        var xy = a.xyPx + b.xyPx;

        return new Vector2(xy.X, xy.Y);
    }

    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
        var xy = a.xyPx - b.xyPx;

        return new Vector2(xy.X, xy.Y);
    }

    public static bool operator ==(Vector2 a, Vector2 b)
    {
        return a.xyPx == b.xyPx;
    }
    public static bool operator !=(Vector2 a, Vector2 b)
    {
        return a.xyPx != b.xyPx;
    }

    public override int GetHashCode()
    {
        return xyPx.GetHashCode();
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is Vector2 vector2)
            return xyPx.Equals(vector2.xyPx);
        if (obj is Vector2D<float> vector2D)
            return xyPx.Equals(vector2D);
        return false;
    }
}