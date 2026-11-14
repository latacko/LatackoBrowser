using System;
using Silk.NET.Vulkan;
using Vulkan;

namespace VulkanManager;

[InstanceFinderCore.Instance(Priority = -100)]
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
        CommandPoolCreateInfo commandPoolCI = new(
            flags: CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit,
            queueFamilyIndex: LogicalDevice.Indices.TransferFamily!.Value
        );

        for (int i = 0; i < TransferCommandBuffers; i++)
        {
            CreateVulkan.vk.CreateCommandPool(LogicalDevice.device, ref commandPoolCI, null, out CommandPools[i]);
        }
    }

    unsafe void CreateFences()
    {
        FenceCreateInfo fenceCI = new(
            flags: FenceCreateFlags.SignaledBit
        );

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
            CommandBufferAllocateInfo commandBufferCI = new(
                commandPool: CommandPools[i],
                commandBufferCount: 1
            );

            fixed (CommandBuffer* commandBufferPtr = &_commandBuffers[i])
                CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, ref commandBufferCI, commandBufferPtr);
        }

        return _commandBuffers;
    }
    public void BeginRecording()
    {
        if (CreateVulkan.vk.GetFenceStatus(LogicalDevice.device, transferDoneFences[currentCommandBuffer]) != Result.Success) return;

        foreach (var manager in transfersManagers)
        {
            manager.BeginRecording();
        }
    }

    public void EndRecording()
    {
        if (CreateVulkan.vk.GetFenceStatus(LogicalDevice.device, transferDoneFences[currentCommandBuffer]) != Result.Success) return;

        foreach (var manager in transfersManagers)
        {
            manager.EndRecording();
        }
    }

    unsafe void AddCommandBuffer(CommandBuffer commandBuffer)
    {
        registeredCB[currentCommandBuffer]++;

        if (registeredCB[currentCommandBuffer] > transferCommandBuffers[currentCommandBuffer].Length)
        {
            var _newCBArray = new CommandBufferSubmitInfo[registeredCB[currentCommandBuffer]];
            Console.WriteLine("Length prev: " + transferCommandBuffers[currentCommandBuffer].Length);
            Array.Copy(transferCommandBuffers[currentCommandBuffer], _newCBArray, transferCommandBuffers[currentCommandBuffer].Length);
            transferCommandBuffers[currentCommandBuffer] = _newCBArray;
        }

        transferCommandBuffers[currentCommandBuffer][(int)registeredCB[currentCommandBuffer] - 1] = new(
            commandBuffer: commandBuffer
        );

        Console.WriteLine("At current buffer: " + currentCommandBuffer + " added cb");
    }

    public void AddStagingBuffer(uint transferCommandBuffer, BufferData stagingBuffer)
    {
        stagingBuffers[transferCommandBuffer].Add(stagingBuffer);
    }

    public unsafe void RenderTick(uint appFrameInFlight)
    {
        // The transfer on the current cb is not done 
        if (CreateVulkan.vk.GetFenceStatus(LogicalDevice.device, transferDoneFences[currentCommandBuffer]) != Result.Success) return;
        CreateVulkan.vk.ResetFences(LogicalDevice.device, 1, in transferDoneFences[currentCommandBuffer]);
        // stagingBuffers[currentCommandBuffer].Clear();
        // Console.WriteLine("Transfer render tick");
        uint _nextCommandBuffer = (currentCommandBuffer + 1) % TransferCommandBuffers;

        bool _isSthToTransfer = false;
        // registeredCB[currentCommandBuffer] = 0;
        foreach (var transfer in transfersManagers)
        {
            var _cb = transfer.GetCommandBuffer(currentCommandBuffer);
            // Console.WriteLine("Testing if " + transfer + " has ready command buffer");
            if (!_cb.HasValue) continue;

            AddCommandBuffer(_cb.Value);
            _isSthToTransfer = true;
            Console.WriteLine("At frame: " + currentCommandBuffer + " is sth to transfer");
        }

        if (!_isSthToTransfer) return;

        Console.WriteLine("Triggering screen in renderdoc");
        RenderDocIntegration.RenderDocApi.StartCapture(CreateVulkan.vulkanInstance.Handle, IntPtr.Zero);

        foreach (var transfer in transfersManagers)
        {
            transfer.SetNextCommandBuffer(_nextCommandBuffer);
        }

        if (registeredCB[currentCommandBuffer] == 0) return;

        SubmitInfo2 submitInfo = new(
            waitSemaphoreInfoCount: 0
        );

        submitInfo.CommandBufferInfoCount = registeredCB[currentCommandBuffer];
        fixed (CommandBufferSubmitInfo* ptrCB = transferCommandBuffers[currentCommandBuffer])
        {
            submitInfo.PCommandBufferInfos = ptrCB;
            if (CreateVulkan.vk.QueueSubmit2(LogicalDevice.TransferQueue, 1, &submitInfo, transferDoneFences[currentCommandBuffer]) != Result.Success)
            {
                throw new Exception("Failed to submit command buffer!");
            }
        }
        RenderDocIntegration.RenderDocApi.TriggerCapture();

        RenderDocIntegration.RenderDocApi.EndCapture(CreateVulkan.vulkanInstance.Handle, IntPtr.Zero);


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
        while (CreateVulkan.vk.GetFenceStatus(LogicalDevice.device, transferDoneFences[_currentCb]) != Result.Success)
        {
            CreateVulkan.vk.WaitForFences(LogicalDevice.device, 1, in transferDoneFences[_currentCb], Vk.True, ulong.MaxValue);
            CleanTransferStage(_currentCb);
            _currentCb = (_currentCb + 1) % TransferCommandBuffers;

        }
    }

    void CleanTransferStage(uint currentCommandBuffer)
    {
        ResetCommandPool(currentCommandBuffer);

        foreach (var stagingBuffer in stagingBuffers[currentCommandBuffer])
        {
            BufferHelper.DestroyBuffer(stagingBuffer);
        }
        stagingBuffers[currentCommandBuffer].Clear();
        registeredCB[currentCommandBuffer] = 0;
    }



    public unsafe void Dispose()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.DestroyCommandPool(LogicalDevice.device, CommandPools[i], null);
        }
    }
}
