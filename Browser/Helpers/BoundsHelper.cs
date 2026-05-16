using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

public static class BoundsHelper
{
    public static bool Contains(Rect2D bounds, Vector2 point)
    {
        return point.X >= bounds.Offset.X &&
                point.Y >= bounds.Offset.Y &&
                point.X <= bounds.Offset.X + bounds.Extent.Width &&
                point.Y <= bounds.Offset.Y + bounds.Extent.Height;
    }

    public static bool Contains(Rect2D outer, Rect2D inner)
    {
        return inner.Offset.X >= outer.Offset.X &&
                inner.Offset.Y >= outer.Offset.Y &&
                inner.Offset.X + inner.Extent.Width <= outer.Offset.X + outer.Extent.Width &&
                inner.Offset.Y + inner.Extent.Height <= outer.Offset.Y + outer.Extent.Height;
    }

    public static bool Intersects(Rect2D a, Rect2D b)
    {
        return a.Offset.X < b.Offset.X + b.Extent.Width &&
                a.Offset.X + a.Extent.Width > b.Offset.X &&
                a.Offset.Y < b.Offset.Y + b.Extent.Height &&
                a.Offset.Y + a.Extent.Height > b.Offset.Y;
    }
}