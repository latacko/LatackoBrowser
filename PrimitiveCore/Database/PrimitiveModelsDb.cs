using System.Numerics;
using AssetCore;
using GraphicsCore;
using InstanceFinderCore;
using PrimitiveCore.Model;
using Silk.NET.Vulkan;
using Units;
using Vulkan;
using VulkanManager;

namespace Vulkan;

[InstanceFinderCore.Instance]
public class PrimitiveModelsDb : IDisposable, ITransfer, IInstanceInit
{
    CommandBuffer[] transferCommandBuffers;
    uint currentTransferCommandBuffer;
    TransferCommandPoolManager transferCommandPoolManager;
    internal static Silk.NET.Vulkan.Buffer primitiveBuffer;
    internal static DeviceMemory primitiveBufferMemory;
    internal static ulong indicesOffset;

    bool sthWasAdded = false;

    readonly Dictionary<PrimitiveUIModel, MeshData<ushort>> uiDb = new()
    {
        [PrimitiveUIModel.Quad] = new MeshData<ushort>(
            [
                new MeshVertex(new(0,0,0), new(0,0)),
                new MeshVertex(new(0,1,0), new(0,1)),
                new MeshVertex(new(1,1,0), new(1,1)),
                new MeshVertex(new(1,0,0), new(1,0)),
            ],
            [0, 1, 2, 2, 3, 0])
    };

    public void Init()
    {
        AssetManager.RegisterMeshDataBufferManager<MeshData<ushort>>(MeshGpuRegister);
        this.transferCommandPoolManager = InstanceRegistry.Get<TransferCommandPoolManager>();
        transferCommandBuffers = transferCommandPoolManager.CreateManagerCommandBuffers();
        CreateBuffers();
    }

    public MeshData<ushort> Get(PrimitiveUIModel model)
    {
        return uiDb[model];
    }


    /// <summary>
    /// Creates two buffers for primitives models. Vertex and Indices buffer
    /// </summary>
    void CreateBuffers()
    {
        foreach (var primitiveModelInfo in uiDb)
            AssetCore.AssetManager.RegisterModel(primitiveModelInfo.Value);

        int _vertexCount = 0;
        int _indexCount = 0;
        foreach (var primitiveModelInfo in uiDb)
        {
            _vertexCount += primitiveModelInfo.Value.Vertices.Length;
            _indexCount += primitiveModelInfo.Value.Indices.Length;
        }

        MeshVertex[] vertices = new MeshVertex[_vertexCount];
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


        // Console.WriteLine("Created vertices buffer " + primitiveBuffer);
    }

    unsafe void CreateVertexAndIndicesBuffer<TIndices>(MeshVertex[] vertices, TIndices[] indices) where TIndices : unmanaged, IBinaryInteger<TIndices>
    {
        uint _vertexSize = (uint)(sizeof(MeshVertex) * vertices.Length);
        uint _indicesSize = (uint)(sizeof(TIndices) * indices.Length);
        uint _bufferSize = _vertexSize + _indicesSize;

        (BufferData bufferData, ulong size) = BufferHelper.CreateStagingBuffer<byte>(_bufferSize);
        BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.IndexBufferBit | BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit, ref primitiveBuffer, ref primitiveBufferMemory);

        vertices.CopyTo(new Span<MeshVertex>(bufferData.Mapped, vertices.Length));
        indicesOffset = (ulong)(sizeof(MeshVertex) * vertices.Length);
        indices.CopyTo(new Span<TIndices>(((byte*)bufferData.Mapped) + indicesOffset, indices.Length));

        BufferCopy2 _bc = new()
        {
            SType = StructureType.BufferCopy2,
            SrcOffset = 0,
            DstOffset = 0,
            Size = _bufferSize,
        };

        CopyBufferInfo2 _cbI = new()
        {
            SType = StructureType.CopyBufferInfo2,
            SrcBuffer = bufferData.Buffer,
            PRegions = &_bc,
            RegionCount = 1,
            DstBuffer = primitiveBuffer,
        };

        Console.WriteLine(currentTransferCommandBuffer);
        Console.WriteLine(transferCommandBuffers);
        Console.WriteLine(transferCommandBuffers[currentTransferCommandBuffer]);

        CreateVulkan.vk.CmdCopyBuffer2(transferCommandBuffers[currentTransferCommandBuffer], ref _cbI);

        transferCommandPoolManager.AddStagingBuffer(currentTransferCommandBuffer, bufferData);
        sthWasAdded = true;
    }

    public CommandBuffer? GetCommandBuffer(uint frameInFlight)
    {
        if (!sthWasAdded)
            return null;
        return transferCommandBuffers[frameInFlight];
    }
    public void SetNextCommandBuffer(uint nextFrameInFlight) => currentTransferCommandBuffer = nextFrameInFlight;

    void MeshGpuRegister(IMeshData meshData)
    {

    }

    public void Dispose()
    {
        BufferHelper.DestroyBuffer(primitiveBuffer, primitiveBufferMemory);
    }
}