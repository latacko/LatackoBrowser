using GraphicsCore;
using Silk.NET.Maths;
using Units;

namespace ObjectCore;

public struct Properties
{
    [Flags]
    public enum ProperitesDirty : byte
    {
        None = 0,
        BorderRadius = 1 << 1,
    }

    internal ProperitesDirty dirty;


    public RuntimeObject runtimeObject;
    public CursorType Cursor;
    public Vector4D<float> BackgroundColor = new(1, 1, 1, 1);
    public float Transition;

    #region Border
    public UIUnit borderRadiusTopLeft;
    public UIUnit borderRadiusTopRight;
    public UIUnit borderRadiusBottomRight;
    public UIUnit borderRadiusBottomLeft;
    #endregion

    public Properties(RuntimeObject runtimeText)
    {
        this.runtimeObject = runtimeText;
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
            GraphicCore.ColorTransitionsHelper.StartTransition(runtimeObject, BackgroundColor, new(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), a), Transition, UpdateColorTransitionHelper);
        }
        else
            SetBackgroundColorWithoutTransition(r, g, b, a);
        return this;
    }

    public readonly void UpdateColorTransitionHelper(Vector4D<float> targetColor)
    {
        runtimeObject.Style.SetProperties(runtimeObject.Style.Properties.SetBackgroundColorWithoutTransitionInLinear(targetColor.X, targetColor.Y, targetColor.Z, targetColor.W));
    }

    public Properties SetBackgroundColorWithoutTransition(float r, float g, float b, float a)
    {
        BackgroundColor = new(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), a);
        return this;
    }

    public Properties SetBackgroundColorWithoutTransitionInLinear(float r, float g, float b, float a)
    {
        BackgroundColor = new(r, g, b, a);
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

    #region Set Border
    public Properties SetBorderRadius(UIUnit borderRadius)
    {
        // borderRadius.ConvertToPx(runtimeObject.Layout.GetSize());

        borderRadiusTopLeft = borderRadius;
        borderRadiusTopRight = borderRadius;
        borderRadiusBottomRight = borderRadius;
        borderRadiusBottomLeft = borderRadius;

        dirty |= ProperitesDirty.BorderRadius;

        return this;
    }

    public Properties SetBorderRadius(UIUnit topLeft, UIUnit topRight, UIUnit bottomRight, UIUnit bottomLeft)
    {
        // topLeft.ConvertToPx(runtimeObject.Layout.GetSize());
        // topRight.ConvertToPx(runtimeObject.Layout.GetSize());
        // bottomRight.ConvertToPx(runtimeObject.Layout.GetSize());
        // bottomLeft.ConvertToPx(runtimeObject.Layout.GetSize());


        borderRadiusTopLeft = topLeft;
        borderRadiusTopRight = topRight;
        borderRadiusBottomRight = bottomRight;
        borderRadiusBottomLeft = bottomLeft;

        dirty |= ProperitesDirty.BorderRadius;

        return this;
    }
    #endregion
}