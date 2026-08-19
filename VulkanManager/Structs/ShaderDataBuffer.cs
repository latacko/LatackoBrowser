using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
namespace Vulkan;

public unsafe struct BufferData
{
    public Buffer Buffer;
    public DeviceMemory Memory; // replaces VMA allocation

    public void* Mapped; // CPU pointer (optional)

    public ulong DeviceAddress; // VkDeviceAddress
};