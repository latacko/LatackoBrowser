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
    public uint InstanceIndex;

    [Flags]
    internal protected enum RenderDirtyFlags : byte
    {
        None = 0,
        Matrix = 1 << 0,
        Data = 1 << 1,
        Model = 1 << 2,
    }
    internal protected RenderDirtyFlags[] renderDirty = new RenderDirtyFlags[VulkanEngine.MAX_FRAMES_IN_FLIGHT];



    internal protected Matrix4X4<float> cachedModel;
    internal protected Vector3D<float> relativePos;
    internal protected Vector3D<float> relativeRot;
    internal protected Vector3D<float> relativeTransformation;
    internal protected bool[] mySizeHasChangedThisFrame = new bool[VulkanEngine.MAX_FRAMES_IN_FLIGHT];

    public Vector2D<float> ParentSize;
    public VisualElement? Parent;

    public readonly EventHelper Events;

    public Style? Style;
    internal protected ComputedStyle computedStyle;

    public VisualElement(uint modelId, uint objectIndex, EventSystem eventSystem, VisualElement? parent = null)
    {
        ModelId = modelId;
        InstanceIndex = objectIndex;

        Events = new(eventSystem, this);

        if (parent != null)
            Parent = parent;
    }

    public void SetParent(VisualElement? parent = null)
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

    #region Layout
    protected internal virtual float GetLayoutLeft() => computedStyle.Pos.X;
    protected internal virtual float GetLayoutTop() => computedStyle.Pos.Y;
    protected internal abstract Vector2D<float> GetLayoutSize();

    protected internal abstract void Arrange(ref float cursorX, ref float cursorY, Action newLine, Action<float> sizeOfLine, ref float width);
    protected internal abstract void UpdateChildrenLayout();
    public abstract Bounds GetBounds();
    #endregion

    public abstract void AddChild(VisualElement runtimeModelData);

    public abstract CursorType GetCursorType();

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

    public void TestToUpdateStyle(uint frame)
    {
        if (Style == null) return;

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

        if (Style != null)
            computedStyle = Style.ComputeStyles(false, true, ParentSize, computedStyle.Size);
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
        computedStyle = style.ComputeStyles(true, false, ParentSize, computedStyle.Size);
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