using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;

[assembly: InternalsVisibleTo("Browser")]
namespace Units;

public struct UIUnit
{
    float ValueInUnit;
    UnitType ValueType;
    public readonly bool IsPercentage;

    public UIUnit(float value) : this(value, UnitType.px)
    {
    }

    public UIUnit(float value, UnitType valueType = UnitType.px)
    {
        ValueInUnit = value;
        ValueType = value == 0 ? UnitType.px : valueType;
        IsPercentage = ValueType == UnitType.percentageWidth || ValueType == UnitType.percentageHeight;
    }

    public readonly float Resolve(Vector2D<float> parentSize)
    {
        switch (ValueType)
        {
            case UnitType.percentageWidth:
                return parentSize != default ? ValueInUnit * parentSize.X / 100f : 0;
            case UnitType.percentageHeight:
                return parentSize != default ? ValueInUnit * parentSize.Y / 100f : 0;
            default:
                return ValueInUnit * UnitsConverter.Get(ValueType);
        }
    }

    public readonly float Resolve()
    {
        return ValueInUnit * UnitsConverter.Get(ValueType);
    }

    public static bool operator ==(UIUnit a, UIUnit b)
    {
        return a.ValueInUnit == b.ValueInUnit && a.ValueType == b.ValueType;
    }

    public static bool operator !=(UIUnit a, UIUnit b)
    {
        return a.ValueInUnit != b.ValueInUnit || a.ValueType != b.ValueType;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is UIUnit unit)
            return ValueInUnit == unit.ValueInUnit && ValueType == unit.ValueType;
        return false;
    }

    public override string ToString()
    {
        return ValueInUnit.ToString() + ValueType.ToString();
    }
}