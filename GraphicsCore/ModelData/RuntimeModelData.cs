using System.Runtime.CompilerServices;
using GraphicsCore;
using Silk.NET.Maths;
using Units;
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
        Data = 1 << 1,
        Model = 1 << 2,
        All = Matrix | Data | Model
    }
    public IModelData ModelData;
    public uint ObjectIndex;

    public EventsBase Events;

    internal protected DirtyFlags[] dirty = new DirtyFlags[VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    internal protected Matrix4X4<float> cachedModel;
    internal protected Vector3D<float> relativePos;
    internal protected Vector3D<float> relativeRot;
    internal protected Vector3D<float> relativeTransformation;

    public Vector2D<float> ParentSize;
    public RuntimeModelData? Parent;

    public RuntimeModelData(IModelData modelData, uint objectIndex, RuntimeModelData? parent = null)
    {
        ModelData = modelData;
        ObjectIndex = objectIndex;

        if (parent != null)
            Parent = parent;

        UpdateMySize(true);
    }

    public void SetParent(RuntimeModelData? parent = null)
    {
        if (parent != null)
            Parent = parent;
        UpdateMySize(true);
    }

    protected internal void AddFlag(DirtyFlags flags)
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            dirty[i] |= flags;
        }
    }

    protected internal void RemoveFlag(DirtyFlags flags, uint frame)
    {
        dirty[frame] &= ~flags;
    }

    protected internal abstract void ConvertToPx();

    protected internal abstract void UpdatePosition();

    protected internal abstract float GetLayoutLeft();
    protected internal abstract float GetLayoutTop();
    protected internal abstract Vector2D<float> GetLayoutSize();
    protected internal abstract void UpdateLayout(ref float cursorX, ref float cursorY, Action newLine, Action<float> sizeOfLine, ref float width);
    public abstract void AddChild(RuntimeModelData runtimeModelData);

    public abstract CursorType GetCursorType();
    public abstract Bounds GetBounds();



    protected internal virtual void UpdateMySize(bool informChildren = false)
    {
        if (Parent != null)
        {
            ParentSize = Parent.GetLayoutSize();
        }
    }

    public void Dispose()
    {
    }
}

public abstract class RuntimeModelData<TSelf, TObjectData, TModelData> : RuntimeModelData
    where TSelf : RuntimeModelData<TSelf, TObjectData, TModelData>
    where TObjectData : unmanaged
    where TModelData : IModelData
{
    public new Events<TSelf> Events
    {
        get => (Events<TSelf>)base.Events;
        protected set => base.Events = value;
    }

    public new TModelData ModelData
    {
        get => (TModelData)base.ModelData;
        protected set => base.ModelData = value;
    }

    public TSelf SetEvents(Func<Events<TSelf>, Events<TSelf>> setEvents)
    {
        Events = setEvents.Invoke(Events);
        return (TSelf)this;
    }

    protected RuntimeModelData(TModelData modelData, uint objectIndex, RuntimeModelData? parent = null)
        : base(modelData, objectIndex, parent) { }

    public abstract bool TryGetObjectData(out TObjectData data, uint frame);
}