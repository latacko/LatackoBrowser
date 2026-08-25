using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
namespace Vulkan;

public unsafe struct BufferData: IDisposable
{
    public Buffer Buffer;
    public DeviceMemory Memory; // replaces VMA allocation

    public void* Mapped; // CPU pointer (optional)

    public ulong DeviceAddress; // VkDeviceAddress

    public void Dispose()
    {
        CreateVulkan.vk.UnmapMemory(LogicalDevice.device, Memory);
        BufferHelper.DestroyBuffer(Buffer, Memory);
    }
};