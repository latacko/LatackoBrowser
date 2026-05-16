using Browser;
using Silk.NET.Maths;
using Units;

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
    public int ObjectIndex;

    public Vector2 Pos;
    public Vector2 Size;
    public Transform Transform = new();
    public Properties Properties = new();

    private DirtyFlags dirty = DirtyFlags.None;
    private Matrix4X4<float> _cachedModel;

    public RuntimeModelData(ModelData<ushort> modelData, int objectIndex)
    {
        ModelData = modelData;
        ObjectIndex = objectIndex;
    }

    public void SetPosition(Vector2 pos)
    {
        if (Pos == pos) return;
        Pos = pos;
        dirty |= DirtyFlags.Matrix;
    }

    public void SetSize(Vector2 size)
    {
        if (this.Size == size) return;
        Size = size;
        dirty |= DirtyFlags.Matrix;
    }

    public void SetTransform(Transform transform)
    {
        Transform = transform;
        dirty |= DirtyFlags.Matrix;
    }

    public void SetProperties(Properties properties)
    {
        Properties = properties;
        dirty |= DirtyFlags.Object;
    }


    public bool TryGetObjectData(out ObjectData data)
    {
        if (BrowserWindow.recreatedSwapChain)
        {
            Pos.ConvertToPx();
            Size.ConvertToPx();

            Transform.ConvertToPx();

            dirty |= DirtyFlags.Matrix;
        }

        if (dirty == DirtyFlags.None)
        {
            data = default;
            return false;
        }


        if (dirty.HasFlag(DirtyFlags.Matrix))
        {
            _cachedModel =
                Matrix4X4.CreateScale(Size.X, Size.Y, 1f) *
                Matrix4X4.CreateTranslation(-Transform.Translate.X, -Transform.Translate.Y, 0f)*
                Matrix4X4.CreateFromYawPitchRoll(Transform.Rotation.X, Transform.Rotation.Y, Transform.Rotation.Z)*
                Matrix4X4.CreateTranslation(Pos.X, Pos.Y, 0f);
            dirty &= DirtyFlags.Matrix;
        }

        data = new ObjectData
        {
            Model = _cachedModel,
            Color = Properties.BackgroundColor,
            hasTexture = 0,
            hasTexture2 = 0,
        };
        dirty &= DirtyFlags.Object;

        return true;
    }

    public unsafe void Dispose()
    {
    }
}