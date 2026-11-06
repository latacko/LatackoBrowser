using System;
using Silk.NET.Vulkan;
using Vulkan;

namespace VulkanManager;

[InstanceFinderCore.Instance(Priority =-100)]
public class TransferCommandPoolManager : IDisposable, IRenderTick
{
    public const uint TransferCommandBuffers = 4;
    public readonly CommandPool[] CommandPools = new CommandPool[TransferCommandBuffers];
    Fence[] transferDoneFences = new Fence[TransferCommandBuffers];
    ulong currentTransferDoneSignalValue;
    ulong nextTransferDoneSignalValue = 1;
    uint[] registeredCB = new uint[TransferCommandBuffers];
    readonly CommandBufferSubmitInfo[][] transferCommandBuffers = new CommandBufferSubmitInfo[TransferCommandBuffers][];
    readonly List<BufferData>[] stagingBuffers = new List<BufferData>[TransferCommandBuffers];
    readonly List<ITransfer> transfersManagers = [];

    uint currentCommandBuffer = 0;
    private int isTransferRunning = 0; // 0 = idle, 1 = running

    public TransferCommandPoolManager()
    {
        CreateCommandPools();
        CreateFences();

        for (int i = 0; i < TransferCommandBuffers; i++)
        {
            registeredCB[i] = 0;
            transferCommandBuffers[i] = [];
            stagingBuffers[i] = [];
        }
    }

    public void RegisterTransferManagers(params ITransfer[] transfers)
    {
        foreach (var transfer in transfers)
        {
            transfersManagers.Add(transfer);
        }
    }

    unsafe void CreateCommandPools()
    {
        CommandPoolCreateInfo commandPoolCI = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.TransientBit,
            QueueFamilyIndex = LogicalDevice.Indices.TransferFamily!.Value,
        };

        for (int i = 0; i < TransferCommandBuffers; i++)
        {
            CreateVulkan.vk.CreateCommandPool(LogicalDevice.device, ref commandPoolCI, null, out CommandPools[i]);
        }
    }

    unsafe void CreateFences()
    {
        FenceCreateInfo fenceCI = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };

        fixed (Fence* fencesPtr = transferDoneFences)
        {
            for (int i = 0; i < TransferCommandBuffers; i++)
            {
                CreateVulkan.vk.CreateFence(LogicalDevice.device, &fenceCI, null, &fencesPtr[i]);
            }
        }
    }

    public void ResetCommandPool(uint frameInFlight)
    {
        CreateVulkan.vk.ResetCommandPool(LogicalDevice.device, CommandPools[frameInFlight], CommandPoolResetFlags.None);
    }

    public unsafe CommandBuffer[] CreateManagerCommandBuffers()
    {
        CommandBuffer[] _commandBuffers = new CommandBuffer[TransferCommandBuffers];
        for (int i = 0; i < TransferCommandBuffers; i++)
        {
            CommandBufferAllocateInfo commandBufferCI = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = CommandPools[i],
                CommandBufferCount = 1
            };

            fixed (CommandBuffer* commandBufferPtr = &_commandBuffers[i])
                CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, ref commandBufferCI, commandBufferPtr);
        }

        return _commandBuffers;
    }

    void AddCommandBuffer(CommandBuffer commandBuffer)
    {
        registeredCB[currentCommandBuffer]++;

        if (registeredCB[currentCommandBuffer] > transferCommandBuffers[currentCommandBuffer].Length)
        {
            var _newCBArray = new CommandBufferSubmitInfo[registeredCB[currentCommandBuffer]];
            Array.Copy(transferCommandBuffers, _newCBArray, transferCommandBuffers.Length);
            transferCommandBuffers[currentCommandBuffer] = _newCBArray;
        }

        transferCommandBuffers[currentCommandBuffer][(int)registeredCB[currentCommandBuffer]] = new()
        {
            SType = StructureType.CommandBufferSubmitInfo,
            CommandBuffer = commandBuffer,
        };
    }

    public void AddStagingBuffer(uint transferCommandBuffer, BufferData stagingBuffer)
    {
        stagingBuffers[transferCommandBuffer].Add(stagingBuffer);
    }

    public unsafe void RenderTick(uint appFrameInFlight)
    {
        // The transfer on the current cb is not done 
        if (CreateVulkan.vk.GetFenceStatus(LogicalDevice.device, transferDoneFences[currentCommandBuffer]) == Result.Success) return;
        stagingBuffers[currentCommandBuffer].Clear();
        uint _nextCommandBuffer = (currentCommandBuffer + 1) % TransferCommandBuffers;

        bool _isSthToTransfer = false;
        registeredCB[currentCommandBuffer] = 0;
        foreach (var transfer in transfersManagers)
        {
            var _cb = transfer.GetCommandBuffer(currentCommandBuffer);
            if (!_cb.HasValue) continue;

            AddCommandBuffer(_cb.Value);
            _isSthToTransfer = true;
        }

        if (!_isSthToTransfer) return;

        foreach (var transfer in transfersManagers)
        {
            transfer.SetNextCommandBuffer(_nextCommandBuffer);
        }

        if (registeredCB[currentCommandBuffer] == 0) return;

        SubmitInfo2 submitInfo = new()
        {
            SType = StructureType.SubmitInfo2,
            WaitSemaphoreInfoCount = 0,
        };

        submitInfo.CommandBufferInfoCount = registeredCB[currentCommandBuffer];
        fixed (CommandBufferSubmitInfo* ptrCB = transferCommandBuffers[currentCommandBuffer])
            submitInfo.PCommandBufferInfos = ptrCB;

        if (CreateVulkan.vk.QueueSubmit2(LogicalDevice.TransferQueue, 1, &submitInfo, transferDoneFences[currentCommandBuffer]) != Result.Success)
        {
            throw new Exception("Failed to submit command buffer!");
        }

        ResetCommandPool(currentCommandBuffer);

        if (Interlocked.CompareExchange(ref isTransferRunning, 1, 0) != 0)
        {
            // Someone else already has it running — do nothing.
            return;
        }

        Task.Run(() =>
        {
            try
            {
                WaitForTransferToEnd(currentCommandBuffer);
            }
            finally
            {
                // Reset so a future request can start it again.
                Interlocked.Exchange(ref isTransferRunning, 0);
            }
        });
        currentCommandBuffer = _nextCommandBuffer;
    }

    void WaitForTransferToEnd(uint currentCommandBuffer)
    {
        uint _currentCb = currentCommandBuffer;
        while (CreateVulkan.vk.GetFenceStatus(LogicalDevice.device, transferDoneFences[_currentCb]) == Result.Success)
        {
            CreateVulkan.vk.WaitForFences(LogicalDevice.device, 1, in transferDoneFences[_currentCb], Vk.True, ulong.MaxValue);

            

            _currentCb = (_currentCb + 1) % TransferCommandBuffers;
        }
    }



    public unsafe void Dispose()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.DestroyCommandPool(LogicalDevice.device, CommandPools[i], null);
        }
    }
}
