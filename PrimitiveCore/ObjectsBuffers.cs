using System.Runtime.CompilerServices;
using AssetCore;
using PrimitiveCore.Buffer;
using PrimitiveCore.Model;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Vulkan;
using VulkanManager.BufferManager;
using Buffer = Silk.NET.Vulkan.Buffer;
namespace PrimitiveCore;

public unsafe class NodesManager
{
    public static NodesManager Instance;
    public const int MAX_OBJECTS = 1000;
    public Dictionary<ulong, NodesBuffer> shaderDataBuffersForObjects = [];
    public static ulong MakeKey(uint siteId, uint modelId) => ((ulong)siteId << 32) | modelId;
    public NodesManager()
    {
        Instance = this;
    }

    public void RegisterBuffers(uint siteId)
    {
        uint[] _idsOfModels = AssetManager.GetIdsOfModelType<MeshData<ushort>>();

        foreach (var modelId in _idsOfModels)
        {
            shaderDataBuffersForObjects[MakeKey(siteId, modelId)] = new();
        }
    }

    public Span<byte> GetDestinationSpan(ulong siteModelKey, uint currentFrame, uint objectIndex, int structSize)
    {
        byte* basePtr = (byte*)shaderDataBuffersForObjects[siteModelKey].GetBuffer(currentFrame).Mapped;
        return new Span<byte>(basePtr + objectIndex * structSize, structSize);
    }

    public void Dispose()
    {
        foreach (var (_, modelBuffers) in shaderDataBuffersForObjects)
        {
            modelBuffers.Dispose();
        }
    }
}