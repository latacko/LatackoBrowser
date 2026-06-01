using System;
using Silk.NET.Vulkan;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanManager.BufferManager;

public unsafe class BufferInfo<T> : IDisposable where T : unmanaged
{
    public Buffer Buffer;
    public DeviceMemory Memory; // replaces VMA allocation

    public nint Mapped; // CPU pointer (optional)

    public ulong DeviceAddress; // VkDeviceAddress
    uint size;

    BufferUsageFlags usage;

    public BufferInfo(uint initSize, BufferUsageFlags usage)
    {
        size = initSize;
        this.usage = usage;
        CreateBuffer(ref Buffer, ref Memory, ref Mapped, ref DeviceAddress);
    }

    void CreateBuffer(ref Buffer buffer, ref DeviceMemory memory, ref nint mapped, ref ulong deviceAddress)
    {
        ulong _bufferSize = (ulong)sizeof(T) * size;
        BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.ShaderDeviceAddressBit | usage, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref buffer, ref memory);
        nint data;
        CreateVulkan.vk.MapMemory(LogicalDevice.device, memory, 0, _bufferSize, 0, (void**)&data);
        mapped = data;

        BufferDeviceAddressInfo addrInfo = new()
        {
            SType = StructureType.BufferDeviceAddressInfo,
            Buffer = buffer
        };

        deviceAddress = CreateVulkan.vk.GetBufferDeviceAddress(LogicalDevice.device, ref addrInfo);
    }

    internal void DoubleBuffer()
    {
        uint oldSize = size;
        size *= 2;

        Buffer _buffer = new();
        DeviceMemory _memory = new();

        nint _mapped = 0;
        ulong _deviceAddress = 0;

        CreateBuffer(ref _buffer, ref _memory, ref _mapped, ref _deviceAddress);

        new Span<T>((void*)Mapped, (int)oldSize).CopyTo(
            new Span<T>((void*)_mapped, (int)size)
        );
        
        Dispose();

        Buffer = _buffer;
        Memory = _memory;

        Mapped = _mapped;
        DeviceAddress = _deviceAddress;
    }

    public void Dispose()
    {
        CreateVulkan.vk.UnmapMemory(LogicalDevice.device, Memory);
        BufferHelper.DestroyBuffer(Buffer, Memory);
    }
}
