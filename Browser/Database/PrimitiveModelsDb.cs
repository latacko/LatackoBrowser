using System.Numerics;
using Browser;
using Browser.DataTypes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public class PrimitiveModelsDb : IDisposable
{
    Buffer primitiveVertexBuffer;
    DeviceMemory primitiveVertexBufferMemory;

    Buffer primitiveIndicesBuffer;
    DeviceMemory primitiveIndicesBufferMemory;

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
        ulong lastVericesOffset = 0;
        ulong lastIndicesOffset = 0;

        int verticesIndex = 0;
        int indicesIndex = 0;

        foreach (var primitiveModelInfo in uiDb)
        {
            Array.Copy(primitiveModelInfo.Value.Vertices, 0, vertices, verticesIndex, primitiveModelInfo.Value.Vertices.Length);
            Array.Copy(primitiveModelInfo.Value.Indices, 0, indices, indicesIndex, primitiveModelInfo.Value.Indices.Length);

            primitiveModelInfo.Value.vertexOffset = lastVericesOffset;
            primitiveModelInfo.Value.indexOffset = lastIndicesOffset;

            lastVericesOffset += (ulong)(sizeof(Vertex) * primitiveModelInfo.Value.Vertices.Length);
            lastIndicesOffset += (ulong)(sizeof(ushort) * primitiveModelInfo.Value.Indices.Length);

            verticesIndex += primitiveModelInfo.Value.Vertices.Length;
            indicesIndex += primitiveModelInfo.Value.Indices.Length;
        }

        CreateVertexBuffer(vertices, ref primitiveVertexBuffer, ref primitiveVertexBufferMemory);
        CreateIndexBuffer(indices, ref primitiveIndicesBuffer, ref primitiveIndicesBufferMemory);

        foreach (var primitiveModelInfo in uiDb)
        {
            primitiveModelInfo.Value.vertexBuffer = primitiveVertexBuffer;
            primitiveModelInfo.Value.indexBuffer = primitiveIndicesBuffer;
        }


        Console.WriteLine("Created vertices buffer " + primitiveVertexBuffer);
        Console.WriteLine("Created Indices buffer " + primitiveIndicesBuffer);
    }

    public unsafe void CreateVertexBuffer(Vertex[] vertices, ref Buffer vertexBuffer, ref DeviceMemory vertexBufferMemory)
    {
        var _bufferSize = (ulong)(sizeof(Vertex) * vertices.Length);
        Buffer _stagingBuffer = new();
        DeviceMemory _stagingBufferMemory = new();
        BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref _stagingBuffer, ref _stagingBufferMemory);

        void* data;
        BrowserWindow.vk.MapMemory(BrowserWindow.device, _stagingBufferMemory, 0, _bufferSize, 0, &data);
        vertices.CopyTo(new Span<Vertex>(data, vertices.Length));
        BrowserWindow.vk.UnmapMemory(BrowserWindow.device, _stagingBufferMemory);

        BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref vertexBuffer, ref vertexBufferMemory);

        BufferHelper.CopyBuffer(_stagingBuffer, vertexBuffer, _bufferSize);

        BrowserWindow.vk.DestroyBuffer(BrowserWindow.device, _stagingBuffer, null);
        BrowserWindow.vk.FreeMemory(BrowserWindow.device, _stagingBufferMemory, null);
    }

    internal unsafe void CreateIndexBuffer<TIndex>(TIndex[] indices, ref Buffer indexBuffer, ref DeviceMemory indexBufferMemory) where TIndex : unmanaged, IBinaryInteger<TIndex>
    {
        ulong _bufferSize = (ulong)(sizeof(TIndex) * indices.Length);
        Buffer _stagingBuffer = new();
        DeviceMemory _stagingBufferMemory = new();

        BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref _stagingBuffer, ref _stagingBufferMemory);

        void* data;
        BrowserWindow.vk.MapMemory(BrowserWindow.device, _stagingBufferMemory, 0, _bufferSize, 0, &data);
        indices.CopyTo(new Span<TIndex>(data, indices.Length));
        BrowserWindow.vk.UnmapMemory(BrowserWindow.device, _stagingBufferMemory);

        BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref indexBuffer, ref indexBufferMemory);
        BufferHelper.CopyBuffer(_stagingBuffer, indexBuffer, _bufferSize);

        BrowserWindow.vk.DestroyBuffer(BrowserWindow.device, _stagingBuffer, null);
        BrowserWindow.vk.FreeMemory(BrowserWindow.device, _stagingBufferMemory, null);
    }

    public ModelData<ushort> Get(PrimitiveUIModel model)
    {
        return uiDb[model];
    }

    public void Dispose()
    {
        BufferHelper.DestroyBuffer(primitiveVertexBuffer, primitiveVertexBufferMemory);
        BufferHelper.DestroyBuffer(primitiveIndicesBuffer, primitiveIndicesBufferMemory);
    }
}