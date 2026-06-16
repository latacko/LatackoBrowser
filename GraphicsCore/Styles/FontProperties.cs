using System;
using GraphicsCore;
using Silk.NET.Maths;
using Units;

namespace GraphicCore.Styles;

public struct FontProperties
{
    [Flags]
    public enum FontProperitesDirty : byte
    {
        None = 0,
        FontSize = 1 << 1,
    }

    internal FontProperitesDirty dirty;
    public CursorType Cursor;
    public Vector4D<float> TextColor = new(1, 1, 1, 1);
    public float Transition;
    public string font = "google-noto/NotoSerif-Regular.ttf";
    public UIUnit fontSize = new(16);

    public FontProperties()
    {
    }

    public FontProperties SetCursor(CursorType cursor)
    {
        Cursor = cursor;
        return this;
    }

    #region Text Color
    public FontProperties SetTextColor255(float r, float g, float b, float a)
    {
        return SetTextColor(r / 255, g / 255, b / 255, a / 255);
    }

    public FontProperties SetTextColor(float r, float g, float b, float a)
    {
        // if (Transition > 0)
        // {
        //     GraphicCore.ColorTransitionsHelper.StartTransition(runtimeText, TextColor, new(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), a), Transition, UpdateColorTransitionHelper);
        // }
        // else
            SetTextColorWithoutTransition(r, g, b, a);
        return this;
    }

    // public readonly void UpdateColorTransitionHelper(Vector4D<float> targetColor)
    // {
    //     runtimeText.SetProperties(runtimeText.Properties.SetBackgroundColorWithoutTransitionInLinear(targetColor.X, targetColor.Y, targetColor.Z, targetColor.W));
    // }

    public FontProperties SetTextColorWithoutTransition(float r, float g, float b, float a)
    {
        TextColor = new(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), a);
        return this;
    }

    public FontProperties SetTextColorWithoutTransitionInLinear(float r, float g, float b, float a)
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

    public FontProperties SetTransition(float transition)
    {
        Transition = transition;
        return this;
    }

    public FontProperties SetFont(string font)
    {
        this.font = font;
        return this;
    }

    public FontProperties SetFontSize(UIUnit fontSize)
    {
        this.fontSize = fontSize;
        dirty |= FontProperitesDirty.FontSize;
        return this;
    }
}
