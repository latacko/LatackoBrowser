using System.Reflection;
using AssetCore;
using VulkanManager;
using VulkanManager.BufferManager;
namespace GraphicsCore.Buffers;

public unsafe class InstancesManager : IDisposable
{
    public Dictionary<ulong, InstancesBuffer<VisualElement>> nodesBuffers = [];
    public static ulong MakeKey(uint siteId, uint meshId) => ((ulong)siteId << 32) | meshId;
    readonly Type meshData;
    readonly Type instanceData;
    public InstancesManager(Type meshData, Type instanceData)
    {
        this.meshData = meshData;
        this.instanceData = instanceData;
        if (meshData.BaseType != typeof(IMeshData))
        {
            throw new ArgumentException("Gpu data must be IMeshData. But it is " + meshData + " base type: " + meshData.BaseType);
        }
    }

    public void RegisterBufferForMesh(uint siteId, uint meshId)
    {
        if (meshData.BaseType == null || !meshData.BaseType.Equals(AssetManager.GetModel(meshId).GetType()))
        {
            throw new Exception("Mesh " + meshId + " is not type of " + meshData);
        }
        nodesBuffers[MakeKey(siteId, meshId)] = new(instanceData);
    }

    public void UnregisterBufferForMesh(uint siteId, uint meshId)
    {
        nodesBuffers[MakeKey(siteId, meshId)].Dispose();
    }

    public void RenderTick(ulong siteModelKey, uint frameInFlight)
    {
        nodesBuffers[siteModelKey].RenderTick(frameInFlight);
    }

    public Span<byte> GetDestinationSpan(ulong siteModelKey, uint frameInFlight, uint objectIndex, int structSize)
    {
        byte* basePtr = (byte*)nodesBuffers[siteModelKey].GetBuffer(frameInFlight).Mapped;
        return new Span<byte>(basePtr + objectIndex * structSize, structSize);
    }

    public ulong GetBufferDeviceAddress(ulong siteModelKey, uint frameInFlight) => nodesBuffers[siteModelKey].GetBuffer(frameInFlight).DeviceAddress;

    public void Dispose()
    {
        foreach (var (_, modelBuffers) in nodesBuffers)
        {
            modelBuffers.Dispose();
        }
    }
}