using GraphicsCore;
using Silk.NET.Maths;
using Units;

namespace GraphicCore.Styles;

public struct Properties
{
    [Flags]
    public enum ProperitesDirty : byte
    {
        None = 0,
        BorderRadius = 1 << 1,
    }

    internal ProperitesDirty dirty;

    [Flags]
    public enum WhereIsPercentage : byte
    {
        None,
        BorderRadius = 1 << 0,
    }
    internal WhereIsPercentage whereIsPercentage;

    public CursorType Cursor;
    public Vector4D<float> BackgroundColor = new(1, 1, 1, 1);
    public float Transition;

    #region Border
    public UIUnit borderRadiusTopLeft;
    public UIUnit borderRadiusTopRight;
    public UIUnit borderRadiusBottomRight;
    public UIUnit borderRadiusBottomLeft;
    #endregion

    public Properties()
    {
    }

    public Properties SetCursor(CursorType cursor)
    {
        Cursor = cursor;
        return this;
    }

    void AddOrRemoveFlagByUnit(ref UIUnit unit, WhereIsPercentage flag)
    {
        if (unit.IsPercentage)
            whereIsPercentage |= flag;
        else
            whereIsPercentage &= ~flag;
    }

    #region Background Color
    public Properties SetBackgroundColor255(float r, float g, float b, float a)
    {
        return SetBackgroundColor(r / 255, g / 255, b / 255, a / 255);
    }

    public Properties SetBackgroundColor(float r, float g, float b, float a)
    {
        // if (Transition > 0)
        // {
        //     GraphicCore.ColorTransitionsHelper.StartTransition(runtimeObject, BackgroundColor, new(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), a), Transition, UpdateColorTransitionHelper);
        // }
        // else
        SetBackgroundColorWithoutTransition(r, g, b, a);
        return this;
    }

    public readonly void UpdateColorTransitionHelper(Vector4D<float> targetColor)
    {
        // runtimeObject.Style.SetProperties(runtimeObject.Style.Properties.SetBackgroundColorWithoutTransitionInLinear(targetColor.X, targetColor.Y, targetColor.Z, targetColor.W));
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

        AddOrRemoveFlagByUnit(ref borderRadius, WhereIsPercentage.BorderRadius);

        dirty |= ProperitesDirty.BorderRadius;

        return this;
    }

    public Properties SetBorderRadius(UIUnit topLeft, UIUnit topRight, UIUnit bottomRight, UIUnit bottomLeft)
    {
        // topLeft.ConvertToPx(runtimeObject.Layout.GetSize());
        // topRight.ConvertToPx(runtimeObject.Layout.GetSize());
        // bottomRight.ConvertToPx(runtimeObject.Layout.GetSize());
        // bottomLeft.ConvertToPx(runtimeObject.Layout.GetSize());
        whereIsPercentage &= ~WhereIsPercentage.BorderRadius;

        borderRadiusTopLeft = topLeft;
        borderRadiusTopRight = topRight;
        borderRadiusBottomRight = bottomRight;
        borderRadiusBottomLeft = bottomLeft;

        AddOrRemoveFlagByUnit(ref topLeft, WhereIsPercentage.BorderRadius);
        if (!whereIsPercentage.HasFlag(WhereIsPercentage.BorderRadius))
            AddOrRemoveFlagByUnit(ref topRight, WhereIsPercentage.BorderRadius);
        if (!whereIsPercentage.HasFlag(WhereIsPercentage.BorderRadius))
            AddOrRemoveFlagByUnit(ref bottomRight, WhereIsPercentage.BorderRadius);
        if (!whereIsPercentage.HasFlag(WhereIsPercentage.BorderRadius))
            AddOrRemoveFlagByUnit(ref bottomLeft, WhereIsPercentage.BorderRadius);

        dirty |= ProperitesDirty.BorderRadius;

        return this;
    }
    #endregion
}