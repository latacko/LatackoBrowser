using Buffer = Silk.NET.Vulkan.Buffer;

namespace AssetCore;

public abstract class IMeshData
{
    public Buffer vertexBuffer;
    public uint vertexOffset;

    public Buffer indexBuffer;
    public uint indexOffset;

    /// <summary>
    /// If true the model has been loaded on the gpu
    /// </summary>
    public bool Loaded;

    public abstract int GetVertexCount();
    public abstract int GetIndicesCount();
}