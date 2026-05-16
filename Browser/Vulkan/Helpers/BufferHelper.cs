using Browser;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
namespace Vulkan;

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

        if (CreateVulkan.vk.CreateBuffer(LogicalDevice.device, ref bufferInfo, null, out buffer) != Result.Success)
        {
            throw new Exception("Failed to create vertex buffer!");
        }

        CreateVulkan.vk.GetBufferMemoryRequirements(LogicalDevice.device, buffer, out var _memRequirements);

        MemoryAllocateFlagsInfo allocFlags = new()
        {
            SType = StructureType.MemoryAllocateFlagsInfo,
            Flags = MemoryAllocateFlags.DeviceAddressBit
        };

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = _memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(CreateVulkan.vk, PhysicalDevice.physicalDevice, _memRequirements.MemoryTypeBits, properties),
            PNext = (usage & BufferUsageFlags.ShaderDeviceAddressBit) != 0 ? &allocFlags : null,
        };

        if (CreateVulkan.vk.AllocateMemory(LogicalDevice.device, ref allocInfo, null, out bufferMemory) != Result.Success)
        {
            throw new Exception("Failed to allocate vertex buffer memory!");
        }

        CreateVulkan.vk.BindBufferMemory(LogicalDevice.device, buffer, bufferMemory, 0);
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

        CreateVulkan.vk.CmdCopyBuffer(_commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);

        CmdHelper.EndSingleTimeCommandsIdle(_commandBuffer);
    }

    public static uint FindMemoryType(Vk vk, Silk.NET.Vulkan.PhysicalDevice device, uint typeFilter, MemoryPropertyFlags properties)
    {
        vk.GetPhysicalDeviceMemoryProperties2(device, out var memProperties);

        for (int i = 0; i < memProperties.MemoryProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << i)) != 0 && (memProperties.MemoryProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }
        throw new Exception("Failed to find suitable memory type!");
    }

    public static unsafe void DestroyBuffer(Buffer buffer, DeviceMemory memory)
    {
        CreateVulkan.vk.DestroyBuffer(LogicalDevice.device, buffer, null);
        CreateVulkan.vk.FreeMemory(LogicalDevice.device, memory, null);
    }
}