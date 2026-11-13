using Silk.NET.Vulkan;
namespace Vulkan;

public static class CmdHelper
{
    public static unsafe CommandBuffer BeginSingleTimeCommands(CommandPool commandPool)
    {
        CommandBufferAllocateInfo _allocInfo = new(
            level: CommandBufferLevel.Primary,
            commandPool: commandPool,
            commandBufferCount: 1
        );

        CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, &_allocInfo, out var _commandBuffer);

        CommandBufferBeginInfo _beginInfo = new(
            flags: CommandBufferUsageFlags.OneTimeSubmitBit
        );

        CreateVulkan.vk.BeginCommandBuffer(_commandBuffer, &_beginInfo);

        return _commandBuffer;
    }

    public static unsafe void EndSingleTimeCommandsIdle(CommandPool commandPool, CommandBuffer commandBuffer)
    {
        CreateVulkan.vk.EndCommandBuffer(commandBuffer);

        SubmitInfo _submitInfo = new(
            commandBufferCount: 1,
            pCommandBuffers: &commandBuffer
        );

        CreateVulkan.vk.QueueSubmit(LogicalDevice.GraphicsQueue, 1, &_submitInfo, default);
        CreateVulkan.vk.QueueWaitIdle(LogicalDevice.GraphicsQueue);

        CreateVulkan.vk.FreeCommandBuffers(LogicalDevice.device, commandPool, 1, &commandBuffer);
    }

    public static unsafe void EndSingleTimeCommands(CommandBuffer commandBuffer, Fence fence)
    {
        CreateVulkan.vk.EndCommandBuffer(commandBuffer);

        SubmitInfo _submitInfo = new(
            commandBufferCount: 1,
            pCommandBuffers: &commandBuffer
        );

        CreateVulkan.vk.QueueSubmit(LogicalDevice.GraphicsQueue, 1, &_submitInfo, fence);
    }
}