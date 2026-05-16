using System.Diagnostics.CodeAnalysis;
using Silk.NET.Maths;

namespace Units;

public struct Vector3
{
    Vector3D<float> xyzPx;
    Vector3D<float> xyz;

    public float X => xyzPx.X;
    public UnitType XType;
    public float Y => xyzPx.Y;
    public UnitType YType;
    public float Z => xyzPx.Z;
    public UnitType ZType;

    public Vector3(float x, float y, float z) : this(x, y, z, UnitType.px, UnitType.px, UnitType.px)
    {
    }

    public Vector3(float x, float y, float z, UnitType xType = UnitType.px, UnitType yType = UnitType.px, UnitType zType = UnitType.px)
    {
        xyz.X = x;
        XType = x==0 ? UnitType.px : xType;

        xyz.Y = y;
        YType = y==0 ? UnitType.px : yType;

        xyz.Z = z;
        ZType = z==0 ? UnitType.px : zType;

        ConvertToPx();
    }

    public void ConvertToPx()
    {
        xyzPx = new Vector3D<float>(xyz.X * UnitsConverter.Get(XType), xyz.Y * UnitsConverter.Get(YType), xyz.Z * UnitsConverter.Get(ZType));
    }

    public static Vector3 operator +(Vector3 a, Vector3 b)
    {
        var xyz = a.xyzPx + b.xyzPx;

        return new Vector3(xyz.X, xyz.Y, xyz.Z);
    }

    public static Vector3 operator -(Vector3 a, Vector3 b)
    {
        var xyz = a.xyzPx - b.xyzPx;

        return new Vector3(xyz.X, xyz.Y, xyz.Z);
    }

    public static bool operator ==(Vector3 a, Vector3 b)
    {
        return a.xyzPx == b.xyzPx;
    }
    public static bool operator !=(Vector3 a, Vector3 b)
    {
        return a.xyzPx != b.xyzPx;
    }

    public override int GetHashCode()
    {
        return xyzPx.GetHashCode();
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is Vector3 vector3)
            return xyzPx.Equals(vector3.xyzPx);
        if (obj is Vector3D<float> vector3D)
            return xyzPx.Equals(vector3D);
        return false;
    }
}