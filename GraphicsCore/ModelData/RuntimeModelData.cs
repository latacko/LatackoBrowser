using System.Runtime.CompilerServices;
using GraphicsCore;
using Silk.NET.Maths;
using Vulkan;

[assembly: InternalsVisibleTo("ObjectCore")]
[assembly: InternalsVisibleTo("TextCore")]
namespace GraphicCore;

public abstract class RuntimeModelData : IDisposable
{
    [Flags]
    internal protected enum DirtyFlags : byte
    {
        None = 0,
        Matrix = 1 << 0,
        Object = 1 << 1,
        Model = 1 << 2,
        All = Matrix | Object | Model
    }
    public ModelData<ushort> ModelData { get; private set; }
    public uint ObjectIndex;

    public EventsBase Events;

    internal protected DirtyFlags[] dirty = new DirtyFlags[VulkanManager.MAX_FRAMES_IN_FLIGHT];
    internal protected Matrix4X4<float> _cachedModel;

    public Vector2D<float> ParentSize;
    public RuntimeModelData? Parent;

    public RuntimeModelData(ModelData<ushort> modelData, uint objectIndex, RuntimeModelData? parent = null)
    {
        ModelData = modelData;
        ObjectIndex = objectIndex;

        if (parent != null)
            Parent = parent;

        UpdateParentSize();
    }

    public void SetParent(RuntimeModelData? parent = null)
    {
        if (parent != null)
            Parent = parent;
        UpdateParentSize();
    }

    protected internal void AddFlag(DirtyFlags flags)
    {
        for (int i = 0; i < VulkanManager.MAX_FRAMES_IN_FLIGHT; i++)
        {
            dirty[i] |= flags;
        }
    }

    protected internal void RemoveFlag(DirtyFlags flags, uint frame)
    {
        dirty[frame] &= flags;
    }

    protected internal abstract void ConvertToPx(Vector2D<float> parentSize);

    protected internal abstract void UpdatePosition();

    protected internal abstract float GetLayoutLeft();
    protected internal abstract float GetLayoutTop();
    protected internal abstract Vector2D<float> GetLayoutSize();
    protected internal abstract void UpdateLayout(ref float cursorX, ref float cursorY, ref float sizeOfLine, ref float width);
    public abstract void AddChild(RuntimeModelData runtimeModelData);

    public abstract CursorType GetCursorType();
    public abstract Bounds GetBounds();



    protected internal virtual void UpdateParentSize(Vector2D<float> size = default)
    {
        if (size != default)
        {
            ParentSize = size;
        }
        else
        {
            ParentSize = Parent != null ? Parent.GetLayoutSize() : new(Swapchain.Instance.swapChainExtent.Width, Swapchain.Instance.swapChainExtent.Height);
        }
    }

    public void Dispose()
    {
    }
}

public abstract class RuntimeModelData<TSelf, TObjectData> : RuntimeModelData
    where TSelf : RuntimeModelData<TSelf, TObjectData> where TObjectData : unmanaged
{
    public new Events<TSelf> Events
    {
        get => (Events<TSelf>)base.Events;
        protected set => base.Events = value;
    }

    public TSelf SetEvents(Func<Events<TSelf>, Events<TSelf>> setEvents)
    {
        Events = setEvents.Invoke(Events);
        return (TSelf)this;
    }

    protected RuntimeModelData(ModelData<ushort> modelData, uint objectIndex, RuntimeModelData? parent = null)
        : base(modelData, objectIndex, parent) { }

    public abstract bool TryGetObjectData(out TObjectData data, uint frame);
}