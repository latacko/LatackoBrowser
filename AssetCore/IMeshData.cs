using Buffer = Silk.NET.Vulkan.Buffer;

namespace AssetCore;

public abstract class IMeshData
{
    public Buffer vertexBuffer;
    public uint vertexOffset;

    public Buffer indexBuffer;
    public uint indexOffset;

    public abstract int GetVertexCount();
    public abstract int GetIndicesCount();
}