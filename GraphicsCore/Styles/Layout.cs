using GraphicsCore;
using Silk.NET.Maths;
using Units;

namespace GraphicsCore.Styles;

public struct Layout
{
    [Flags]
    public enum Align : byte
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Top = 1 << 2,
        Bottom = 1 << 3,
    }

    [Flags]
    public enum LayoutDirty : byte
    {
        None = 0,
        Position = 1 << 0,
        Size = 1 << 1,
        Top = 1 << 2,
        Bottom = 1 << 3,
        Padding = 1 << 4,
        Margin = 1 << 4,
    }


    [Flags]
    public enum WhereIsPercentage : byte
    {
        None,
        Pos = 1 << 0,
        Size = 1 << 1,
        Padding = 1 << 2,
        Margin = 1 << 3,
    }

    internal LayoutDirty dirty;
    internal WhereIsPercentage whereIsPercentage;

    #region Pos
    Bounds bounds = new();
    public Bounds Bounds => bounds;
    internal Vector2D<float> LayoutPos;
    public UIUnit Top { get; private set; }
    public Align TopAlign { get; private set; } = Align.Top;
    public UIUnit Left { get; private set; }
    public Align LeftAlign { get; private set; } = Align.Left;
    #endregion

    #region Size
    internal UIUnit Width { get; private set; }
    internal UIUnit Height { get; private set; }
    #endregion
    public enum DisplayType
    {
        inline,
        block,
        contents,
        flox,
        gird,
        inlineBlock,
        none,
        inherit,
    }
    public DisplayType Display;

    #region Padding
    public UIUnit PaddingLeft { get; private set; }
    public UIUnit PaddingTop { get; private set; }
    public UIUnit PaddingRight { get; private set; }
    public UIUnit PaddingBottom { get; private set; }
    #endregion

    #region Margin
    public UIUnit MarginLeft { get; private set; }
    public UIUnit MarginTop { get; private set; }
    public UIUnit MarginRight { get; private set; }
    public UIUnit MarginBottom { get; private set; }
    #endregion

    public Layout()
    {
    }


    // public void UpdateBoundsOffset()
    // {
    //     if (runtimeObject == null)
    //         return;

    //     if (runtimeObject.Parent == null)
    //     {
    // bounds.OffsetX = Left.Value;
    // bounds.OffsetY = Top.Value;
    // }
    // else
    // {
    // bounds.OffsetX = runtimeObject.Parent.GetLayoutLeft() + LayoutPos.X + Left.Value;
    // bounds.OffsetY = runtimeObject.Parent.GetLayoutTop() + LayoutPos.Y + Top.Value;
    //     }
    // }

    void AddOrRemoveFlagByUnit(ref UIUnit unit, WhereIsPercentage flag)
    {
        if (unit.IsPercentage)
            whereIsPercentage |= flag;
        else
            whereIsPercentage &= ~flag;
    }

    public Layout SetLeft(UIUnit left, Align align = Align.Left)
    {
        if (Left == left) return this;
        Left = left;
        LeftAlign = align;
        dirty |= LayoutDirty.Position;

        AddOrRemoveFlagByUnit(ref left, WhereIsPercentage.Pos);

        return this;
    }

    public Layout SetTop(UIUnit top, Align align = Align.Top)
    {
        if (Top == top) return this;
        Top = top;
        TopAlign = align;
        dirty |= LayoutDirty.Position;

        AddOrRemoveFlagByUnit(ref top, WhereIsPercentage.Pos);

        return this;
    }

    public Layout SetWidth(UIUnit width)
    {
        if (Width == width) return this;
        Width = width;
        dirty |= LayoutDirty.Size;

        AddOrRemoveFlagByUnit(ref width, WhereIsPercentage.Size);

        return this;
    }

    public Layout SetHeight(UIUnit height)
    {
        if (Height == height) return this;
        Height = height;
        dirty |= LayoutDirty.Size;

        AddOrRemoveFlagByUnit(ref height, WhereIsPercentage.Size);

        return this;
    }

    public Vector2D<float> GetSize()
    {
        float _width = 0;
        float _height = 0;

        // if (Width.Value == -1)
        //     _width = BaseSize.X;
        // else
        //     _width = Width.Value;
        // if (Height.Value == -1)
        //     _height = BaseSize.Y;
        // else
        //     _height = Height.Value;

        return new(_width, _height);
    }

    public Layout SetDisplay(DisplayType display)
    {
        Display = display;
        return this;
    }
}