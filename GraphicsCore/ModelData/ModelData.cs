using System.Numerics;
using Units;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace GraphicCore;

public class ModelData<TIndex> where TIndex : unmanaged, IBinaryInteger<TIndex>
{
    public Vertex[] Vertices;
    public TIndex[] Indices;

    public Buffer vertexBuffer;
    public uint vertexOffset;

    public Buffer indexBuffer;
    public uint indexOffset;

    public ModelData(Vertex[] vertices, TIndex[] indices)
    {
        if (typeof(TIndex) != typeof(byte) &&
            typeof(TIndex) != typeof(ushort) &&
            typeof(TIndex) != typeof(uint))
        {
            throw new Exception(typeof(TIndex).Name + "is not a valid Vulkan index type. Use byte, ushort or uint.");
        }

        Vertices = vertices;
        Indices = indices;
    }

    public int GetIndicesCount()=>Indices.Length;
}