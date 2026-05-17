using System.Diagnostics.CodeAnalysis;
using Silk.NET.Maths;
namespace Units;

public struct UIUnit
{
    float ValueInPx = -1;
    float ValueInUnit;
    UnitType ValueType;

    public float Value => ValueInPx;

    /// <summary>
    /// DON"T USE! Use Element.CreateUnity instead
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal UIUnit(float value) : this(value, UnitType.px)
    {
    }

    /// <summary>
    /// DON"T USE! Use Element.CreateUnity instead
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal UIUnit(float value, UnitType valueType = UnitType.px)
    {
        ValueInUnit = value;
        ValueType = value == 0 ? UnitType.px : valueType;
    }

    public UIUnit ConvertToPx(Vector2D<float> parentSize = default)
    {
        if (ValueType == UnitType.percentageWidth)
            ValueInPx = parentSize != default ? ValueInUnit * parentSize.X / 100f : 0;
        else if (ValueType == UnitType.percentageHeight)
            ValueInPx = parentSize != default ? ValueInUnit * parentSize.Y / 100f : 0;
        else
            ValueInPx = ValueInUnit * UnitsConverter.Get(ValueType);
        return this;
    }

    public static UIUnit operator +(UIUnit a, UIUnit b)
    {
        var _newValue = a.ValueInPx + b.ValueInPx;

        return new(_newValue);
    }

    public static UIUnit operator -(UIUnit a, UIUnit b)
    {
        var _newValue = a.ValueInPx - b.ValueInPx;

        return new UIUnit(_newValue);
    }

    public static bool operator ==(UIUnit a, UIUnit b)
    {
        return a.ValueInPx == b.ValueInPx;
    }
    public static bool operator !=(UIUnit a, UIUnit b)
    {
        return a.ValueInPx != b.ValueInPx;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is UIUnit unity)
            return ValueInPx == unity.ValueInPx;
        return false;
    }

    public override string ToString()
    {
        return ValueInUnit.ToString() + ValueType.ToString() + "=" + Value.ToString() + "px";
    }
}