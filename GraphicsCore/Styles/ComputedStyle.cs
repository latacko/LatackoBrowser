using System;
using Silk.NET.Maths;

namespace GraphicsCore.Styles;

public record struct ComputedStyle
{
    public Vector2D<float> Pos;
    public Vector2D<float> Size;
    public Vector4D<float> Padding;
    public Vector4D<float> Margin;
    public Vector4D<float> BorderRadius;
    public Vector2D<float> Translate;

    #region Text
    public float FontSize;
    #endregion
}
