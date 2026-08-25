using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Vulkan;

namespace GraphicsCore.Buffers;

public class InstancesBuffer : IDisposable
{
    public const int NODES_COUNT_PER_INCREASE = 100;
    uint incresedTimes = 1;
    uint lastInstanceId = 0;
    readonly BufferData[] buffers = new BufferData[VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    VisualElement[] instances = [];
    readonly List<uint> freeSlots = [];
    readonly Queue<VisualElement> addInstanceQueue = [];

    readonly int gpuDataSize;

    public InstancesBuffer()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            buffers[i] = CreateBuffer();
        }

        Type type = typeof(InstancesManager);

        MethodInfo openMethod = typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!;

        MethodInfo closedMethod = openMethod.MakeGenericMethod(type);

        gpuDataSize = (int)closedMethod.Invoke(null, null)!;
    }

    #region Buffer functions
    public unsafe BufferData CreateBuffer()
    {
        BufferData _bufferData = new();
        ulong _bufferSize = (ulong)gpuDataSize * NODES_COUNT_PER_INCREASE * incresedTimes;
        BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.ShaderDeviceAddressBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref _bufferData.Buffer, ref _bufferData.Memory);
        void* data;
        CreateVulkan.vk.MapMemory(LogicalDevice.device, _bufferData.Memory, 0, _bufferSize, 0, &data);
        _bufferData.Mapped = data;

        BufferDeviceAddressInfo addrInfo = new()
        {
            SType = StructureType.BufferDeviceAddressInfo,
            Buffer = _bufferData.Buffer
        };

        _bufferData.DeviceAddress = CreateVulkan.vk.GetBufferDeviceAddress(LogicalDevice.device, ref addrInfo);
        return _bufferData;
    }

    public unsafe void IncreseBuffer(int frameInFlight, uint newIncresedTimes)
    {
        ulong _bufferSize = (ulong)gpuDataSize * NODES_COUNT_PER_INCREASE * incresedTimes;
        incresedTimes = newIncresedTimes;
        BufferData _newBuffer = CreateBuffer();
        ulong _newBufferSize = (ulong)gpuDataSize * NODES_COUNT_PER_INCREASE * incresedTimes;

        new Span<byte>(buffers[frameInFlight].Mapped, (int)_bufferSize).CopyTo(new Span<byte>(_newBuffer.Mapped, (int)_newBufferSize));
        buffers[frameInFlight].Dispose();
        buffers[frameInFlight] = _newBuffer;
    }

    public BufferData GetBuffer(uint frameInFlight)
    {
        return buffers[frameInFlight];
    }

    uint GetNewSize(uint requiredSize)
    {
        uint i = 0;
        while (requiredSize > NODES_COUNT_PER_INCREASE * (incresedTimes + i))
        {
            i++;
        }
        return incresedTimes + i;
    }
    #endregion
    public void EnqueueInstance(VisualElement element)
    {
        addInstanceQueue.Enqueue(element);
    }

    public void RenderTick(uint frameInFlight)
    {
        if (addInstanceQueue.Count == 0) return;

        #region Test if it is required to create new buffer with larger size
        uint _newSize = GetNewSize(lastInstanceId + (uint)addInstanceQueue.Count);
        if (_newSize != incresedTimes)
        {
            IncreseBuffer((int)frameInFlight, _newSize);
            var _oldInstances = instances;
            instances = new VisualElement[_newSize];
            Array.Copy(_oldInstances, instances, _oldInstances.Length);
        }
        #endregion

        while (addInstanceQueue.TryDequeue(out var element))
        {
            AddInstance(element);
        }

        if (freeSlots.Count == 0) return;

        foreach (var freeSlot in freeSlots)
        {
            UpdateLastInstanceId();
            instances[freeSlot] = instances[lastInstanceId - 1];
            instances[freeSlot].ChangeInstanceId(freeSlot);
        }
        freeSlots.Clear();
    }

    void UpdateLastInstanceId()
    {
        if (!freeSlots.Contains(lastInstanceId - 1)) return;

        do
        {
            lastInstanceId--;
        } while (freeSlots.Contains(lastInstanceId - 1));
    }

    uint GetNewInstanceId()
    {
        if (freeSlots.Count != 0)
        {
            var _newId = freeSlots[0];
            freeSlots.RemoveAt(0);
            return _newId;
        }
        return lastInstanceId++;
    }

    void AddInstance(VisualElement element)
    {
        var _newInstanceId = GetNewInstanceId();
        element.ChangeInstanceId(_newInstanceId);
        instances[_newInstanceId] = element;
    }

    public void RemoveData(uint instanceId)
    {
        freeSlots.Add(instanceId);
    }

    public void Dispose()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            buffers[i].Dispose();
        }
    }
}
