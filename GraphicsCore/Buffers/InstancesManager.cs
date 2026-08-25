using System.Reflection;
using AssetCore;
using VulkanManager.BufferManager;
namespace GraphicsCore.Buffers;

public unsafe class InstancesManager: IDisposable
{
    public static InstancesManager? Instance;
    public const int MAX_OBJECTS = 1000;
    bool disposed;

    public Dictionary<ulong, InstancesBuffer> nodesBuffers = [];
    public static ulong MakeKey(uint siteId, uint modelId) => ((ulong)siteId << 32) | modelId;
    readonly Type gpuDataType;
    public InstancesManager(Type gpuDataType)
    {
        Instance = this;
        this.gpuDataType = gpuDataType;
    }

    public void RegisterBuffers(uint siteId)
    {
        MethodInfo openMethod = typeof(AssetManager).GetMethod(nameof(AssetManager.GetIdsOfModelType))!;

        MethodInfo closedMethod = openMethod.MakeGenericMethod(gpuDataType);

        uint[] _idsOfModels = (uint[])closedMethod.Invoke(null, null)!;

        foreach (var modelId in _idsOfModels)
        {
            nodesBuffers[MakeKey(siteId, modelId)] = new();
        }
    }

    public Span<byte> GetDestinationSpan(ulong siteModelKey, uint currentFrame, uint objectIndex, int structSize)
    {
        byte* basePtr = (byte*)nodesBuffers[siteModelKey].GetBuffer(currentFrame).Mapped;
        return new Span<byte>(basePtr + objectIndex * structSize, structSize);
    }

    public void RenderTick(ulong siteModelKey, uint frameInFlight)
    {
        nodesBuffers[siteModelKey].RenderTick(frameInFlight);
    }

    public ulong GetBufferDeviceAddress(ulong siteModelKey, uint currentFrame) => nodesBuffers[siteModelKey].GetBuffer(currentFrame).DeviceAddress;

    public void Dispose()
    {
        if (disposed) return;

        foreach (var (_, modelBuffers) in nodesBuffers)
        {
            modelBuffers.Dispose();
        }
    }
}