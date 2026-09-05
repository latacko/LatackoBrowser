using System;
using Msdfgen;

namespace MsdfAtlasGen;

public static class Vector2Extensions
{
    public static Vector2 Lerp(Vector2 a, Vector2 b, double t)
    {
        return new Vector2(double.Lerp(a.X, b.X, t), double.Lerp(a.Y, b.Y, t));
    }
}
