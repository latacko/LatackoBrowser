using System.Runtime.CompilerServices;
using GraphicCore.Styles;
using GraphicsCore;
using Silk.NET.Maths;
using Units;
using Vulkan;

[assembly: InternalsVisibleTo("ObjectCore")]
[assembly: InternalsVisibleTo("TextCore")]
namespace GraphicCore;


public abstract class RuntimeModelData : IDisposable
{
    public struct ObjectComileInfo
    {
        public RuntimeModelData runtimeModel;
        public BaseShader shader;
    }
    public static HashSet<ObjectComileInfo> ObjectsToCompile = new();
    [Flags]
    internal protected enum RenderDirtyFlags : byte
    {
        None = 0,
        Matrix = 1 << 0,
        Data = 1 << 1,
        Model = 1 << 2,
    }
    public IModelData ModelData;
    public uint ObjectIndex;

    public EventsBase Events;

    internal protected RenderDirtyFlags[] renderDirty = new RenderDirtyFlags[VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    internal protected Matrix4X4<float> cachedModel;
    internal protected Vector3D<float> relativePos;
    internal protected Vector3D<float> relativeRot;
    internal protected Vector3D<float> relativeTransformation;
    internal protected bool[] mySizeHasChangedThisFrame = new bool[VulkanEngine.MAX_FRAMES_IN_FLIGHT];

    public Vector2D<float> ParentSize;
    public RuntimeModelData? Parent;

    public Style Style;
    internal protected ComputedStyle computedStyle;

    public RuntimeModelData(IModelData modelData, uint objectIndex, RuntimeModelData? parent = null)
    {
        ModelData = modelData;
        ObjectIndex = objectIndex;

        if (parent != null)
            Parent = parent;
    }

    public void SetParent(RuntimeModelData? parent = null)
    {
        if (parent != null)
            Parent = parent;
        OnParentSizeChanged(true);
    }

    protected internal void AddFlag(RenderDirtyFlags flags)
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            renderDirty[i] |= flags;
        }
    }

    protected internal void RemoveFlag(RenderDirtyFlags flags, uint frame)
    {
        renderDirty[frame] &= ~flags;
    }

    protected internal abstract void UpdatePosition();

    protected internal virtual float GetLayoutLeft() => computedStyle.Pos.X;
    protected internal virtual float GetLayoutTop() => computedStyle.Pos.Y;
    protected internal abstract Vector2D<float> GetLayoutSize();
    protected internal abstract void Arrange(ref float cursorX, ref float cursorY, Action newLine, Action<float> sizeOfLine, ref float width);
    protected internal abstract void UpdateChildrenLayout();
    public abstract void AddChild(RuntimeModelData runtimeModelData);

    public abstract CursorType GetCursorType();
    public abstract Bounds GetBounds();

    protected internal virtual void OnParentSizeChanged(bool informChildren = false)
    {
        if (Parent != null)
        {
            ParentSize = Parent.GetLayoutSize();
        }
        else
        {
            ParentSize = new(Swapchain.Instance.swapChainExtent.Width, Swapchain.Instance.swapChainExtent.Height);
        }
        Console.WriteLine("Updating my parent size: " + ParentSize);
    }

    public void Dispose()
    {
    }

    public void TestToUpdateStyle(uint frame)
    {
        var _prevSize = computedStyle.Size;
        if (Style.ShouldObjectUpdate[frame])
        {
            computedStyle = Style.ComputeStyles(false, true, ParentSize, computedStyle.Size);
        }

        mySizeHasChangedThisFrame[frame] = _prevSize != computedStyle.Size;
    }

    public virtual void Compile()
    {
        OnParentSizeChanged();
        computedStyle = Style.ComputeStyles(false, true, ParentSize, computedStyle.Size);
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

    public TSelf SetStyle(Style style)
    {
        Style = style;
        StylesManager.AddInlineStyle(style);
        computedStyle = style.ComputeStyles(true, false, ParentSize, computedStyle.Size);
        return (TSelf)this;
    }

    public TSelf SetStyle(string name)
    {
        Style = StylesManager.GetStyle(name);
        return (TSelf)this;
    }

    protected RuntimeModelData(TModelData modelData, uint objectIndex, RuntimeModelData? parent = null)
        : base(modelData, objectIndex, parent) { }

    public abstract bool TryGetObjectData(out TObjectData data, uint frame);
}