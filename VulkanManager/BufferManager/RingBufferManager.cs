using System;
using InstanceFinderCore;
using Silk.NET.Vulkan;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanManager.BufferManager;

public class RingBufferManager
{
    CommandBuffer[] commandBuffers = new CommandBuffer[VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    TransferCommandPoolManager transferCommandPoolManager;
    bool sthWasAdded = false;

    public void SetTransferCommandPoolManager(TransferCommandPoolManager transferCommandPoolManager)
    {
        this.transferCommandPoolManager = transferCommandPoolManager;
        CreateCommandBuffers();
    }

    unsafe void CreateCommandBuffers()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CommandBufferAllocateInfo commandBufferCI = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = transferCommandPoolManager.CommandPools[i],
                CommandBufferCount = 1
            };

            fixed (CommandBuffer* commandBufferPtr = &commandBuffers[i])
                CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, ref commandBufferCI, commandBufferPtr);
        }
    }

    public unsafe void BeginRecording(uint frameInFlight)
    {
        sthWasAdded = false;
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

    public bool EndRecording(uint frameInFlight)
    {
        CreateVulkan.vk.EndCommandBuffer(commandBuffers[frameInFlight]);
        return sthWasAdded;
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

    public CommandBuffer GetCommandBuffer(uint frameInFlight) => commandBuffers[frameInFlight];
}
