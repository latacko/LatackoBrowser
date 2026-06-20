using System;

namespace GraphicCore.Styles;

public static class StylesManager
{
    [Flags]
    public enum DirtyFlag
    {
        None = 0,
        ScreenSize = 1 << 0,
    }
    static DirtyFlag dirty = DirtyFlag.ScreenSize;
    static readonly Dictionary<string, Style> styles = new();
    static readonly HashSet<Style> inlineStyles = new();

    public static void SetStyle(string styleKey, Style style)
    {
        styles[styleKey] = style;
        styles[styleKey].computedStyles = styles[styleKey].ComputeStyles();
    }

    public static void UpdateStyle(string styleKey, Func<Style, Style> updatedStyle)
    {
        styles[styleKey] = updatedStyle.Invoke(styles[styleKey]);
        styles[styleKey].computedStyles = styles[styleKey].ComputeStyles();
    }

    public static void AddInlineStyle(Style style)
    {
        inlineStyles.Add(style);
        style.computedStyles = style.ComputeStyles();
    }

    public static void AddFlag(DirtyFlag dirty)
    {
        StylesManager.dirty |= dirty;
    }

    public static void ComputeStyles()
    {
        bool _shouldForce = dirty.HasFlag(DirtyFlag.ScreenSize);
        // if (!_shouldForce)
        //     return;

        // Console.WriteLine("Computing styles.");
        Parallel.ForEach(styles, item =>
        {
            item.Value.computedStyles = item.Value.ComputeStyles(_shouldForce);
        });


        Parallel.ForEach(inlineStyles, item =>
        {
            item.computedStyles = item.ComputeStyles(_shouldForce);
        });

        dirty &= ~DirtyFlag.ScreenSize;
    }

    public static void SetFrameAsNotDirty(uint frame)
    {
        Parallel.ForEach(styles, item =>
        {
            item.Value.ShouldObjectUpdate[frame] = false;
        });


        Parallel.ForEach(inlineStyles, item =>
        {
            item.ShouldObjectUpdate[frame] = false;
        });
    }
}
