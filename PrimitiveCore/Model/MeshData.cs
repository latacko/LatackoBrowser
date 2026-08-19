using System.Numerics;
using AssetCore;
using GraphicsCore;
using Units;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace PrimitiveCore.Model;

public class MeshData<TIndex> : IMeshData where TIndex : unmanaged, IBinaryInteger<TIndex>
{
    public MeshVertex[] Vertices;
    public TIndex[] Indices;

    public MeshData(MeshVertex[] vertices, TIndex[] indices)
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

    public override int GetIndicesCount()=>Indices.Length;

    public override int GetVertexCount()=>Vertices.Length;
}