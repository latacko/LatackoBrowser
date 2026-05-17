using Silk.NET.Maths;
using Units;

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
    Vector2D<float> BaseSize;
    internal UIUnit Width;
    internal UIUnit Height;
    #endregion

    RuntimeModelData runtimeModelData;
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

    public Layout(RuntimeModelData runtimeModelData)
    {
        this.runtimeModelData = runtimeModelData;
    }


    public void ConvertToPx(Vector2D<float> size)
    {
        Width.ConvertToPx(size);
        Height.ConvertToPx(size);

        Left.ConvertToPx(size);
        Top.ConvertToPx(size);
    }

    public void UpdateBoundsOffset()
    {
        if (runtimeModelData.Parent == null)
        {
            bounds.OffsetX = Left.Value;
            bounds.OffsetY = Top.Value;
        }
        else
        {
            bounds.OffsetX = runtimeModelData.Parent.Layout.Left.Value + LayoutPos.X + Left.Value;
            bounds.OffsetY = runtimeModelData.Parent.Layout.Top.Value + LayoutPos.Y + Top.Value;
        }
    }

    public Layout SetLeft(UIUnit left, Align align = Align.Left)
    {
        if (Left == left) return this;
        Left = left;
        LeftAlign = align;
        dirty |= LayoutDirty.Position;
        UpdateBoundsOffset();
        return this;
    }

    public Layout SetTop(UIUnit top, Align align = Align.Top)
    {
        if (Top == top) return this;
        Top = top;
        TopAlign = align;
        dirty |= LayoutDirty.Position;
        UpdateBoundsOffset();
        return this;
    }

    public Layout SetWidth(UIUnit width)
    {
        if (Width == width) return this;
        Width = width;
        dirty |= LayoutDirty.Size;

        bounds.Width = width.Value;
        return this;
    }

    public Layout SetHeight(UIUnit height)
    {
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
        if (runtimeModelData.Children == null) return;
        float sizeOfLine = 0;

        float innerWidth = Width.Value - PaddingLeft.Value - PaddingRight.Value;
        float cursorX = PaddingLeft.Value;
        float cursorY = PaddingTop.Value;

        foreach (var child in runtimeModelData.Children)
        {
            switch (child.Layout.Display)
            {
                case DisplayType.inline:
                    if (cursorX + child.Layout.Width.Value > Width.Value - PaddingRight.Value && cursorX > PaddingLeft.Value)
                    {
                        cursorX = PaddingLeft.Value;
                        cursorY += sizeOfLine;
                    }

                    child.Layout.LayoutPos = new(cursorX, cursorY);
                    cursorX += child.Layout.Width.Value;

                    sizeOfLine = Math.Max(sizeOfLine, child.Layout.Height.Value);
                    break;
                case DisplayType.block:
                    cursorX = PaddingLeft.Value;
                    cursorY += sizeOfLine > 0 ? sizeOfLine : 0;
                    child.Layout.LayoutPos = new Vector2D<float>(cursorX, cursorY);
                    child.Layout.BaseSize = new(innerWidth, 0);
                    break;
            }
            child.Layout.UpdateChildrenLayout();
        }
    }
}