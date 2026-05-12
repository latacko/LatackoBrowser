using Browser;
using Silk.NET.Vulkan;

public static class CmdHelper
{
    internal static unsafe CommandBuffer BeginSingleTimeCommands()
    {
        CommandBufferAllocateInfo _allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = BrowserWindow.commandPool,
            CommandBufferCount = 1,
        };

        BrowserWindow.vk.AllocateCommandBuffers(BrowserWindow.device, &_allocInfo, out var _commandBuffer);

        CommandBufferBeginInfo _beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        BrowserWindow.vk.BeginCommandBuffer(_commandBuffer, &_beginInfo);

        return _commandBuffer;
    }

    internal static unsafe void EndSingleTimeCommands(CommandBuffer commandBuffer)
    {
        BrowserWindow.vk.EndCommandBuffer(commandBuffer);

        SubmitInfo _submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        BrowserWindow.vk.QueueSubmit(BrowserWindow.graphicsQueue, 1, &_submitInfo, default);
        BrowserWindow.vk.QueueWaitIdle(BrowserWindow.graphicsQueue);

        BrowserWindow.vk.FreeCommandBuffers(BrowserWindow.device, BrowserWindow.commandPool, 1, &commandBuffer);
    }
}