using System;

namespace ObjectCore.Styles;

public static class StylesManager
{
    [Flags]
    public enum DirtyFlag
    {
        None = 0,
        ScreenSize = 1 << 0,
    }
    static DirtyFlag dirty;
    static readonly Dictionary<string, Style> styles = new();
    static readonly HashSet<Style> inlineStyles = new();

    public static void SetStyle(string styleKey, Style style)
    {
        styles[styleKey] = style;
    }

    public static void UpdateStyle(string styleKey, Func<Style, Style> updatedStyle)
    {
        styles[styleKey] = updatedStyle.Invoke(styles[styleKey]);
    }

    public static void AddInlineStyle(Style style)
    {
        inlineStyles.Add(style);
    }

    public static void AddFlag(DirtyFlag dirty)
    {
        StylesManager.dirty |= dirty;
    }

    public static void ComputeStyles()
    {
        bool _shouldForce = dirty.HasFlag(DirtyFlag.ScreenSize);
        if (!_shouldForce)
            return;
            
        Parallel.ForEach(styles, item =>
        {
            item.Value.computedStyles = item.Value.ComputeStyles(_shouldForce);
        });


        Parallel.ForEach(styles, item =>
        {
            item.Value.computedStyles = item.Value.ComputeStyles(_shouldForce);
        });

        dirty &= ~DirtyFlag.ScreenSize;
    }
}
