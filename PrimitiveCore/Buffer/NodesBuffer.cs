using System;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using Vulkan;

namespace PrimitiveCore.Buffer;

//TODO - Add request model then add it to queue and write data in render loop so it woudnt fight over buffer with prev renderer. A
public class NodesBuffer : IDisposable
{
    public const int NODES_COUNT_PER_INCREASE = 100;
    uint incresedTimes = 1;
    readonly BufferData[] buffers = new BufferData[VulkanEngine.MAX_FRAMES_IN_FLIGHT];

    public NodesBuffer()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            buffers[i] = CreateBuffer();
        }
    }

    public unsafe BufferData CreateBuffer()
    {
        BufferData _bufferData = new();
        ulong _bufferSize = (ulong)Unsafe.SizeOf<ModelGPUData>() * NODES_COUNT_PER_INCREASE * incresedTimes;
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

    public unsafe void IncreseBuffer(int frameInFlight)
    {
        ulong _bufferSize = (ulong)Unsafe.SizeOf<ModelGPUData>() * NODES_COUNT_PER_INCREASE * incresedTimes;
        incresedTimes++;
        BufferData _newBuffer = CreateBuffer();
        ulong _newBufferSize = (ulong)Unsafe.SizeOf<ModelGPUData>() * NODES_COUNT_PER_INCREASE * incresedTimes;

        new Span<byte>(buffers[frameInFlight].Mapped, (int)_bufferSize).CopyTo(new Span<byte>(_newBuffer.Mapped, (int)_newBufferSize));
        DisposeBuffer(buffers[frameInFlight]);
        buffers[frameInFlight] = _newBuffer;
    }

    public BufferData GetBuffer(uint frameInFlight)
    {
        return buffers[frameInFlight];
    }

    static void DisposeBuffer(BufferData bufferData)
    {
        CreateVulkan.vk.UnmapMemory(LogicalDevice.device, bufferData.Memory);
        BufferHelper.DestroyBuffer(bufferData.Buffer, bufferData.Memory);
    }

    public void Dispose()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            DisposeBuffer(buffers[i]);
        }
    }
}
