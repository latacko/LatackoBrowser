using Silk.NET.Maths;

public struct UIVector2
{
    Vector2D<float> xyPx;
    Vector2D<float> xy;

    public float X => xyPx.X;
    public UnitType XType;
    public float Y => xyPx.Y;
    public UnitType YType;

    public UIVector2(float x, float y) : this(x, y, UnitType.px, UnitType.px)
    {
    }

    public UIVector2(float x, float y, UnitType xType = UnitType.px, UnitType yType = UnitType.px)
    {
        xy.X = x;
        XType = xType;

        xy.Y = y;
        YType = yType;

        ConvertToPx();
    }

    public void ConvertToPx()
    {
        xyPx = new Vector2D<float>(xy.X * UnitsConverter.Get(XType), xy.Y * UnitsConverter.Get(YType));
    }

    public static UIVector2 operator +(UIVector2 a, UIVector2 b)
    {
        var xy = a.xyPx + b.xyPx;

        return new UIVector2(xy.X, xy.Y);
    }

    public static UIVector2 operator -(UIVector2 a, UIVector2 b)
    {
        var xy = a.xyPx - b.xyPx;

        return new UIVector2(xy.X, xy.Y);
    }

    public static bool operator ==(UIVector2 a, UIVector2 b)
    {
        return a.xyPx == b.xyPx;
    }
    public static bool operator !=(UIVector2 a, UIVector2 b)
    {
        return a.xyPx != b.xyPx;
    }
}