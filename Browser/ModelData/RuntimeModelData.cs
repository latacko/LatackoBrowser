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

    Rect2D bounds = new();
    public Rect2D Bounds => bounds;

    public Vector2 Pos;
    public Vector2 Size;
    public Transform Transform = new();
    public Properties Properties = new();

    private DirtyFlags[] dirty = new DirtyFlags[VulkanManager.MAX_FRAMES_IN_FLIGHT];
    private Matrix4X4<float> _cachedModel;

    public RuntimeModelData(ModelData<ushort> modelData, uint objectIndex)
    {
        ModelData = modelData;
        ObjectIndex = objectIndex;
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

    public RuntimeModelData SetPosition(Vector2 pos)
    {
        if (Pos == pos) return this;
        Pos = pos;
        
        bounds.Offset = new()
        {
            X = (int)Pos.X,
            Y = (int)Pos.Y
        };

        AddFlag(DirtyFlags.Matrix);
        return this;
    }

    public RuntimeModelData SetSize(Vector2 size)
    {
        if (this.Size == size) return this;
        Size = size;
        bounds.Extent = new()
        {
            Width = (uint)Size.X,
            Height = (uint)Size.Y
        };
        AddFlag(DirtyFlags.Matrix);
        return this;
    }

    public RuntimeModelData SetTransform(Func<Transform, Transform> setTransform)
    {
        Transform = setTransform.Invoke(Transform);
        AddFlag(DirtyFlags.Matrix);
        return this;
    }

    public RuntimeModelData SetProperties(Func<Properties, Properties> setProperties)
    {
        Properties = setProperties.Invoke(Properties);
        AddFlag(DirtyFlags.Matrix);
        return this;
    }


    public bool TryGetObjectData(out ObjectData data, uint frame)
    {
        if (Swapchain.Instance.recreatedSwapChain)
        {
            Pos.ConvertToPx();
            Size.ConvertToPx();

            Transform.ConvertToPx();

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
                Matrix4X4.CreateScale(Size.X, Size.Y, 1f) *
                Matrix4X4.CreateTranslation(-Transform.Translate.X, -Transform.Translate.Y, 0f) *
                Matrix4X4.CreateFromYawPitchRoll(Transform.Rotation.X, Transform.Rotation.Y, Transform.Rotation.Z) *
                Matrix4X4.CreateTranslation(Pos.X, Pos.Y, 0f);
            RemoveFlag(DirtyFlags.Matrix, frame);
        }

        data = new ObjectData
        {
            Model = _cachedModel,
            Color = Properties.BackgroundColor,
            TextureIndex = 0,
            hasTexture2 = 0,
        };
        RemoveFlag(DirtyFlags.Object, frame);

        return true;
    }

    public unsafe void Dispose()
    {
    }
}