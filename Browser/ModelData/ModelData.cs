using System.Numerics;
using Browser;
using Browser.DataTypes;
using Silk.NET.Vulkan;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public class ModelData<TIndex> where TIndex : unmanaged, IBinaryInteger<TIndex>
{
    public Vertex[] Vertices;
    public TIndex[] Indices;

    public Buffer vertexBuffer;
    internal uint vertexOffset;

    public Buffer indexBuffer;
    internal uint indexOffset;

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