using System.Numerics;

namespace GraphicsCore;

public struct Bounds
{
    public float OffsetX;
    public float OffsetY;

    public float Width;
    public float Height;

    public readonly bool Contains(Vector2 point)
    {
        return point.X >= OffsetX &&
                point.Y >= OffsetY &&
                point.X <= OffsetX + Width &&
                point.Y <= OffsetY + Height;
    }

    public static bool Contains(Bounds outer, Bounds inner)
    {
        return inner.OffsetX >= outer.OffsetX &&
                inner.OffsetY >= outer.OffsetY &&
                inner.OffsetX + inner.Width <= outer.OffsetX + outer.Width &&
                inner.OffsetY + inner.Height <= outer.OffsetY + outer.Height;
    }

    public static bool Intersects(Bounds a, Bounds b)
    {
        return a.OffsetX < b.OffsetX + b.Width &&
                a.OffsetX + a.Width > b.OffsetX &&
                a.OffsetY < b.OffsetY + b.Height &&
                a.OffsetY + a.Height > b.OffsetY;
    }
}