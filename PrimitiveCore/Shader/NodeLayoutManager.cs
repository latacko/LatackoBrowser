using System;
using GraphicsCore;
using GraphicsCore.Styles;
using Silk.NET.Maths;
using Vulkan;

namespace PrimitiveCore.Shader;

public struct NodeLayoutManager : ILayoutManager
{
    Node node;
    Vector2D<float> parentSize;
    Bounds bounds;

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

    public void Arrange(ref float cursorX, ref float cursorY, ref float paddingLeft, ref float sizeOfLine, ref float width)
    {
        node.UpdateArrangement();
        UpdateSizeOfLine(ref sizeOfLine, GetLayoutSize().Y);



        switch (node.Style.Layout.Display)
        {
            case GraphicsCore.Styles.Layout.DisplayType.inline:
                if (cursorX + node.computedStyle.Size.X > width)
                    NewLine(ref cursorX, ref cursorY, paddingLeft, sizeOfLine);

                UpdateBounds(in cursorX, in cursorY);

                node.worldPosition = new(cursorX, cursorY);
                cursorX += node.computedStyle.Size.X;
                break;
            case GraphicsCore.Styles.Layout.DisplayType.block:
                NewLine(ref cursorX, ref cursorY, paddingLeft, sizeOfLine);

                UpdateBounds(in cursorX, in cursorY);
                
                node.worldPosition = new(cursorX, cursorY);
                // Layout.BaseSize = new(width, 0);
                break;
        }
    }

    void UpdateBounds(in float cursorX, in float cursorY)
    {
        bounds = new()
        {
            OffsetX = cursorX,
            OffsetY = cursorY,

            Width = node.computedStyle.Size.X,
            Height = node.computedStyle.Size.Y,
        };
    }

    void NewLine(ref float cursorX, ref float cursorY, in float _paddingLeft, in float sizeOfLine)
    {
        cursorX = _paddingLeft;
        cursorY += sizeOfLine;
    }
    void UpdateSizeOfLine(ref float sizeOfLine, in float newSizeOfLine)
    {
        if (newSizeOfLine > sizeOfLine)
        {
            sizeOfLine = newSizeOfLine;
        }
    }

    public Bounds GetBounds() => bounds;

    public float GetLayoutTop() => node.worldPosition.Y;
    public float GetLayoutLeft() => node.worldPosition.X;
    public Vector2D<float> GetLayoutSize() => node.computedStyle.Size;

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
