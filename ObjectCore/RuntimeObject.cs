using GraphicCore;
using GraphicsCore;
using ObjectCore.Textures;
using Silk.NET.Maths;
using Vulkan;

namespace ObjectCore;

public class RuntimeObject : RuntimeModelData<RuntimeObject, ObjectData, ObjectModelData<ushort>>
{
    public Transform Transform = new();
    public Properties Properties;
    public Layout Layout;

    public List<RuntimeModelData> Children;
    public Texture texture;

    public RuntimeObject(ObjectModelData<ushort> modelData, uint objectIndex, Texture texture, RuntimeModelData? parent = null) : base(modelData, objectIndex, parent)
    {
        Properties = new(this);
        Layout = new(this);
        Events = new(this);

        this.texture = texture;
    }

    public RuntimeObject SetLayout(Func<Layout, Layout> setLayout)
    {
        return SetLayout(setLayout.Invoke(Layout));
    }

    public RuntimeObject SetLayout(Layout layout)
    {
        Layout = layout;
        if (Layout.dirty.HasFlag(Layout.LayoutDirty.Position) || Layout.dirty.HasFlag(Layout.LayoutDirty.Size))
        {
            if (Layout.dirty.HasFlag(Layout.LayoutDirty.Size))
            {
                ConvertToPx(ParentSize);
                UpdateChildrenSizes();
            }
            if (Layout.dirty.HasFlag(Layout.LayoutDirty.Position))
                UpdatePosition();

            Layout.dirty &= Layout.LayoutDirty.Position;
            Layout.dirty &= Layout.LayoutDirty.Size;

            AddFlag(DirtyFlags.Matrix);
        }
        return this;
    }

    public RuntimeObject SetTransform(Func<Transform, Transform> setTransform)
    {
        return SetTransform(setTransform.Invoke(Transform));
    }

    public RuntimeObject SetTransform(Transform transform)
    {
        Transform = transform;
        AddFlag(DirtyFlags.Matrix);
        return this;
    }

    public RuntimeObject SetProperties(Func<Properties, Properties> setProperties)
    {
        return SetProperties(setProperties.Invoke(Properties));
    }

    public RuntimeObject SetProperties(Properties properties)
    {
        Properties = properties;
        AddFlag(DirtyFlags.Data);
        return this;
    }

    protected internal override void UpdateParentSize(Vector2D<float> size = default)
    {
        base.UpdateParentSize(size);

        var _prevSize = Layout.GetSize();
        ConvertToPx(ParentSize);
        if (_prevSize != Layout.GetSize())
            UpdateChildrenSizes();
    }

    protected void UpdateChildrenSizes()
    {
        relativePos = (Parent != null ? Parent.relativePos: new Vector3D<float>()) + new Vector3D<float>(GetLayoutLeft(), GetLayoutTop(), 0);
        relativeRot = (Parent != null ? Parent.relativeRot: new Vector3D<float>()) + Transform.Rotation;
        if (Children == null) return;

        foreach (var children in Children)
        {
            children.UpdateParentSize(Layout.GetSize());
        }
    }

    protected internal override void UpdatePosition()
    {
        AddFlag(DirtyFlags.Matrix);
        Layout.UpdateBoundsOffset();
        if (Children == null) return;

        foreach (var child in Children)
        {
            child.UpdatePosition();
        }
    }

    protected internal override void ConvertToPx(Vector2D<float> parentSize)
    {
        Layout.ConvertToPx(parentSize);
        Transform.ConvertToPx(parentSize);
        Properties.ConvertToPx(Layout.GetSize());

        AddFlag(DirtyFlags.Matrix);
    }

    protected internal override float GetLayoutLeft() => Layout.Left.Value;
    protected internal override float GetLayoutTop() => Layout.Top.Value;
    protected internal override Vector2D<float> GetLayoutSize() => Layout.GetSize();
    public override Bounds GetBounds() => Layout.Bounds;
    public override CursorType GetCursorType() => Properties.Cursor;

    protected internal override void UpdateLayout(ref float cursorX, ref float cursorY, ref float sizeOfLine, ref float width)
    {
        switch (Layout.Display)
        {
            case Layout.DisplayType.inline:
                if (cursorX + Layout.Width.Value > width)
                {
                    cursorX = 0;
                    cursorY += sizeOfLine;
                }

                Layout.LayoutPos = new(cursorX, cursorY);
                cursorX += Layout.Width.Value;

                sizeOfLine = Math.Max(sizeOfLine, Layout.Height.Value);
                break;
            case Layout.DisplayType.block:
                cursorX = 0;
                cursorY += sizeOfLine > 0 ? sizeOfLine : 0;
                Layout.LayoutPos = new Vector2D<float>(cursorX, cursorY);
                Layout.BaseSize = new(width, 0);
                break;
        }
    }

    public override void AddChild(RuntimeModelData runtimeModelData)
    {
        Children ??= new();
        Children.Add(runtimeModelData);
    }

    public override bool TryGetObjectData(out ObjectData data, uint frame)
    {
        if (Swapchain.Instance.recreatedSwapChain)
        {
            UpdateParentSize();
            AddFlag(DirtyFlags.Matrix);
        }

        if (dirty[frame] == DirtyFlags.None)
        {
            data = default;
            return false;
        }


        if (dirty[frame].HasFlag(DirtyFlags.Matrix))
        {
            cachedModel =
                Matrix4X4.CreateScale(Layout.GetSize().X, Layout.GetSize().Y, 1f) *
                Matrix4X4.CreateTranslation(-Transform.TranslateX.Value, -Transform.TranslateY.Value, 0f) *
                Matrix4X4.CreateFromYawPitchRoll(Transform.Rotation.X, Transform.Rotation.Y, Transform.Rotation.Z) *
                Matrix4X4.CreateTranslation(Layout.Left.Value, Layout.Top.Value, 0f);

            if (Parent != null)
            {
                cachedModel *=
                    Matrix4X4.CreateFromYawPitchRoll(Parent.relativeRot.X, Parent.relativeRot.Y, Parent.relativeRot.Z) *
                    Matrix4X4.CreateTranslation(Parent.relativePos.X, Parent.relativePos.Y, Parent.relativePos.Z);
            }
            RemoveFlag(DirtyFlags.Matrix, frame);
        }

        data = new ObjectData
        {
            Model = cachedModel,
            Color = Properties.BackgroundColor,

            pos = new Vector2D<float>(cachedModel.M41, cachedModel.M42),
            size = Layout.GetSize(),

            TextureIndex = texture == null ? uint.MaxValue : texture.GetID(),
            borderRadiusTopLeft = Properties.borderRadiusTopLeft.Value,
            borderRadiusTopRight = Properties.borderRadiusTopRight.Value,
            borderRadiusBottomRight = Properties.borderRadiusBottomRight.Value,
            borderRadiusBottomLeft = Properties.borderRadiusBottomLeft.Value,
        };

        RemoveFlag(DirtyFlags.Data, frame);

        return true;
    }
}
