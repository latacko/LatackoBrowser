using System;
using Silk.NET.Vulkan;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanManager.BufferManager;

public unsafe class BufferInfo<T> : IDisposable where T : unmanaged
{
    public Buffer Buffer;
    public DeviceMemory Memory; // replaces VMA allocation

    uint maxElements;

    BufferUsageFlags usage;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="maxElements">Max count of elements of type T</param>
    /// <param name="usage"></param>
    public BufferInfo(uint maxElements, BufferUsageFlags usage)
    {
        this.maxElements = maxElements;
        this.usage = usage;
        CreateBuffer(ref Buffer, ref Memory);
    }

    void CreateBuffer(ref Buffer buffer, ref DeviceMemory memory)
    {
        ulong _bufferSize = (ulong)sizeof(T) * maxElements;
        BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.ShaderDeviceAddressBit | usage, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref buffer, ref memory);
    }

    public void Dispose()
    {
        CreateVulkan.vk.UnmapMemory(LogicalDevice.device, Memory);
        BufferHelper.DestroyBuffer(Buffer, Memory);
    }
}
