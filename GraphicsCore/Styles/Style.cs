using System;
using GraphicsCore;
using Silk.NET.Maths;
using Units;

namespace GraphicsCore.Styles;

public class Style
{
    [Flags]
    enum DirtyFlag
    {
        None = 0,
        Layout = 1 << 0,
        Properties = 1 << 1,
        Transform = 1 << 2,
        FontProperties = 1 << 3,
    }
    DirtyFlag dirty;

    public bool[] ShouldObjectUpdate = new bool[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];

    public Layout Layout = new();
    public Properties Properties = new();
    public Transform Transform = new();
    public FontProperties FontProperties = new();

    internal ComputedStyle computedStyles;
    public string Name;

    public Style(string name)
    {
        this.Name = name;
    }

    void SetDirtyObjectFlag()
    {
        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            ShouldObjectUpdate[i] = true;
        }
    }

    public Style SetLayout(Func<Layout, Layout> setLayout)
    {
        return SetLayout(setLayout.Invoke(Layout));
    }

    public Style SetLayout(Layout layout)
    {
        Layout = layout;
        dirty |= DirtyFlag.Layout;
        // StylesManager.AddFlag(StylesManager.DirtyFlag.StylesDirty);
        return this;
    }

    public Style SetTransform(Func<Transform, Transform> setTransform)
    {
        return SetTransform(setTransform.Invoke(Transform));
    }

    public Style SetTransform(Transform transform)
    {
        dirty |= DirtyFlag.Transform;
        // StylesManager.AddFlag(StylesManager.DirtyFlag.StylesDirty);
        Transform = transform;
        return this;
    }

    public Style SetProperties(Func<Properties, Properties> setProperties)
    {
        return SetProperties(setProperties.Invoke(Properties));
    }

    public Style SetProperties(Properties properties)
    {
        dirty |= DirtyFlag.Properties;
        // StylesManager.AddFlag(StylesManager.DirtyFlag.StylesDirty);
        Properties = properties;
        return this;
    }

    public Style SetFontProperties(Func<FontProperties, FontProperties> setProperties)
    {
        return SetFontProperties(setProperties.Invoke(FontProperties));
    }

    public Style SetFontProperties(FontProperties fontProperties)
    {
        dirty |= DirtyFlag.FontProperties;
        // StylesManager.AddFlag(StylesManager.DirtyFlag.StylesDirty);
        FontProperties = fontProperties;
        return this;
    }

    public ComputedStyle ComputeStyles(bool forceUpdate = false, bool updatePercentage = false, Vector2D<float> parentSize = default, Vector2D<float> objectSize = default, bool shouldMarkAsDirty = false)
    {
        bool _shouldForce = forceUpdate || updatePercentage;
        if (dirty == DirtyFlag.None && !_shouldForce)
        {
            return computedStyles;
        }
        float pixels;

        Console.WriteLine(Name + " Zostałem skompilowany "+ " update percentage: " + updatePercentage);
        // Console.WriteLine(Name + " Zostałem skompilowany "+ " update percentage: " + updatePercentage + Environment.StackTrace);

        if (_shouldForce || dirty.HasFlag(DirtyFlag.Layout))
        {
            if (forceUpdate || Layout.dirty.HasFlag(Layout.LayoutDirty.Padding) || Layout.whereIsPercentage.HasFlag(Layout.WhereIsPercentage.Padding))
            {
                var _padding = computedStyles.Padding;

                if (TryUpdateValue(Layout.PaddingLeft, parentSize, out pixels))
                    _padding.X = pixels;

                if (TryUpdateValue(Layout.PaddingTop, parentSize, out pixels))
                    _padding.Y = pixels;

                if (TryUpdateValue(Layout.PaddingRight, parentSize, out pixels))
                    _padding.Z = pixels;

                if (TryUpdateValue(Layout.PaddingBottom, parentSize, out pixels))
                    _padding.W = pixels;

                computedStyles.Padding = _padding;
                Layout.dirty &= ~Layout.LayoutDirty.Padding;
            }

            if (forceUpdate || Layout.dirty.HasFlag(Layout.LayoutDirty.Margin) || Layout.whereIsPercentage.HasFlag(Layout.WhereIsPercentage.Margin))
            {
                var _margin = computedStyles.Margin;

                if (TryUpdateValue(Layout.MarginLeft, parentSize, out pixels))
                    _margin.X = pixels;

                if (TryUpdateValue(Layout.MarginTop, parentSize, out pixels))
                    _margin.Y = pixels;

                if (TryUpdateValue(Layout.MarginRight, parentSize, out pixels))
                    _margin.Z = pixels;

                if (TryUpdateValue(Layout.MarginBottom, parentSize, out pixels))
                    _margin.W = pixels;

                computedStyles.Margin = _margin;
                Layout.dirty &= ~Layout.LayoutDirty.Margin;
            }

            if (forceUpdate || Layout.dirty.HasFlag(Layout.LayoutDirty.Position) || Layout.whereIsPercentage.HasFlag(Layout.WhereIsPercentage.Pos))
            {
                var _position = computedStyles.Pos;

                if (TryUpdateValue(Layout.Left, parentSize, out pixels))
                    _position.X = pixels;

                if (TryUpdateValue(Layout.Top, parentSize, out pixels))
                    _position.Y = pixels;

                computedStyles.Pos = _position;
                Layout.dirty &= ~Layout.LayoutDirty.Position;
            }

            if (forceUpdate || Layout.dirty.HasFlag(Layout.LayoutDirty.Size) || Layout.whereIsPercentage.HasFlag(Layout.WhereIsPercentage.Size))
            {
                var _size = computedStyles.Size;
                // Console.WriteLine("Size is dirty parent size: " + parentSize);
                if (TryUpdateValue(Layout.Width, parentSize, out pixels))
                    _size.X = pixels + computedStyles.Padding.X + computedStyles.Padding.Z;

                if (TryUpdateValue(Layout.Height, parentSize, out pixels))
                    _size.Y = pixels + computedStyles.Padding.Y + computedStyles.Padding.W;
                // Console.WriteLine("Size is : " + _size);
                computedStyles.Size = _size;
                objectSize = computedStyles.Size;
                Layout.dirty &= ~Layout.LayoutDirty.Size;
            }

            dirty &= ~DirtyFlag.Layout;
        }

        if (_shouldForce || dirty.HasFlag(DirtyFlag.Properties))
        {
            if (forceUpdate || Properties.dirty.HasFlag(Properties.ProperitesDirty.BorderRadius) || Properties.whereIsPercentage.HasFlag(Properties.WhereIsPercentage.BorderRadius))
            {
                var _borderRadius = computedStyles.BorderRadius;

                if (TryUpdateValue(Properties.borderRadiusTopLeft, objectSize, out pixels))
                    _borderRadius.X = pixels;

                if (TryUpdateValue(Properties.borderRadiusTopRight, objectSize, out pixels))
                    _borderRadius.Y = pixels;

                if (TryUpdateValue(Properties.borderRadiusBottomRight, objectSize, out pixels))
                    _borderRadius.Z = pixels;

                if (TryUpdateValue(Properties.borderRadiusBottomLeft, objectSize, out pixels))
                    _borderRadius.W = pixels;

                computedStyles.BorderRadius = _borderRadius;
                Properties.dirty &= ~Properties.ProperitesDirty.BorderRadius;
            }

            dirty &= ~DirtyFlag.Properties;
        }

        if (_shouldForce || dirty.HasFlag(DirtyFlag.Transform))
        {
            if (forceUpdate || Transform.dirty.HasFlag(Transform.TransformDirty.Translate) || Transform.whereIsPercentage.HasFlag(Transform.WhereIsPercentage.Translate))
            {
                var _translate = computedStyles.Translate;

                if (TryUpdateValue(Transform.TranslateX, objectSize, out pixels))
                    _translate.X = pixels;

                if (TryUpdateValue(Transform.TranslateY, objectSize, out pixels))
                    _translate.Y = pixels;

                computedStyles.Translate = _translate;
                Transform.dirty &= ~Transform.TransformDirty.Translate;
            }
            dirty &= ~DirtyFlag.Transform;
        }

        if (_shouldForce || dirty.HasFlag(DirtyFlag.FontProperties))
        {
            if (forceUpdate || FontProperties.dirty.HasFlag(FontProperties.FontProperitesDirty.FontSize) || FontProperties.whereIsPercentage.HasFlag(FontProperties.WhereIsPercentage.FontSize))
            {
                var _fontSize = computedStyles.FontSize;

                if (TryUpdateValue(FontProperties.fontSize, parentSize, out pixels))
                    _fontSize = pixels;

                Console.WriteLine("Font size has changed to: " + _fontSize + " original font size: " + FontProperties.fontSize + " object size: " + objectSize);
                computedStyles.FontSize = _fontSize;
                FontProperties.dirty &= ~FontProperties.FontProperitesDirty.FontSize;
            }
            dirty &= ~DirtyFlag.FontProperties;
        }


        // Console.WriteLine("Is forced: " + _shouldForce);

        if (shouldMarkAsDirty)
        {
            // Console.WriteLine("Pokazuje że muszą zrobić update");
            SetDirtyObjectFlag();
        }

        return computedStyles;

        bool TryUpdateValue(UIUnit unit, Vector2D<float> size, out float value)
        {
            if ((updatePercentage && !unit.IsPercentage) || (!updatePercentage && unit.IsPercentage))
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
