using System.Reflection;
using AssetCore;
using VulkanManager;
using VulkanManager.BufferManager;
namespace GraphicsCore.Buffers;

public unsafe class InstancesManager: IDisposable
{
    public static InstancesManager? Instance;
    bool disposed;

    public Dictionary<ulong, InstancesBuffer> nodesBuffers = [];
    public static ulong MakeKey(uint siteId, uint modelId) => ((ulong)siteId << 32) | modelId;
    readonly Type meshData;
    readonly Type instanceData;
    public InstancesManager(Type meshData, Type instanceData)
    {
        Instance = this;
        this.meshData = meshData;
        this.instanceData = instanceData;
        if (meshData.GetType().BaseType != typeof(IMeshData))
        {
            throw new ArgumentException("Gpu data must by IMeshData");
        }
    }

    public void RegisterBuffers(uint siteId)
    {
        MethodInfo openMethod = typeof(AssetManager).GetMethod(nameof(AssetManager.GetIdsOfModelType))!;

        MethodInfo closedMethod = openMethod.MakeGenericMethod(instanceData);

        uint[] _idsOfModels = (uint[])closedMethod.Invoke(null, null)!;

        foreach (var modelId in _idsOfModels)
        {
            nodesBuffers[MakeKey(siteId, modelId)] = new(instanceData);
        }
    }

    public Span<byte> GetDestinationSpan(ulong siteModelKey, uint frameInFlight, uint objectIndex, int structSize)
    {
        byte* basePtr = (byte*)nodesBuffers[siteModelKey].GetBuffer(frameInFlight).Mapped;
        return new Span<byte>(basePtr + objectIndex * structSize, structSize);
    }

    public void RenderTick(ulong siteModelKey, uint frameInFlight)
    {
        nodesBuffers[siteModelKey].RenderTick(frameInFlight);
    }

    public ulong GetBufferDeviceAddress(ulong siteModelKey, uint frameInFlight) => nodesBuffers[siteModelKey].GetBuffer(frameInFlight).DeviceAddress;

    public void Dispose()
    {
        if (disposed) return;

        foreach (var (_, modelBuffers) in nodesBuffers)
        {
            modelBuffers.Dispose();
        }
    }
}