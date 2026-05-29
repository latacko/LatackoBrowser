using System.Numerics;
using Units;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace GraphicCore;

public abstract class IModelData
{
    public Buffer vertexBuffer;
    public uint vertexOffset;

    public Buffer indexBuffer;
    public uint indexOffset;

    public abstract int GetVertexCount();
    public abstract int GetIndicesCount();
}