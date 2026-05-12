using Browser;
using Silk.NET.Maths;

public class RuntimeModelData : IDisposable
{
    public ModelData<ushort> ModelData { get; private set; }
    public int ObjectIndex; // index into shader's object storage buffer

    public UIVector2 Pos;
    public UIVector2 Size;
    public float RotationX { get; private set; }
    public float RotationY { get; private set; }
    public float RotationZ { get; private set; }

    public Vector4D<float> BackgroundColor = new(1, 1, 1, 1);

    private bool _matrixDirty = true;
    private Matrix4X4<float> _cachedModel;

    public RuntimeModelData(ModelData<ushort> modelData, int objectIndex)
    {
        ModelData = modelData;
        ObjectIndex = objectIndex;
    }

    public void SetPosition(UIVector2 pos)
    {
        if (Pos == pos) return;
        Pos = pos;
        _matrixDirty = true;
    }

    public void SetSize(UIVector2 size)
    {
        if (this.Size == size) return;
        Size = size;
        _matrixDirty = true;
    }

    public void SetRotation(float x, float y, float z)
    {
        if (RotationX == x && RotationY == y && RotationZ == z) return;
        RotationX = x; RotationY = y; RotationZ = z;
        _matrixDirty = true;
    }

    public void SetBackgroundColor255(float r, float g, float b, float a)
    {
        BackgroundColor = new(r/255, g/255, b/255, a);
        _matrixDirty = true;
    }

    public void SetBackgroundColor(float r, float g, float b, float a)
    {
        BackgroundColor = new(r, g, b, a);
        _matrixDirty = true;
    }

    // returns true if object buffer needs updating
    public bool TryGetObjectData(out ObjectData data)
    {
        if (!_matrixDirty && !BrowserWindow.recreatedSwapChain)
        {
            data = default;
            return false;
        }

        if (BrowserWindow.recreatedSwapChain)
        {
            Pos.ConvertToPx();
            Size.ConvertToPx();
        }

        float centerX = Pos.X + Size.X / 2f;
        float centerY = Pos.Y + Size.Y / 2f;

        _cachedModel =
            Matrix4X4.CreateScale(Size.X, Size.Y, 1f) *
            Matrix4X4.CreateFromYawPitchRoll(RotationY, RotationX, RotationZ) *
            Matrix4X4.CreateTranslation(Pos.X, Pos.Y, 0f);

        data = new ObjectData
        {
            Model = _cachedModel,
            Color = BackgroundColor,
            hasTexture = 0,
            hasTexture2 = 0,
        };

        _matrixDirty = false;
        return true;
    }

    public unsafe void Dispose()
    {
    }
}