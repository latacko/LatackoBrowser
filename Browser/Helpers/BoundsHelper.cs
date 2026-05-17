using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

public static class BoundsHelper
{
    public static bool Contains(Bounds bounds, Vector2 point)
    {
        return point.X >= bounds.OffsetX &&
                point.Y >= bounds.OffsetY &&
                point.X <= bounds.OffsetX + bounds.Width &&
                point.Y <= bounds.OffsetY + bounds.Height;
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