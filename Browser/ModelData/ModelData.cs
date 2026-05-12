using System.Numerics;
using Browser;
using Browser.DataTypes;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public class ModelData<TIndex> where TIndex : unmanaged, IBinaryInteger<TIndex>
{
    public Vertex[] Vertices;
    public TIndex[] Indices;

    public Buffer vertexBuffer;
    internal ulong vertexOffset;

    public Buffer indexBuffer;
    internal ulong indexOffset;

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

    public void BindVertexBuffers(Vk vk, CommandBuffer commandBuffer)
    {
        vk!.CmdBindVertexBuffers(commandBuffer, 0, 1, ref vertexBuffer, ref vertexOffset);
    }

    public void BindIndexBuffer(Vk vk, CommandBuffer commandBuffer)
    {
        vk!.CmdBindIndexBuffer(commandBuffer, indexBuffer, indexOffset, IndexType.Uint16);
    }

    public int GetIndicesCount()=>Indices.Length;
}