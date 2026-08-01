using System;
using System.Diagnostics;

namespace GraphicCore.Styles;

public static class StylesManager
{
    [Flags]
    public enum DirtyFlag
    {
        None = 0,
        ScreenSize = 1 << 0,
        StylesDirty = 1 << 1,
    }
    static DirtyFlag dirty = DirtyFlag.None;
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
        // styles[styleKey].computedStyles = styles[styleKey].ComputeStyles();
    }

    public static void AddInlineStyle(Style style)
    {
        inlineStyles.Add(style);
        // style.computedStyles = style.ComputeStyles();
    }

    public static void AddFlag(DirtyFlag dirty)
    {
        StylesManager.dirty |= dirty;
    }

    public static void ClearFlags()
    {
        dirty = DirtyFlag.None;
    }

    public static Style GetStyle(string styleKey)
    {
        return styles[styleKey];
    }

    static Stopwatch stopwatch = new();

    public static void ComputeStyles()
    {
        bool _shouldForce = dirty.HasFlag(DirtyFlag.ScreenSize) || dirty.HasFlag(DirtyFlag.StylesDirty);
        if (!_shouldForce)
            return;

        // Console.WriteLine("Computing styles.");
        // stopwatch.Restart();

        // Parallel.ForEach(styles, item =>
        // {
        //     item.Value.computedStyles = item.Value.ComputeStyles(_shouldForce, shouldMarkAsDirty:true);
        // });


        // Parallel.ForEach(inlineStyles, item =>
        // {
        //     item.computedStyles = item.ComputeStyles(_shouldForce, shouldMarkAsDirty:true);
        // });

        foreach (var item in styles)
        {
            item.Value.computedStyles = item.Value.ComputeStyles(_shouldForce, shouldMarkAsDirty: true);
        }

        foreach (var item in inlineStyles)
        {
            item.computedStyles = item.ComputeStyles(_shouldForce, shouldMarkAsDirty: true);
        }


        // stopwatch.Stop();
        // Console.WriteLine($"Took: {stopwatch.Elapsed.TotalMicroseconds:F2}us");

        dirty = DirtyFlag.None;
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
