using System.Reflection;
using AssetCore;
using GraphicsCore;
using GraphicsCore.Buffers;
using TextCore.Text;
using VulkanManager;
using VulkanManager.BufferManager;

namespace TextCore.Buffer;

public unsafe class TextInstancesManager : IDisposable
{
    static readonly Dictionary<Type, TextInstancesManager> textInstancesManagers = [];
    readonly Dictionary<uint, InstancesBuffer<VisualElement>> textContainerInstancesBuffer = [];
    readonly Dictionary<uint, InstancesBuffer<VisualElement>> textLineInstancesBuffer = [];
    Type instanceData;
    internal TextInstancesManager(Type instanceData)
    {
        this.instanceData = instanceData;
        textInstancesManagers[instanceData] = this;
    }

    public static TextInstancesManager Get<InstanceData>() where InstanceData : struct
    {
        var _instanceData = typeof(InstanceData);
        if (textInstancesManagers.TryGetValue(_instanceData, out TextInstancesManager? value))
            return value;

        textInstancesManagers[_instanceData] = new TextInstancesManager(_instanceData);
        return textInstancesManagers[_instanceData];
    }

    public void RegisterBufferForSite(uint siteId)
    {
        textContainerInstancesBuffer.Add(siteId, new(instanceData));
        textLineInstancesBuffer.Add(siteId, new(instanceData));
    }

    public void RenderTick(uint siteid, uint frameInFlight)
    {
        textContainerInstancesBuffer[siteid].RenderTick(frameInFlight);
        textLineInstancesBuffer[siteid].RenderTick(frameInFlight);
    }

    public Span<byte> GetTextContainerDestinationSpan(uint siteId, uint frameInFlight, uint objectIndex, int structSize)
    {
        byte* basePtr = (byte*)textContainerInstancesBuffer[siteId].GetBuffer(frameInFlight).Mapped;
        return new Span<byte>(basePtr + objectIndex * structSize, structSize);
    }

    public Span<byte> GetTextLineDestinationSpan(uint siteId, uint frameInFlight, uint objectIndex, int structSize)
    {
        byte* basePtr = (byte*)textLineInstancesBuffer[siteId].GetBuffer(frameInFlight).Mapped;
        return new Span<byte>(basePtr + objectIndex * structSize, structSize);
    }

    public ulong GetTextContainerBufferDeviceAddress(uint siteId, uint frameInFlight) => textContainerInstancesBuffer[siteId].GetBuffer(frameInFlight).DeviceAddress;
    public ulong GetTextLineBufferDeviceAddress(uint siteId, uint frameInFlight) => textLineInstancesBuffer[siteId].GetBuffer(frameInFlight).DeviceAddress;

    public void Dispose()
    {
        foreach (var (_, instanceBuffer) in textContainerInstancesBuffer)
        {
            instanceBuffer.Dispose();
        }
        foreach (var (_, instanceBuffer) in textLineInstancesBuffer)
        {
            instanceBuffer.Dispose();
        }
    }
}