using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using VulkanManager.Helpers;
using Semaphore = Silk.NET.Vulkan.Semaphore;

[assembly: InternalsVisibleTo("Browser")]
[assembly: InternalsVisibleTo("GraphicsCore")]
[assembly: InternalsVisibleTo("TextCore")]
namespace Vulkan;

public unsafe class VulkanEngine : IDisposable
{
    public static VulkanEngine Instance;
    public const int MAX_FRAMES_IN_FLIGHT = 2;

    public CommandBuffer[] commandBuffers = new CommandBuffer[MAX_FRAMES_IN_FLIGHT];
    public CameraBuffers cameraBuffers = new();
    internal event Action CreateBuffers;

    public Sampler sampler;

    #region Syncing
    public Semaphore[] renderCompleteSemaphores;
    public Semaphore[] imageAcquiredSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
    public Fence[] fences = new Fence[MAX_FRAMES_IN_FLIGHT];
    #endregion

    internal static CommandPool commandPool;

    public VulkanEngine()
    {
        Instance = this;
    }

    internal void Init()
    {
        CreateSynchronizationObjects();

        CreateCommandPool();
        CreateCommandBuffers();

        CreateImageSampler();

        CreateShaderDataBuffers();
    }

    void CreateShaderDataBuffers()
    {
        cameraBuffers.CreateBuffers();
        CreateBuffers?.Invoke();
    }

    void CreateSynchronizationObjects()
    {
        renderCompleteSemaphores = new Semaphore[Swapchain.Instance.swapChainImages.Length];
        var _semaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        var _fences = new Fence[MAX_FRAMES_IN_FLIGHT];

        SemaphoreCreateInfo semaphoreCI = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        FenceCreateInfo fenceCI = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };

        fixed (Fence* fencesPtr = fences)
        fixed (Semaphore* semaphoresPtr = imageAcquiredSemaphores)
        fixed (Semaphore* renderCompleteSemaphoresPtr = renderCompleteSemaphores)
        {
            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                CreateVulkan.vk.CreateFence(LogicalDevice.device, &fenceCI, null, &fencesPtr[i]);
                CreateVulkan.vk.CreateSemaphore(LogicalDevice.device, &semaphoreCI, null, &semaphoresPtr[i]);
            }

            for (int i = 0; i < renderCompleteSemaphores.Length; i++)
            {
                CreateVulkan.vk.CreateSemaphore(LogicalDevice.device, &semaphoreCI, null, &renderCompleteSemaphoresPtr[i]);
            }
        }
    }

    void CreateCommandPool()
    {
        var queueFamilyIndices = PhysicalDevice.Instance.FindQueueFamilies(PhysicalDevice.physicalDevice);
        CommandPoolCreateInfo commandPoolCI = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
        };

        CreateVulkan.vk.CreateCommandPool(LogicalDevice.device, ref commandPoolCI, null, out commandPool);
    }

    void CreateCommandBuffers()
    {
        CommandBufferAllocateInfo commandPoolCI = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            CommandBufferCount = MAX_FRAMES_IN_FLIGHT
        };

        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
            CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, ref commandPoolCI, commandBuffersPtr);
    }

    void CreateImageSampler()
    {
        SamplerCreateInfo _samplerCI = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AnisotropyEnable = Vk.True,
            MinLod = 0,
            MaxLod = Vk.LodClampNone, // = 1000.0f, allows all mip levels
            AddressModeU = SamplerAddressMode.ClampToEdge, // good for atlas
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
        };

        CreateVulkan.vk.GetPhysicalDeviceProperties(PhysicalDevice.physicalDevice, out var properties);
        _samplerCI.MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy;

        // Console.WriteLine("Creating sampler");
        fixed (Sampler* samplerPtr = &sampler)
            CreateVulkan.vk.CreateSampler(LogicalDevice.device, &_samplerCI, null, samplerPtr);
    }

    public void UpdateCameraBuffer(uint currentFrame, Matrix4X4<float> proj)
    {
        cameraBuffers.Update(currentFrame, proj);
    }

    public void Dispose()
    {
        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.DestroyFence(LogicalDevice.device, fences[i], null);
            CreateVulkan.vk.DestroySemaphore(LogicalDevice.device, imageAcquiredSemaphores[i], null);
        }

        for (int i = 0; i < renderCompleteSemaphores.Length; i++)
            CreateVulkan.vk.DestroySemaphore(LogicalDevice.device, renderCompleteSemaphores[i], null);

        CreateVulkan.vk.DestroyCommandPool(LogicalDevice.device, commandPool, null);

        CreateVulkan.vk.DestroySampler(LogicalDevice.device, sampler, null);

        cameraBuffers.Dispose();
    }
}