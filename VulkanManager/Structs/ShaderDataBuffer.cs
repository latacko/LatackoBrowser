using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
namespace Vulkan;

unsafe struct ShaderDataBuffer
{
    public Buffer Buffer;
    public DeviceMemory Memory; // replaces VMA allocation

    public void* Mapped; // CPU pointer (optional)

    public ulong DeviceAddress; // VkDeviceAddress
};