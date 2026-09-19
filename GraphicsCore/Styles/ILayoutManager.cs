using System;
using Silk.NET.Maths;
using Vulkan;

namespace GraphicsCore.Styles;

public interface ILayoutManager
{
    public void SetVisualElement(VisualElement visualElement);
    public VisualElement GetVisualElement();
    public void SetParentSize(Vector2D<float> size);
    public Vector2D<float> GetParentSize();

    public float GetLayoutLeft();
    public float GetLayoutTop();

    public Vector2D<float> GetLayoutSize();

    public void Arrange(ref float cursorX, ref float cursorY, Action newLine, Action<float> updateSizeOfLine, ref float width);

    public void UpdateChildrenLayout();

    public Bounds GetBounds();

    public virtual void OnParentSizeChanged(bool informChildren = false)
    {
        Vector2D<float> _parentSize;
        if (GetVisualElement() != null)
        {
            _parentSize = GetVisualElement().Parent.LayoutManager.GetLayoutSize();
        }
        else
        {
            _parentSize = new(Swapchain.Instance.swapChainExtent.Width, Swapchain.Instance.swapChainExtent.Height);
        }
        SetParentSize(_parentSize);
        Console.WriteLine("Updating my parent size: " + _parentSize);
    }

    public void UpdatePosition();
}
