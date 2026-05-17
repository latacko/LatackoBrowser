using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using Browser;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Units;
using Vulkan;

public class RuntimeModelData : IDisposable
{
    [Flags]
    private enum DirtyFlags : byte
    {
        None = 0,
        Matrix = 1 << 0,
        Object = 1 << 1,
        All = Matrix | Object
    }
    public ModelData<ushort> ModelData { get; private set; }
    public uint ObjectIndex;

    public Transform Transform = new();
    public Properties Properties;
    public Layout Layout;

    private DirtyFlags[] dirty = new DirtyFlags[VulkanManager.MAX_FRAMES_IN_FLIGHT];
    private Matrix4X4<float> _cachedModel;

    public Vector2D<float> ParentSize;
    internal RuntimeModelData? Parent;
    public List<RuntimeModelData> Children;

    public RuntimeModelData(ModelData<ushort> modelData, uint objectIndex, RuntimeModelData? parent = null)
    {
        ModelData = modelData;
        ObjectIndex = objectIndex;

        if (parent != null)
            Parent = parent;

        Properties = new(this);
        Layout = new(this);
        UpdateParentSize();
    }

    public void SetParent(RuntimeModelData? parent = null)
    {
        if (parent != null)
            Parent = parent;
        UpdateParentSize();
    }

    void AddFlag(DirtyFlags flags)
    {
        for (int i = 0; i < VulkanManager.MAX_FRAMES_IN_FLIGHT; i++)
        {
            dirty[i] |= flags;
        }
    }

    void RemoveFlag(DirtyFlags flags, uint frame)
    {
        dirty[frame] &= flags;
    }

    public RuntimeModelData SetLayout(Func<Layout, RuntimeModelData, Layout> setLayout)
    {
        return SetLayout(setLayout.Invoke(Layout, this));
    }

    public RuntimeModelData SetLayout(Layout layout)
    {
        Layout = layout;
        if (Layout.dirty.HasFlag(Layout.LayoutDirty.Position) || Layout.dirty.HasFlag(Layout.LayoutDirty.Size))
        {
            if (Layout.dirty.HasFlag(Layout.LayoutDirty.Size))
                UpdateChildrenSizes();
            if (Layout.dirty.HasFlag(Layout.LayoutDirty.Position))
                UpdateChildrenPosition();

            Layout.dirty &= Layout.LayoutDirty.Position;
            Layout.dirty &= Layout.LayoutDirty.Size;

            AddFlag(DirtyFlags.Matrix);
        }
        return this;
    }

    public RuntimeModelData SetTransform(Func<Transform, RuntimeModelData, Transform> setTransform)
    {
        return SetTransform(setTransform.Invoke(Transform, this));
    }

    public RuntimeModelData SetTransform(Transform transform)
    {
        Transform = transform;
        AddFlag(DirtyFlags.Matrix);
        return this;
    }

    public RuntimeModelData SetProperties(Func<Properties, RuntimeModelData, Properties> setProperties)
    {
        return SetProperties(setProperties.Invoke(Properties, this));
    }

    public RuntimeModelData SetProperties(Properties properties)
    {
        Properties = properties;
        AddFlag(DirtyFlags.Object);
        return this;
    }

    public UIUnit CreateUnit(float value, UnitType valueType = UnitType.px)
    {
        return new UIUnit(value, valueType).ConvertToPx(ParentSize);
    }

    void ConvertToPx(Vector2D<float> parentSize)
    {
        Layout.ConvertToPx(parentSize);
        Transform.ConvertToPx(parentSize);
        Properties.ConvertToPx(parentSize);

        AddFlag(DirtyFlags.Matrix);
    }

    void UpdateChildrenSizes()
    {
        if (Children == null) return;

        foreach (var children in Children)
        {
            children.UpdateParentSize(Layout.GetSize());
        }
    }

    void UpdateChildrenPosition()
    {
        if (Children == null) return;

        foreach (var child in Children)
        {
            child.AddFlag(DirtyFlags.Matrix);
            child.Layout.UpdateBoundsOffset();
            child.UpdateChildrenPosition();
        }
    }

    internal void UpdateParentSize(Vector2D<float> size = default)
    {
        if (size != default)
        {
            ParentSize = size;
        }
        else
        {
            ParentSize = Parent != null ? Parent.Layout.GetSize() : new(Swapchain.Instance.swapChainExtent.Width, Swapchain.Instance.swapChainExtent.Height);
        }
        var _prevSize = Layout.GetSize();
        ConvertToPx(ParentSize);
        if (_prevSize != Layout.GetSize())
            UpdateChildrenSizes();
    }

    public bool TryGetObjectData(out ObjectData data, uint frame)
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
            _cachedModel =
                Matrix4X4.CreateScale(Layout.GetSize().X, Layout.GetSize().Y, 1f) *
                Matrix4X4.CreateTranslation(-Transform.TranslateX.Value, -Transform.TranslateY.Value, 0f) *
                Matrix4X4.CreateFromYawPitchRoll(Transform.Rotation.X, Transform.Rotation.Y, Transform.Rotation.Z) *
                (
                    Parent == null ?
                        Matrix4X4.CreateTranslation(Layout.Left.Value, Layout.Top.Value, 0f) :
                        Matrix4X4.CreateTranslation(Parent.Layout.Left.Value + Layout.LayoutPos.X + Layout.Left.Value, Parent.Layout.Top.Value + Layout.LayoutPos.Y + Layout.Top.Value, 0f)
                );
            RemoveFlag(DirtyFlags.Matrix, frame);
        }

        data = new ObjectData
        {
            Model = _cachedModel,
            Color = Properties.BackgroundColor,

            pos = new Vector2D<float>(_cachedModel.M41, _cachedModel.M42),
            size = Layout.GetSize(),

            TextureIndex = 0,
            borderRadiusTopLeft = Properties.borderRadiusTopLeft.Value,
            borderRadiusTopRight = Properties.borderRadiusTopRight.Value,
            borderRadiusBottomRight = Properties.borderRadiusBottomRight.Value,
            borderRadiusBottomLeft = Properties.borderRadiusBottomLeft.Value,
        };

        RemoveFlag(DirtyFlags.Object, frame);

        return true;
    }

    public void Dispose()
    {
    }
}