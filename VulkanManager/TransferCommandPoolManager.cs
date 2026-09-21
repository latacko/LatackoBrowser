using System;
using Silk.NET.Vulkan;
using Vulkan;

namespace VulkanManager;

public class TransferCommandPoolManager : IDisposable, IRenderTick
{
    internal CommandPool[] CommandPools = new CommandPool[VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    Silk.NET.Vulkan.Semaphore transferDoneSemaphore;
    ulong currentTransferDoneSignalValue;
    ulong nextTransferDoneSignalValue = 1;
    uint[] registeredCB = [];
    readonly CommandBufferSubmitInfo[][] transferCommandBuffers = [];

    public TransferCommandPoolManager()
    {
        CreateCommandPools();
        CreateSemaphore();

        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            registeredCB[i] = 0;
            transferCommandBuffers[i] = [];
        }
    }

    unsafe void CreateCommandPools()
    {
        CommandPoolCreateInfo commandPoolCI = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.None,
            QueueFamilyIndex = LogicalDevice.Indices.TransferFamily!.Value,
        };

        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.CreateCommandPool(LogicalDevice.device, ref commandPoolCI, null, out CommandPools[i]);
        }
    }

    unsafe void CreateSemaphore()
    {
        SemaphoreTypeCreateInfo _typeInfo = new()
        {
            SType = StructureType.SemaphoreTypeCreateInfo,
            SemaphoreType = SemaphoreType.Timeline,
            InitialValue = 0,
        };

        SemaphoreCreateInfo _semCI = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
            PNext = &_typeInfo,
        };

        CreateVulkan.vk.CreateSemaphore(LogicalDevice.device, ref _semCI, null, out transferDoneSemaphore);
    }

    public ulong GetNextDoneSignalValue() => nextTransferDoneSignalValue;
    public ulong GetCurrentDoneSignalValue() => currentTransferDoneSignalValue;

    public CommandPool GetCommandPool(uint frameInFlight)
    {
        return CommandPools[frameInFlight];
    }

    public void ResetCommandPool(uint frameInFlight)
    {
        CreateVulkan.vk.ResetCommandPool(LogicalDevice.device, GetCommandPool(frameInFlight), CommandPoolResetFlags.None);
    }

    public void AddCommandBuffer(uint frameInFlight, CommandBuffer commandBuffer)
    {
        registeredCB[frameInFlight]++;

        if (registeredCB[frameInFlight] > transferCommandBuffers[frameInFlight].Length)
        {
            var _newCBArray = new CommandBufferSubmitInfo[registeredCB[frameInFlight]];
            Array.Copy(transferCommandBuffers, _newCBArray, transferCommandBuffers.Length);
            transferCommandBuffers[frameInFlight] = _newCBArray;
        }

        transferCommandBuffers[frameInFlight][(int)registeredCB[frameInFlight]] = new()
        {
            SType = StructureType.CommandBufferSubmitInfo,
            CommandBuffer = commandBuffer,
        };
    }

    public unsafe void RenderTick(uint frameInFlight)
    {
        CreateVulkan.vk.GetSemaphoreCounterValue(LogicalDevice.device, transferDoneSemaphore, out currentTransferDoneSignalValue);
        if (registeredCB[frameInFlight] == 0) return;

        SubmitInfo2 submitInfo = new()
        {
            SType = StructureType.SubmitInfo2,
            WaitSemaphoreInfoCount = 0,
        };

        submitInfo.CommandBufferInfoCount = registeredCB[frameInFlight];

        fixed (CommandBufferSubmitInfo* ptrCB = transferCommandBuffers[frameInFlight])
            submitInfo.PCommandBufferInfos = ptrCB;

        SemaphoreSubmitInfo _signalSemaphoreInfo = new()
        {
            SType = StructureType.SemaphoreSubmitInfo,
            Semaphore = transferDoneSemaphore,
            Value = nextTransferDoneSignalValue
        };
        nextTransferDoneSignalValue++;

        submitInfo.SignalSemaphoreInfoCount = 1;
        submitInfo.PSignalSemaphoreInfos = &_signalSemaphoreInfo;

        if (CreateVulkan.vk.QueueSubmit2(LogicalDevice.TransferQueue, 1, &submitInfo, default) != Result.Success)
        {
            throw new Exception("Failed to submit command buffer!");
        }

        registeredCB[frameInFlight] = 0;
        ResetCommandPool(frameInFlight);
    }

    public unsafe void Dispose()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.DestroyCommandPool(LogicalDevice.device, CommandPools[i], null);
        }
    }
}
