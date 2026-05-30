using System;
using GraphicsCore;
using Silk.NET.Maths;
using Units;

namespace TextCore.Styles;

public struct Properties
{
    public RuntimeTextContainer runtimeText;
    public CursorType Cursor;
    public Vector4D<float> TextColor = new(1, 1, 1, 1);
    public float Transition;
    public string font;
    public UIUnit fontSize;

    public Properties(RuntimeTextContainer runtimeText)
    {
        this.runtimeText = runtimeText;
    }

    public Properties SetCursor(CursorType cursor)
    {
        Cursor = cursor;
        return this;
    }

    #region Background Color
    public Properties SetBackgroundColor255(float r, float g, float b, float a)
    {
        return SetBackgroundColor(r / 255, g / 255, b / 255, a / 255);
    }

    public Properties SetBackgroundColor(float r, float g, float b, float a)
    {
        if (Transition > 0)
        {
            GraphicCore.ColorTransitionsHelper.StartTransition(runtimeText, TextColor, new(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), a), Transition, UpdateColorTransitionHelper);
        }
        else
            SetBackgroundColorWithoutTransition(r, g, b, a);
        return this;
    }

    public readonly void UpdateColorTransitionHelper(Vector4D<float> targetColor)
    {
        runtimeText.SetProperties(runtimeText.Properties.SetBackgroundColorWithoutTransitionInLinear(targetColor.X, targetColor.Y, targetColor.Z, targetColor.W));
    }

    public Properties SetBackgroundColorWithoutTransition(float r, float g, float b, float a)
    {
        TextColor = new(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), a);
        return this;
    }

    public Properties SetBackgroundColorWithoutTransitionInLinear(float r, float g, float b, float a)
    {
        TextColor = new(r, g, b, a);
        return this;
    }
    static float SrgbToLinear(float c)
    {
        return c <= 0.04045f
            ? c / 12.92f
            : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    }
    #endregion

    public Properties SetTransition(float transition)
    {
        Transition = transition;
        return this;
    }

    public Properties SetFont(string font)
    {
        this.font = font;
        return this;
    }

    public Properties SetFontSize(UIUnit fontSize)
    {
        this.fontSize = fontSize;
        return this;
    }

    public void ConvertToPx(Vector2D<float> parentSize)
    {
        fontSize.ConvertToPx(parentSize);
    }
}
