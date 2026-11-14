using System;
using Silk.NET.Vulkan;

namespace VulkanManager;

public interface ITransfer
{
    public CommandBuffer? GetCommandBuffer(uint commandBuffer);
    public void SetNextCommandBuffer(uint nextCommandBuffer);
    public void BeginRecording();
    public void EndRecording();
}
