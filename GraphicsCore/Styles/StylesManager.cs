using System;
using System.Diagnostics;

namespace GraphicCore.Styles;

public class StylesManager
{
    [Flags]
    public enum DirtyFlag
    {
        None = 0,
        ScreenSize = 1 << 0,
        StylesDirty = 1 << 1,
    }
    DirtyFlag dirty = DirtyFlag.None;
    readonly Dictionary<string, Style> styles = new();
    readonly HashSet<Style> inlineStyles = new();

    public void SetStyle(string styleKey, Style style)
    {
        styles[styleKey] = style;
        styles[styleKey].computedStyles = styles[styleKey].ComputeStyles();
    }

    public void UpdateStyle(string styleKey, Func<Style, Style> updatedStyle)
    {
        styles[styleKey] = updatedStyle.Invoke(styles[styleKey]);
        // styles[styleKey].computedStyles = styles[styleKey].ComputeStyles();
    }

    public void AddInlineStyle(Style style)
    {
        inlineStyles.Add(style);
        // style.computedStyles = style.ComputeStyles();
    }

    public void AddFlag(DirtyFlag flag)
    {
        dirty |= flag;
    }

    public void ClearFlags()
    {
        dirty = DirtyFlag.None;
    }

    public Style GetStyle(string styleKey)
    {
        return styles[styleKey];
    }

    static Stopwatch stopwatch = new();

    public void ComputeStyles()
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

    public void SetFrameAsNotDirty(uint frame)
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
