using System;
using Silk.NET.Vulkan;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanManager.BufferManager;

public class RingBufferManager
{
    CommandBuffer[] commandBuffers;

    public unsafe void BeginRecording(uint frameInFlight)
    {
        CommandBufferBeginInfo _beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,

            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
            PInheritanceInfo = null,
        };

        if (CreateVulkan.vk.BeginCommandBuffer(commandBuffers[frameInFlight], &_beginInfo) != Result.Success)
        {
            throw new Exception("Failed to begin recording command buffer!");
        }
    }

    public void EndRecording(uint frameInFlight)
    {
        CreateVulkan.vk.EndCommandBuffer(commandBuffers[frameInFlight]);
    }

    public unsafe void AddTransfer(uint frameInFlight, Buffer srcBuffer, Buffer dstBuffer, BufferCopy2[] copyInfo)
    {
        fixed (BufferCopy2* copyListAddress = copyInfo)
        {
            CopyBufferInfo2 _cbI = new()
            {
                SrcBuffer = srcBuffer,
                PRegions = copyListAddress,
                RegionCount = (uint)copyInfo.Length,
                DstBuffer = dstBuffer,
            };
            CreateVulkan.vk.CmdCopyBuffer2(commandBuffers[frameInFlight], ref _cbI);
        }
    }
}
