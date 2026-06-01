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
                ConvertToPx();
                UpdateMySize();
            }
            if (Layout.dirty.HasFlag(Layout.LayoutDirty.Position))
            {
                UpdatePosition();
            }

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
        ConvertToPx();
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
        ConvertToPx();
        AddFlag(DirtyFlags.Data);
        return this;
    }

    protected internal override void UpdateMySize(bool informChildren = false)
    {
        base.UpdateMySize(informChildren);

        var _prevSize = Layout.GetSize();
        ConvertToPx();

        if (_prevSize == Layout.GetSize()) return;
        Layout.UpdateChildrenLayout();


        if (!informChildren || Children == null) return;

        foreach (var children in Children)
        {
            children.UpdateMySize();
        }
    }

    protected internal override void UpdatePosition()
    {
        relativePos = (Parent != null ? Parent.relativePos : new Vector3D<float>()) + new Vector3D<float>(GetLayoutLeft(), GetLayoutTop(), 0);
        relativeRot = (Parent != null ? Parent.relativeRot : new Vector3D<float>()) + Transform.Rotation;
        relativeTransformation = (Parent != null ? Parent.relativeTransformation : new Vector3D<float>()) + new Vector3D<float>(Transform.TranslateX.Value, Transform.TranslateY.Value, 0);

        AddFlag(DirtyFlags.Matrix);
        Layout.UpdateBoundsOffset();
        if (Children == null) return;

        foreach (var child in Children)
        {
            child.UpdatePosition();
        }
    }

    protected internal override void ConvertToPx()
    {
        Layout.ConvertToPx(ParentSize);
        Transform.ConvertToPx(Layout.GetSize());
        Properties.ConvertToPx(Layout.GetSize());

        AddFlag(DirtyFlags.Matrix);
    }

    protected internal override float GetLayoutLeft() => Layout.Left.Value;
    protected internal override float GetLayoutTop() => Layout.Top.Value;
    protected internal override Vector2D<float> GetLayoutSize() => Layout.GetSize();
    public override Bounds GetBounds() => Layout.Bounds;
    public override CursorType GetCursorType() => Properties.Cursor;

    protected internal override void UpdateLayout(ref float cursorX, ref float cursorY, Action newLine, Action<float> updateSizeOfLine, ref float width)
    {
        updateSizeOfLine(GetLayoutSize().Y);

        switch (Layout.Display)
        {
            case Layout.DisplayType.inline:
                if (cursorX + Layout.Width.Value > width)
                    newLine.Invoke();

                Layout.LayoutPos = new(cursorX, cursorY);
                cursorX += Layout.Width.Value;

                break;
            case Layout.DisplayType.block:
                newLine.Invoke();

                Layout.LayoutPos = new Vector2D<float>(cursorX, cursorY);
                Layout.BaseSize = new(width, 0);
                break;
        }
    }

    public override void AddChild(RuntimeModelData runtimeModelData)
    {
        Children ??= new();
        Children.Add(runtimeModelData);
        Layout.UpdateChildrenLayout();
    }

    public override bool TryGetObjectData(out ObjectData data, uint frame)
    {
        if (Swapchain.Instance.recreatedSwapChain)
        {
            UpdateMySize();
            AddFlag(DirtyFlags.Matrix);
        }

        if (dirty[frame] == DirtyFlags.None)
        {
            data = default;
            return false;
        }


        if (dirty[frame].HasFlag(DirtyFlags.Matrix))
        {
            // Task.Run(() =>
            // {
            // });
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
