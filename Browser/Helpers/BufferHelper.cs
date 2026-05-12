using Browser;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

static public class BufferHelper
{
    public static unsafe void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Buffer buffer, ref DeviceMemory bufferMemory)
    {
        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,

            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        if (BrowserWindow.vk.CreateBuffer(BrowserWindow.device, ref bufferInfo, null, out buffer) != Result.Success)
        {
            throw new Exception("Failed to create vertex buffer!");
        }

        BrowserWindow.vk.GetBufferMemoryRequirements(BrowserWindow.device, buffer, out var _memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = _memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(BrowserWindow.vk, BrowserWindow.physicalDevice, _memRequirements.MemoryTypeBits, properties)
        };

        if (BrowserWindow.vk.AllocateMemory(BrowserWindow.device, ref allocInfo, null, out bufferMemory) != Result.Success)
        {
            throw new Exception("Failed to allocate vertex buffer memory!");
        }

        BrowserWindow.vk.BindBufferMemory(BrowserWindow.device, buffer, bufferMemory, 0);
    }

    public static unsafe void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        CommandBuffer _commandBuffer = CmdHelper.BeginSingleTimeCommands();

        BufferCopy copyRegion = new()
        {
            SrcOffset = 0,
            DstOffset = 0,
            Size = size,
        };

        BrowserWindow.vk.CmdCopyBuffer(_commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);

        CmdHelper.EndSingleTimeCommands(_commandBuffer);
    }

    public static uint FindMemoryType(Vk vk, PhysicalDevice device, uint typeFilter, MemoryPropertyFlags properties)
    {
        vk.GetPhysicalDeviceMemoryProperties(device, out var memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }
        throw new Exception("Failed to find suitable memory type!");
    }

    public static unsafe void DestroyBuffer(Buffer buffer, DeviceMemory memory)
    {
        BrowserWindow.vk.DestroyBuffer(BrowserWindow.device, buffer, null);
        BrowserWindow.vk.FreeMemory(BrowserWindow.device, memory, null);
    }
}