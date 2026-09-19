using System;
using GraphicsCore;
using GraphicsCore.Styles;
using Silk.NET.Maths;

namespace TextCore.Text;

public struct TextLineLayoutManager : ILayoutManager
{
    public TextLine textLine;
    Vector2D<float> parentSize;

    public VisualElement GetVisualElement() => textLine;
    public void SetVisualElement(VisualElement visualElement)
    {
        textLine = (TextLine)visualElement;
    }

    public Vector2D<float> GetParentSize() => parentSize;
    public void SetParentSize(Vector2D<float> size)
    {
        parentSize = size;
    }


    public void Arrange(ref float cursorX, ref float cursorY, Action newLine, Action<float> updateSizeOfLine, ref float width)
    {
        throw new System.Exception("This funtion shoudn't be executed on runtime text!");
    }

    public Bounds GetBounds() => new();
    public float GetLayoutTop() => 0;

    public float GetLayoutLeft() => 0;

    public Vector2D<float> GetLayoutSize()
    {
        return new Vector2D<float>(textLine.widthWithoutScale * textLine.textContainer.computedStyle.FontSize / 2, textLine.textContainer.fontAtlas.height * textLine.textContainer.computedStyle.FontSize);
    }

    public void UpdateChildrenLayout()
    {
        throw new Exception("You shoudn't update children layout for this object.");
    }

    public void UpdatePosition()
    {
        textLine.AddFlag(VisualElement.RenderDirtyFlags.Matrix);
        return;
    }
}
