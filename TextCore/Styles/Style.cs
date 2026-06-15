using System;
using Silk.NET.Maths;
using Units;

namespace TextCore.Styles;

public class Style
{
    [Flags]
    enum DirtyFlag
    {
        None = 0,
        Layout = 1 << 0,
        Properties = 1 << 1,
        Transform = 1 << 2,
    }
    DirtyFlag dirty;

    public Properties Properties = new();
    internal ComputedStyle computedStyles;

    public Style SetProperties(Func<Properties, Properties> setProperties)
    {
        return SetProperties(setProperties.Invoke(Properties));
    }

    public Style SetProperties(Properties properties)
    {
        dirty |= DirtyFlag.Properties;
        Properties = properties;
        return this;
    }

    public ComputedStyle ComputeStyles(bool forceUpdate = false, bool updatePercentage = false, Vector2D<float> parentSize = default, Vector2D<float> objectSize = default)
    {
        if (dirty == DirtyFlag.None)
            return computedStyles;
        float pixels;

        bool _shouldForce = forceUpdate || updatePercentage;

        if (_shouldForce || dirty.HasFlag(DirtyFlag.Properties))
        {
            if (_shouldForce || Properties.dirty.HasFlag(Properties.ProperitesDirty.FontSize))
            {
                var _fontSize = computedStyles.FontSize;

                if (TryUpdateValue(Properties.fontSize, objectSize, out pixels))
                    _fontSize = pixels;

                computedStyles.FontSize = _fontSize;
                Properties.dirty &= ~Properties.ProperitesDirty.FontSize;
            }
        }


        return computedStyles;

        bool TryUpdateValue(UIUnit unit, Vector2D<float> size, out float value)
        {
            if (updatePercentage && !unit.IsPercentage)
            {
                value = default;
                return false;
            }

            value = updatePercentage && unit.IsPercentage
                ? unit.Resolve(size)
                : unit.Resolve();

            return true;
        }
    }
}
