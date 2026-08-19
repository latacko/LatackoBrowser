using System.Diagnostics;
using System.Runtime.CompilerServices;
using GraphicsCore;
using GraphicsCore.Events;
using PrimitiveCore.Textures;
using Silk.NET.Maths;
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

    DirtyFlags objectDirty;
    Vector2D<float> worldPosition;

    public List<VisualElement>? Children;
    public Texture texture;

    protected internal override int ObjectDataSize => Unsafe.SizeOf<ModelGPUData>();

    public Node(uint modelId, uint objectIndex, Texture texture, EventSystem eventSystem, VisualElement? parent = null) : base(modelId, objectIndex, eventSystem, parent)
    {
        this.texture = texture;
    }

    public override void AddChild(VisualElement runtimeModelData)
    {
        Children ??= [];
        Children.Add(runtimeModelData);

        objectDirty |= DirtyFlags.Layout;
    }

    protected internal override void OnParentSizeChanged(bool informChildren = false)
    {
        base.OnParentSizeChanged(informChildren);
        // Console.WriteLine("Parent size changed");
        objectDirty |= DirtyFlags.Size;
    }

    protected internal override void UpdatePosition()
    {
        relativePos = (Parent != null ? Parent.relativePos : new Vector3D<float>()) + new Vector3D<float>(GetLayoutLeft(), GetLayoutTop(), 0);
        relativeRot = (Parent != null ? Parent.relativeRot : new Vector3D<float>()) + Style.Transform.Rotation;
        relativeTransformation = (Parent != null ? Parent.relativeTransformation : new Vector3D<float>()) + new Vector3D<float>(computedStyle.Translate.X, computedStyle.Translate.Y, 0);

        AddFlag(RenderDirtyFlags.Matrix);
        // Layout.UpdateBoundsOffset();
        if (Children == null) return;

        foreach (var child in Children)
        {
            child.UpdatePosition();
        }
    }

    protected internal override void UpdateChildrenLayout()
    {
        // Layout.UpdateChildrenLayout();
    }

    protected internal override void Arrange(ref float cursorX, ref float cursorY, Action newLine, Action<float> updateSizeOfLine, ref float width)
    {
        updateSizeOfLine(GetLayoutSize().Y);

        switch (Style.Layout.Display)
        {
            case GraphicsCore.Styles.Layout.DisplayType.inline:
                if (cursorX + computedStyle.Size.X > width)
                    newLine.Invoke();

                worldPosition = new(cursorX, cursorY);
                cursorX += computedStyle.Size.X;

                break;
            case GraphicsCore.Styles.Layout.DisplayType.block:
                newLine.Invoke();

                worldPosition = new(cursorX, cursorY);
                // Layout.BaseSize = new(width, 0);
                break;
        }
    }

    protected internal override Vector2D<float> GetLayoutSize() => computedStyle.Size;
    public override Bounds GetBounds() => new();
    public override CursorType GetCursorType() => Style.Properties.Cursor;

    bool TryGetObjectData(out ModelGPUData data, uint frame)
    {
        if (Swapchain.Instance.recreatedSwapChain)
        {
            if (Parent == null)
                objectDirty |= DirtyFlags.Size;
            AddFlag(RenderDirtyFlags.Matrix);
        }

        UpdateObjectDirty(frame);

        if (renderDirty[frame] == RenderDirtyFlags.None)
        {
            data = default;
            return false;
        }


        if (renderDirty[frame].HasFlag(RenderDirtyFlags.Matrix))
        {
            cachedModel =
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

    void UpdateObjectDirty(uint frame)
    {
        if ((objectDirty.HasFlag(DirtyFlags.Size) || base.Style.ShouldObjectUpdate[frame]) && mySizeHasChangedThisFrame[frame])
        {
            // Console.WriteLine("Size:" + computedStyle.Size);
            UpdateMySize(frame);

            objectDirty &= ~DirtyFlags.Size;

            objectDirty |= DirtyFlags.Layout;
        }

        if (objectDirty.HasFlag(DirtyFlags.Layout))
        {
            UpdateArrangement();

            objectDirty &= ~DirtyFlags.Layout;
        }
    }

    void UpdateArrangement()
    {
        if (Children == null) return;
        float sizeOfLine = 0;
        var _paddingLeft = computedStyle.Padding.X;
        var _paddingTop = computedStyle.Padding.Y;
        float innerWidth = computedStyle.Size.X - _paddingLeft - computedStyle.Padding.Z;

        float cursorX = _paddingLeft;
        float cursorY = _paddingTop;

        foreach (var child in Children)
        {
            child.Arrange(ref cursorX, ref cursorY, NewLine, UpdateSizeOfLine, ref innerWidth);
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

    void UpdateMySize(uint frame)
    {
        // Console.WriteLine("Update my size: " + computedStyle.Size);
        // Layout.UpdateChildrenLayout();
        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.OnParentSizeChanged();
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
