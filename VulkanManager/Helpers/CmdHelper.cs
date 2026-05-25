using Silk.NET.Vulkan;
namespace Vulkan;

public static class CmdHelper
{
    internal static unsafe CommandBuffer BeginSingleTimeCommands()
    {
        CommandBufferAllocateInfo _allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = VulkanEngine.commandPool,
            CommandBufferCount = 1,
        };

        CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, &_allocInfo, out var _commandBuffer);

        CommandBufferBeginInfo _beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        CreateVulkan.vk.BeginCommandBuffer(_commandBuffer, &_beginInfo);

        return _commandBuffer;
    }

    internal static unsafe void EndSingleTimeCommandsIdle(CommandBuffer commandBuffer)
    {
        CreateVulkan.vk.EndCommandBuffer(commandBuffer);

        SubmitInfo _submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        CreateVulkan.vk.QueueSubmit(LogicalDevice.graphicsQueue, 1, &_submitInfo, default);
        CreateVulkan.vk.QueueWaitIdle(LogicalDevice.graphicsQueue);

        CreateVulkan.vk.FreeCommandBuffers(LogicalDevice.device, VulkanEngine.commandPool, 1, &commandBuffer);
    }

    public static unsafe void EndSingleTimeCommands(CommandBuffer commandBuffer, Fence fence)
    {
        CreateVulkan.vk.EndCommandBuffer(commandBuffer);

        SubmitInfo _submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        CreateVulkan.vk.QueueSubmit(LogicalDevice.graphicsQueue, 1, &_submitInfo, fence);
    }
}