using System.Numerics;
using Silk.NET.Maths;

namespace TextCore;

internal record struct GlyphData
{
    public Vector2D<float> UVMin;
    public Vector2D<float> UVMax;

    public float BearingX;
    public float BearingY;

    public float Advance;

    public float Width; // visual size
    public float Height; // visual size
}