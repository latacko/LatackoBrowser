using System.Runtime.CompilerServices;
using AssetCore;
using GraphicsCore.Styles;
using GraphicsCore;
using Silk.NET.Maths;
using Units;
using Vulkan;
using GraphicsCore.Events;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("PrimitiveCore")]
[assembly: InternalsVisibleTo("TextCore")]
namespace GraphicsCore;


public abstract class VisualElement : IDisposable
{
    public struct ObjectComileInfo
    {
        public VisualElement runtimeModel;
        public BaseShader shader;
    }
    public static HashSet<ObjectComileInfo> ObjectsToCompile = new();

    public readonly uint ModelId;
    public uint InstanceIndex {get; private set;}

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
    internal protected bool[] mySizeHasChangedThisFrame = new bool[VulkanEngine.MAX_FRAMES_IN_FLIGHT];

    public VisualElement? Parent;

    public ILayoutManager LayoutManager {get; protected set;}

    public readonly EventHelper Events;

    public Style? Style;
    internal protected ComputedStyle computedStyle;

    public VisualElement(uint modelId, uint instanceIndex, EventSystem? eventSystem, VisualElement? parent = null)
    {
        ModelId = modelId;
        InstanceIndex = instanceIndex;

        if (eventSystem != null)
            Events = new(eventSystem, this);

        if (parent != null)
            Parent = parent;
    }

    public void SetParent(VisualElement? parent = null)
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

    public abstract void AddChild(VisualElement runtimeModelData);

    public abstract CursorType GetCursorType();

    public void TestToUpdateStyle(uint frame)
    {
        if (Style == null) return;

        var _prevSize = computedStyle.Size;
        if (Style.ShouldObjectUpdate[frame])
        {
            computedStyle = Style.ComputeStyles(false, true, LayoutManager.GetParentSize(), computedStyle.Size);
        }

        mySizeHasChangedThisFrame[frame] = _prevSize != computedStyle.Size;
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

    public VisualElement SetStyle(Style style)
    {
        Style = style;
        // StylesManager.AddInlineStyle(style);
        computedStyle = style.ComputeStyles(true, false, LayoutManager.GetParentSize(), computedStyle.Size);
        return this;
    }

    public VisualElement SetStyle(string name)
    {
        // Style = StylesManager.GetStyle(name);
        return this;
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