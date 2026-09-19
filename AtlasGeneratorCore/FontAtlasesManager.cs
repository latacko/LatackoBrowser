using Silk.NET.Vulkan;
using Vulkan;

namespace AtlasGeneratorCore;

public class FontAtlasesManager: IDisposable
{

    public static FontAtlasesManager? Instance { get; private set; }

    internal CommandPool[] CommandPools = new CommandPool[VulkanEngine.MAX_FRAMES_IN_FLIGHT];

    CommandBufferSubmitInfo[] atlasesCommandBuffer = new CommandBufferSubmitInfo[2];
    uint registeredCB = 0;
    ulong nextAtlasDoneSignalValue = 1;
    ulong currentAtlasDoneSignalValue;
    Silk.NET.Vulkan.Semaphore atlasDoneSemaphore;
    public FontAtlasesManager()
    {
        Instance = this;
        CreateCommandPools();
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

        CreateVulkan.vk.CreateSemaphore(LogicalDevice.device, ref _semCI, null, out atlasDoneSemaphore);
    }

    public ulong GetNextDoneSignalValue() => nextAtlasDoneSignalValue;
    public ulong GetCurrentDoneSignalValue() => currentAtlasDoneSignalValue;

    public static void ResetCommandPool(uint FrameInFlight)
    {
        CreateVulkan.vk.ResetCommandPool(LogicalDevice.device, Instance!.CommandPools[FrameInFlight], CommandPoolResetFlags.None);
    }

    public static void RegisterCommandBuffer(CommandBuffer commandBuffer)
    {
        if (Instance == null) throw new NullReferenceException("The font atlases manager hasn't been initialized yet!");

        Instance.registeredCB++;

        if (Instance.registeredCB > Instance.atlasesCommandBuffer.Length)
        {
            var _newCBArray = new CommandBufferSubmitInfo[Instance.registeredCB];
            Array.Copy(Instance.atlasesCommandBuffer, _newCBArray, Instance.atlasesCommandBuffer.Length);
            Instance.atlasesCommandBuffer = _newCBArray;
        }

        Instance.atlasesCommandBuffer[Instance.registeredCB - 1] = new()
        {
            SType = StructureType.CommandBufferSubmitInfo,
            CommandBuffer = commandBuffer,
        };
    }

    public unsafe void RenderTick()
    {
        CreateVulkan.vk.GetSemaphoreCounterValue(LogicalDevice.device, atlasDoneSemaphore, out currentAtlasDoneSignalValue);
        if (registeredCB == 0) return;

        SubmitInfo2 submitInfo = new()
        {
            SType = StructureType.SubmitInfo2,
            WaitSemaphoreInfoCount = 0,
        };

        submitInfo.CommandBufferInfoCount = registeredCB;
        fixed (CommandBufferSubmitInfo* ptrCB = atlasesCommandBuffer)
            submitInfo.PCommandBufferInfos = ptrCB;

        SemaphoreSubmitInfo _signalSemaphoreInfo = new()
        {
            SType = StructureType.SemaphoreSubmitInfo,
            Semaphore = atlasDoneSemaphore,
            Value = nextAtlasDoneSignalValue++
        };

        submitInfo.SignalSemaphoreInfoCount = 1;
        submitInfo.PSignalSemaphoreInfos = &_signalSemaphoreInfo;

        if (CreateVulkan.vk.QueueSubmit2(LogicalDevice.TransferQueue, 1, &submitInfo, default) != Result.Success)
        {
            throw new Exception("Failed to submit command buffer!");
        }

        atlasesCommandBuffer = new CommandBufferSubmitInfo[2];
        registeredCB = 0;
    }

    public unsafe void Dispose()
    {
        CreateVulkan.vk.DestroySemaphore(LogicalDevice.device, atlasDoneSemaphore, null);

        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.DestroyCommandPool(LogicalDevice.device, CommandPools[i], null);
        }
    }
}
