using System;
using InstanceFinderCore;
using Silk.NET.Vulkan;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanManager.BufferManager;

public class RingBufferManager: ITransfer, IInstanceInit
{
    CommandBuffer[] transferCommandBuffers;
    uint currentTransferCommandBuffer;
    TransferCommandPoolManager transferCommandPoolManager;
    bool sthWasAdded = false;

    public void Init()
    {
        this.transferCommandPoolManager = InstanceFinder.GetInstance<TransferCommandPoolManager>();
        transferCommandBuffers = transferCommandPoolManager.CreateManagerCommandBuffers();
        Console.WriteLine("Init ring buffer");
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

        if (CreateVulkan.vk.BeginCommandBuffer(transferCommandBuffers[currentTransferCommandBuffer], &_beginInfo) != Result.Success)
        {
            throw new Exception("Failed to begin recording command buffer!");
        }
    }

    public void EndRecording(uint frameInFlight)
    {
        CreateVulkan.vk.EndCommandBuffer(transferCommandBuffers[currentTransferCommandBuffer]);
    }

    public unsafe void AddTransfer(uint frameInFlight, BufferData srcBuffer, Buffer dstBuffer, BufferCopy2[] copyInfo)
    {
        fixed (BufferCopy2* copyListAddress = copyInfo)
        {
            CopyBufferInfo2 _cbI = new()
            {
                SrcBuffer = srcBuffer.Buffer,
                PRegions = copyListAddress,
                RegionCount = (uint)copyInfo.Length,
                DstBuffer = dstBuffer,
            };
            CreateVulkan.vk.CmdCopyBuffer2(transferCommandBuffers[currentTransferCommandBuffer], ref _cbI);
            transferCommandPoolManager.AddStagingBuffer(currentTransferCommandBuffer, srcBuffer);
        }
    }

    public CommandBuffer? GetCommandBuffer(uint transferCommandBuffer)
    {
        if (!sthWasAdded)
            return null;
        return transferCommandBuffers[transferCommandBuffer];
    }

    public void SetNextCommandBuffer(uint nextCommandBuffer)
    {
        currentTransferCommandBuffer = nextCommandBuffer;
    }
}
