using System.Diagnostics;
using System.Runtime.CompilerServices;
using GraphicsCore;
using GraphicsCore.Events;
using GraphicsCore.Styles;
using PrimitiveCore.Model;
using PrimitiveCore.Shader;
using Silk.NET.Maths;
using TextureCore;
using Vulkan;

namespace PrimitiveCore;

public class Node : VisualElement
{
    [Flags]
    internal protected enum DirtyFlags : byte
    {
        None = 0,
        Layout = 1 << 0,
        Size = 1 << 1,
    }

    DirtyFlags dirtyObjectProperties;
    internal Vector2D<float> worldPosition;

    public List<VisualElement>? Children;
    public Texture? texture;

    protected internal override int ObjectDataSize => Unsafe.SizeOf<ModelGPUData>();

    public Node(uint modelId, uint objectIndex, Texture? texture, EventSystem eventSystem, StylesManager stylesManager, VisualElement? parent = null) : base(modelId, objectIndex, eventSystem, stylesManager, parent)
    {
        this.texture = texture;
        this.LayoutManager = new NodeLayoutManager();
        this.LayoutManager.SetVisualElement(this);
    }

    public override void AddChild(VisualElement visualElement)
    {
        if (visualElement.Parent != null)
            visualElement.Parent.RemoveChild(visualElement);

        visualElement.SetParent(this);
        Children ??= [];
        Children.Add(visualElement);

        dirtyObjectProperties |= DirtyFlags.Layout;
    }

    public override void RemoveChild(VisualElement visualElement)
    {
        Children.Remove(visualElement);
        if (Children.Count == 0)
            Children = null;
        dirtyObjectProperties |= DirtyFlags.Layout;
    }

    public override CursorType GetCursorType() => Style.Properties.Cursor;

    bool TryGetObjectData(out ModelGPUData data, uint frame)
    {
        if (renderDirty[frame] == RenderDirtyFlags.None)
        {
            data = default;
            return false;
        }


        if (renderDirty[frame].HasFlag(RenderDirtyFlags.Matrix))
        {
            cachedModel =
            // Matrix4X4.CreateScale(200, 200, 1f);
                Matrix4X4.CreateScale(computedStyle.Size.X, computedStyle.Size.Y, 1f) *
                Matrix4X4.CreateTranslation(-computedStyle.Translate.X, -computedStyle.Translate.Y, 0f) *
                Matrix4X4.CreateFromYawPitchRoll(Style.Transform.Rotation.X, Style.Transform.Rotation.Y, Style.Transform.Rotation.Z) *
                Matrix4X4.CreateTranslation(worldPosition.X, worldPosition.Y, 0f);

            if (Parent != null)
            {
                cachedModel *=
                    Matrix4X4.CreateFromYawPitchRoll(Parent.relativeRot.X, Parent.relativeRot.Y, Parent.relativeRot.Z) *
                    Matrix4X4.CreateTranslation(Parent.relativePos.X, Parent.relativePos.Y, Parent.relativePos.Z);
            }
            RemoveFlag(RenderDirtyFlags.Matrix, frame);
        }

        data = new ModelGPUData
        {
            Model = cachedModel,
            Color = Style.Properties.BackgroundColor,

            pos = new Vector2D<float>(cachedModel.M41, cachedModel.M42),
            size = computedStyle.Size,

            TextureIndex = texture == null ? uint.MaxValue : texture.GetID(),
            borderRadiusTopLeft = computedStyle.BorderRadius.X,
            borderRadiusTopRight = computedStyle.BorderRadius.Y,
            borderRadiusBottomRight = computedStyle.BorderRadius.Z,
            borderRadiusBottomLeft = computedStyle.BorderRadius.W,
        };

        RemoveFlag(RenderDirtyFlags.Data, frame);
        return true;
    }

    protected internal override bool TryWriteObjectData(Span<byte> destination, uint frame)
    {
        return TryGetObjectData(out var data, frame) && WriteStruct(data, destination);
    }

    void UpdateMyProperties()
    {
        // if (dirtyObjectProperties.HasFlag(DirtyFlags.Size))
        // {
        //     Console.WriteLine("Dirty size: " + dirtyObjectProperties.HasFlag(DirtyFlags.Size));
        //     Console.WriteLine("ShouldUpdateStyle: " + base.Style.ShouldUpdateStyle);
        //     Console.WriteLine("mySizeHasChanged: " + mySizeHasChanged);
        // }
        if (dirtyObjectProperties.HasFlag(DirtyFlags.Size))
        // if ((dirtyObjectProperties.HasFlag(DirtyFlags.Size) || base.Style.ShouldUpdateStyle) && mySizeHasChanged)
        {
            // Console.WriteLine("Size:" + computedStyle.Size);
            UpdateMySize();
        }

        // if (dirtyObjectProperties.HasFlag(DirtyFlags.Layout))
        // {
        //     UpdateArrangement();

        //     dirtyObjectProperties &= ~DirtyFlags.Layout;
        // }
    }

    public void UpdateArrangement()
    {
        if (Swapchain.Instance.recreatedSwapChain)
        {
            if (Parent == null)
                dirtyObjectProperties |= DirtyFlags.Size;
            AddFlag(RenderDirtyFlags.Matrix);
        }

        if (dirtyObjectProperties == DirtyFlags.None) return;
        Console.WriteLine("======================== Updating arrangement for " + Style.Name + " " + dirtyObjectProperties + "================================");

        UpdateMyProperties();
        TestToUpdateStyle();

        Console.WriteLine("Will update arrangement: " + (!dirtyObjectProperties.HasFlag(DirtyFlags.Layout) && !mySizeHasChanged) + "\nIs layout dirty: " + dirtyObjectProperties.HasFlag(DirtyFlags.Layout) + "\nMy size has changed: " + mySizeHasChanged);
        if (!dirtyObjectProperties.HasFlag(DirtyFlags.Layout) && !mySizeHasChanged) return;
        dirtyObjectProperties &= ~DirtyFlags.Layout;
        Console.WriteLine("Updating layout");

        if (Children == null) return;
        float sizeOfLine = 0;
        var _paddingLeft = computedStyle.Padding.X;
        var _paddingTop = computedStyle.Padding.Y;
        float innerWidth = computedStyle.Size.X - _paddingLeft - computedStyle.Padding.Z;

        float cursorX = _paddingLeft;
        float cursorY = _paddingTop;
        Console.WriteLine("Updating children layout");

        foreach (var child in Children)
        {
            child.LayoutManager.Arrange(ref cursorX, ref cursorY, NewLine, UpdateSizeOfLine, ref innerWidth);
        }
        void NewLine()
        {
            cursorX = _paddingLeft;
            cursorY += sizeOfLine;
        }
        void UpdateSizeOfLine(float newSizeOfLine)
        {
            if (newSizeOfLine > sizeOfLine)
            {
                sizeOfLine = newSizeOfLine;
            }
        }
    }

    void UpdateMySize()
    {
        dirtyObjectProperties &= ~DirtyFlags.Size;
        dirtyObjectProperties |= DirtyFlags.Layout;
        LayoutManager.OnParentSizeChanged();
        // Layout.UpdateChildrenLayout();
        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.LayoutManager.OnParentSizeChanged();
            }
        }
        AddFlag(RenderDirtyFlags.Matrix);
    }

    public override void Compile()
    {
        base.Compile();
        AddFlag(RenderDirtyFlags.Matrix);
    }
}
