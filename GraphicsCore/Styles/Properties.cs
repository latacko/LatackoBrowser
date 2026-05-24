using Silk.NET.Maths;
using Units;

namespace GraphicCore;
public struct Properties
{
    public enum CursorType : byte
    {
        defaultCursor,
        pointer,
        text,
        move,
        wait,
        cursorHelp,
        notAllowed,
        progress,
        crosshair,
        grab,
        grabbing,
        none,
    }
    public RuntimeModelData runtimeModelData;
    public CursorType Cursor;
    public Vector4D<float> BackgroundColor = new(1, 1, 1, 1);
    public float Transition;

    #region Border
    public UIUnit borderRadiusTopLeft;
    public UIUnit borderRadiusTopRight;
    public UIUnit borderRadiusBottomRight;
    public UIUnit borderRadiusBottomLeft;
    #endregion

    public Properties(RuntimeModelData runtimeModelData)
    {
        this.runtimeModelData = runtimeModelData;
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
            ColorTransitionsHelper.StartTransition(runtimeModelData, BackgroundColor, new(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), a), Transition);
        }
        else
            SetBackgroundColorWithoutTransition(r, g, b, a);
        return this;
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
        borderRadius.ConvertToPx(runtimeModelData.Layout.GetSize());

        borderRadiusTopLeft = borderRadius;
        borderRadiusTopRight = borderRadius;
        borderRadiusBottomRight = borderRadius;
        borderRadiusBottomLeft = borderRadius;

        return this;
    }

    public Properties SetBorderRadius(UIUnit topLeft, UIUnit topRight, UIUnit bottomRight, UIUnit bottomLeft)
    {
        topLeft.ConvertToPx(runtimeModelData.Layout.GetSize());
        topRight.ConvertToPx(runtimeModelData.Layout.GetSize());
        bottomRight.ConvertToPx(runtimeModelData.Layout.GetSize());
        bottomLeft.ConvertToPx(runtimeModelData.Layout.GetSize());


        borderRadiusTopLeft = topLeft;
        borderRadiusTopRight = topRight;
        borderRadiusBottomRight = bottomRight;
        borderRadiusBottomLeft = bottomLeft;

        return this;
    }
    #endregion

    internal Properties ConvertToPx(Vector2D<float> selfSize)
    {
        borderRadiusTopLeft.ConvertToPx(selfSize);
        borderRadiusTopRight.ConvertToPx(selfSize);
        borderRadiusBottomRight.ConvertToPx(selfSize);
        borderRadiusBottomLeft.ConvertToPx(selfSize);

        return this;
    }
}