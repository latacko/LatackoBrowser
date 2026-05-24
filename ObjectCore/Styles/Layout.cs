using GraphicCore;
using Silk.NET.Maths;
using Units;

namespace ObjectCore;
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
    }

    internal LayoutDirty dirty;

    #region Pos
    Bounds bounds = new();
    public Bounds Bounds => bounds;
    internal Vector2D<float> LayoutPos;
    public UIUnit Top;
    public Align TopAlign = Align.Top;
    public UIUnit Left;
    public Align LeftAlign = Align.Left;
    #endregion

    #region Size
    internal Vector2D<float> BaseSize;
    internal UIUnit Width;
    internal UIUnit Height;
    #endregion

    RuntimeObject runtimeObject;
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
    public UIUnit PaddingLeft;
    public UIUnit PaddingTop;
    public UIUnit PaddingRight;
    public UIUnit PaddingBottom;
    #endregion

    #region Margin
    public UIUnit MarginLeft;
    public UIUnit MarginTop;
    public UIUnit MarginRight;
    public UIUnit MarginBottom;
    #endregion

    public Layout(RuntimeObject runtimeObject)
    {
        this.runtimeObject = runtimeObject;
    }


    public void ConvertToPx(Vector2D<float> parentSize)
    {
        Width.ConvertToPx(parentSize);
        Height.ConvertToPx(parentSize);

        Left.ConvertToPx(parentSize);
        Top.ConvertToPx(parentSize);
    }

    public void UpdateBoundsOffset()
    {
        if (runtimeObject.Parent == null)
        {
            bounds.OffsetX = Left.Value;
            bounds.OffsetY = Top.Value;
        }
        else
        {
            bounds.OffsetX = runtimeObject.Parent.GetLayoutLeft() + LayoutPos.X + Left.Value;
            bounds.OffsetY = runtimeObject.Parent.GetLayoutTop() + LayoutPos.Y + Top.Value;
        }
    }

    public Layout SetLeft(UIUnit left, Align align = Align.Left)
    {
        left.ConvertToPx(runtimeObject.ParentSize);
        if (Left == left) return this;
        Left = left;
        LeftAlign = align;
        dirty |= LayoutDirty.Position;
        UpdateBoundsOffset();
        return this;
    }

    public Layout SetTop(UIUnit top, Align align = Align.Top)
    {
        top.ConvertToPx(runtimeObject.ParentSize);
        if (Top == top) return this;
        Top = top;
        TopAlign = align;
        dirty |= LayoutDirty.Position;
        UpdateBoundsOffset();
        return this;
    }

    public Layout SetWidth(UIUnit width)
    {
        width.ConvertToPx(runtimeObject.ParentSize);
        if (Width == width) return this;
        Width = width;
        dirty |= LayoutDirty.Size;

        bounds.Width = width.Value;
        return this;
    }

    public Layout SetHeight(UIUnit height)
    {
        height.ConvertToPx(runtimeObject.ParentSize);
        if (Height == height) return this;
        Height = height;
        dirty |= LayoutDirty.Size;
        bounds.Height = Height.Value;
        return this;
    }

    public Vector2D<float> GetSize()
    {
        float _width;
        float _height;

        if (Width.Value == -1)
            _width = BaseSize.X;
        else
            _width = Width.Value;
        if (Height.Value == -1)
            _height = BaseSize.Y;
        else
            _height = Height.Value;

        return new(_width, _height);
    }

    public Layout SetDisplay(DisplayType display)
    {
        Display = display;
        return this;
    }

    public void UpdateChildrenLayout()
    {
        if (runtimeObject.Children == null) return;
        float sizeOfLine = 0;

        float innerWidth = Width.Value - PaddingLeft.Value - PaddingRight.Value;
        float cursorX = PaddingLeft.Value;
        float cursorY = PaddingTop.Value;

        foreach (var child in runtimeObject.Children)
        {
            child.UpdateLayout(ref cursorX, ref cursorY, ref sizeOfLine, ref innerWidth);
            // child.Layout.UpdateChildrenLayout();
        }
    }
}