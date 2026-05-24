using System.Numerics;
using Silk.NET.Maths;

namespace TextCore;

internal struct GlyphData
{
    public Vector2D<float> UVMin;
    public Vector2D<float> UVMax;

    public float BearingX;
    public float BearingY;

    public float Advance;

    public int Width; // visual size
    public int Height; // visual size
}