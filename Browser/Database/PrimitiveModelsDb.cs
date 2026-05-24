using System.Numerics;
using Browser;
using GraphicCore;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsWPF;
using Silk.NET.Vulkan;
using Units;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public class PrimitiveModelsDb : IDisposable
{
    internal static Buffer primitiveBuffer;
    internal static ulong indicesOffset;
    DeviceMemory primitiveBufferMemory;

    readonly Dictionary<PrimitiveUIModel, ModelData<ushort>> uiDb = new()
    {
        [PrimitiveUIModel.Quad] = new ModelData<ushort>(
            [
                new Vertex(new(0,0,0), new(0,0)),
                new Vertex(new(0,1,0), new(0,1)),
                new Vertex(new(1,1,0), new(1,1)),
                new Vertex(new(1,0,0), new(1,0)),
            ],
            [0, 1, 2, 2, 3, 0])
    };

    /// <summary>
    /// Creates two buffers for primitives models. Vertex and Indices buffer
    /// </summary>
    internal unsafe void CreateBuffers()
    {
        int _vertexCount = 0;
        int _indexCount = 0;
        foreach (var primitiveModelInfo in uiDb)
        {
            _vertexCount += primitiveModelInfo.Value.Vertices.Length;
            _indexCount += primitiveModelInfo.Value.Indices.Length;
        }

        Vertex[] vertices = new Vertex[_vertexCount];
        ushort[] indices = new ushort[_indexCount];

        int verticesIndex = 0;
        int indicesIndex = 0;

        foreach (var primitiveModelInfo in uiDb)
        {
            Array.Copy(primitiveModelInfo.Value.Vertices, 0, vertices, verticesIndex, primitiveModelInfo.Value.Vertices.Length);
            Array.Copy(primitiveModelInfo.Value.Indices, 0, indices, indicesIndex, primitiveModelInfo.Value.Indices.Length);

            primitiveModelInfo.Value.vertexOffset = (uint)verticesIndex;
            primitiveModelInfo.Value.indexOffset = (uint)indicesIndex;

            verticesIndex += primitiveModelInfo.Value.Vertices.Length;
            indicesIndex += primitiveModelInfo.Value.Indices.Length;
        }

        CreateVertexAndIndicesBuffer(vertices, indices);
        foreach (var primitiveModelInfo in uiDb)
        {
            primitiveModelInfo.Value.vertexBuffer = primitiveBuffer;
        }


        Console.WriteLine("Created vertices buffer " + primitiveBuffer);
    }

    public unsafe void CreateVertexAndIndicesBuffer<TIndices>(Vertex[] vertices, TIndices[] indices) where TIndices : unmanaged, IBinaryInteger<TIndices>
    {
        ulong _vertexSize = (ulong)(sizeof(Vertex) * vertices.Length);
        ulong _indicesSize = (ulong)(sizeof(TIndices) * indices.Length);
        ulong _bufferSize = _vertexSize + _indicesSize;

        BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.IndexBufferBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit | MemoryPropertyFlags.HostVisibleBit, ref primitiveBuffer, ref primitiveBufferMemory);

        void* data;
        CreateVulkan.vk.MapMemory(LogicalDevice.device, primitiveBufferMemory, 0, _bufferSize, 0, &data);
        byte* ptr = (byte*)data;
        vertices.CopyTo(new Span<Vertex>(ptr, vertices.Length));
        indicesOffset = (ulong)(sizeof(Vertex) * vertices.Length);
        ptr += indicesOffset;
        indices.CopyTo(new Span<TIndices>(ptr, indices.Length));
        CreateVulkan.vk.UnmapMemory(LogicalDevice.device, primitiveBufferMemory);
    }



    public ModelData<ushort> Get(PrimitiveUIModel model)
    {
        return uiDb[model];
    }

    public void Dispose()
    {
        BufferHelper.DestroyBuffer(primitiveBuffer, primitiveBufferMemory);
    }
}