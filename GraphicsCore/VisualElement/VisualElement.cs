using System.Runtime.CompilerServices;
using AssetCore;
using GraphicsCore.Styles;
using GraphicsCore;
using Silk.NET.Maths;
using Units;
using Vulkan;
using GraphicsCore.Events;
using System.Runtime.InteropServices;
using GraphicsCore.Buffers;

[assembly: InternalsVisibleTo("PrimitiveCore")]
[assembly: InternalsVisibleTo("TextCore")]
namespace GraphicsCore;


public abstract class VisualElement : IDisposable, IInstance
{
    public struct ObjectComileInfo
    {
        public VisualElement runtimeModel;
        public BaseShader shader;
    }
    public static readonly Dictionary<uint, List<ObjectComileInfo>> ObjectsToCompile = [];

    public uint ModelId { get; private set; }
    public uint InstanceIndex { get; private set; }

    [Flags]
    internal protected enum RenderDirtyFlags : byte
    {
        None = 0,
        Matrix = 1 << 0,
        Data = 1 << 1,
        Model = 1 << 2,
        InstanceId = 1 << 3,
    }
    internal protected RenderDirtyFlags[] renderDirty = new RenderDirtyFlags[VulkanEngine.MAX_FRAMES_IN_FLIGHT];



    internal protected Matrix4X4<float> cachedModel;
    internal protected Vector3D<float> relativePos;
    internal protected Vector3D<float> relativeRot;
    internal protected Vector3D<float> relativeTransformation;
    internal protected bool mySizeHasChanged;

    public VisualElement? Parent;

    public ILayoutManager LayoutManager { get; protected set; }

    public readonly EventHelper Events;

    private readonly StylesManager? stylesManager;
    public Style? Style;
    internal protected ComputedStyle computedStyle;

    public VisualElement(EventSystem? eventSystem, VisualElement? parent = null)
    {
        if (eventSystem != null)
            Events = new(eventSystem, this);

        if (parent != null)
            Parent = parent;
    }

    public VisualElement(uint modelId, uint instanceIndex, EventSystem? eventSystem, StylesManager stylesManager, VisualElement? parent = null)
    {
        ModelId = modelId;
        InstanceIndex = instanceIndex;

        this.stylesManager = stylesManager;

        if (eventSystem != null)
            Events = new(eventSystem, this);

        if (parent != null)
            Parent = parent;
    }

    internal protected void SetParent(VisualElement? parent = null)
    {
        if (parent != null)
            Parent = parent;
        LayoutManager.OnParentSizeChanged(true);
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

    public void ChangeInstanceId(uint newInstanceId)
    {
        AddFlag(RenderDirtyFlags.InstanceId);
        InstanceIndex = newInstanceId;
    }

    public abstract void AddChild(VisualElement visualElement);
    public abstract void RemoveChild(VisualElement visualElement);

    public abstract CursorType GetCursorType();

    public void TestToUpdateStyle()
    {
        if (Style == null) return;
        // Console.WriteLine(Style.Name + " is set as to update: " + Style.ShouldUpdateStyle);
        if (!Style.ShouldUpdateStyle) return;
        
        var _prevSize = computedStyle.Size;
        // Console.WriteLine("Updating style for " + Style.Name);
        computedStyle = Style.ComputeStyles(false, true, LayoutManager.GetParentSize(), computedStyle.Size);
        mySizeHasChanged = _prevSize != computedStyle.Size;
        // Console.WriteLine("My new size: " + computedStyle.Size + " my prev size: " + _prevSize);
    }

    public virtual void Compile()
    {
        LayoutManager.OnParentSizeChanged();

        if (Style != null)
            computedStyle = Style.ComputeStyles(false, true, LayoutManager.GetParentSize(), computedStyle.Size);
    }

    public VisualElement SetEvents(Action<EventHelper> setEvents)
    {
        setEvents.Invoke(Events);
        return this;
    }

    public VisualElement SetInlineStyle(Style style)
    {
        Style = style;
        stylesManager.AddInlineStyle(style);
        // computedStyle = style.ComputeStyles(true, false, LayoutManager.GetParentSize(), computedStyle.Size);
        return this;
    }

    public VisualElement SetStyle(string name)
    {
        // Style = StylesManager.GetStyle(name);
        return this;
    }

    public void SetModelId(uint modelId)
    {
        ModelId = modelId;
    }

    public void SetInstanceIndex(uint instanceIndex)
    {
        InstanceIndex = instanceIndex;
    }

    protected internal abstract bool TryWriteObjectData(Span<byte> destination, uint frame);
    protected internal abstract int ObjectDataSize { get; }

    protected static bool WriteStruct<T>(in T data, Span<byte> destination) where T : unmanaged
    {
        if (destination.Length < Unsafe.SizeOf<T>()) return false;
        MemoryMarshal.Write(destination, in data);
        return true;
    }
    public void Dispose()
    {
    }
}