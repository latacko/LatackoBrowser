using System;
using GraphicsCore;
using GraphicsCore.Styles;
using Silk.NET.Maths;

namespace PrimitiveCore;

public struct NodeLayoutManager : ILayoutManager
{
    Node node;
    Vector2D<float> parentSize;

    public VisualElement GetVisualElement() => node;

    public Vector2D<float> GetParentSize() => parentSize;

    public void SetVisualElement(VisualElement visualElement)
    {
        node = (Node)visualElement;
    }

    public void SetParentSize(Vector2D<float> size)
    {
        parentSize = size;
    }

    public void UpdateChildrenLayout()
    {
        // throw new NotImplementedException();
    }

    public void Arrange(ref float cursorX, ref float cursorY, Action newLine, Action<float> updateSizeOfLine, ref float width)
    {
        updateSizeOfLine(GetLayoutSize().Y);

        switch (node.Style.Layout.Display)
        {
            case GraphicsCore.Styles.Layout.DisplayType.inline:
                if (cursorX + node.computedStyle.Size.X > width)
                    newLine.Invoke();

                node.worldPosition = new(cursorX, cursorY);
                cursorX += node.computedStyle.Size.X;

                break;
            case GraphicsCore.Styles.Layout.DisplayType.block:
                newLine.Invoke();

                node.worldPosition = new(cursorX, cursorY);
                // Layout.BaseSize = new(width, 0);
                break;
        }
    }

    public Bounds GetBounds()=>new();

    public float GetLayoutTop()=>node.worldPosition.Y;
    public float GetLayoutLeft()=>node.worldPosition.X;
    public Vector2D<float> GetLayoutSize()=>node.computedStyle.Size;

    public void UpdatePosition()
    {
        node.relativePos = (node.Parent != null ? node.Parent.relativePos : new Vector3D<float>()) + new Vector3D<float>(GetLayoutLeft(), GetLayoutTop(), 0);
        node.relativeRot = (node.Parent != null ? node.Parent.relativeRot : new Vector3D<float>()) + node.Style.Transform.Rotation;
        node.relativeTransformation = (node.Parent != null ? node.Parent.relativeTransformation : new Vector3D<float>()) + new Vector3D<float>(node.computedStyle.Translate.X, node.computedStyle.Translate.Y, 0);

        node.AddFlag(VisualElement.RenderDirtyFlags.Matrix);
        // Layout.UpdateBoundsOffset();
        if (node.Children == null) return;

        foreach (var child in node.Children)
        {
            child.LayoutManager.UpdatePosition();
        }
    }
}
