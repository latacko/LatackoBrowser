using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using VulkanManager.Helpers;
using Semaphore = Silk.NET.Vulkan.Semaphore;

[assembly: InternalsVisibleTo("Browser")]
[assembly: InternalsVisibleTo("GraphicsCore")]
[assembly: InternalsVisibleTo("TextCore")]
[assembly: InternalsVisibleTo("Renderer")]
namespace Vulkan;

public unsafe class VulkanEngine : IDisposable
{
    public const int MAX_FRAMES_IN_FLIGHT = 2;

    // internal event Action CreateBuffers;

    // public Sampler sampler;

    #region Syncing
    public Semaphore[] renderCompleteSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
    #endregion

    public CommandPool[] commandPools;


    public VulkanEngine()
    {
        CreateCommandPools();
        CreateSynchronizationObjects();
    }

    void CreateSynchronizationObjects()
    {
        SemaphoreCreateInfo semaphoreCI = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        fixed (Semaphore* renderCompleteSemaphoresPtr = renderCompleteSemaphores)
        {
            for (int i = 0; i < renderCompleteSemaphores.Length; i++)
            {
                CreateVulkan.vk.CreateSemaphore(LogicalDevice.device, &semaphoreCI, null, &renderCompleteSemaphoresPtr[i]);
            }
        }
    }

    public void CreateCommandPools()
    {
        var queueFamilyIndices = PhysicalDevice.Instance.FindQueueFamilies(PhysicalDevice.physicalDevice);
        CommandPoolCreateInfo commandPoolCI = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.None,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
        };

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.CreateCommandPool(LogicalDevice.device, ref commandPoolCI, null, out commandPools[i]);
        }
    }

    // void CreateImageSampler()
    // {
    //     SamplerCreateInfo _samplerCI = new()
    //     {
    //         SType = StructureType.SamplerCreateInfo,
    //         MagFilter = Filter.Linear,
    //         MinFilter = Filter.Linear,
    //         MipmapMode = SamplerMipmapMode.Linear,
    //         AnisotropyEnable = false,
    //         MinLod = 0,
    //         MaxLod = 0, // = 1000.0f, allows all mip levels
    //         AddressModeU = SamplerAddressMode.ClampToEdge, // good for atlas
    //         AddressModeV = SamplerAddressMode.ClampToEdge,
    //         AddressModeW = SamplerAddressMode.ClampToEdge,
    //     };

    //     CreateVulkan.vk.GetPhysicalDeviceProperties(PhysicalDevice.physicalDevice, out var properties);
    //     _samplerCI.MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy;

    //     // Console.WriteLine("Creating sampler");
    //     fixed (Sampler* samplerPtr = &sampler)
    //         CreateVulkan.vk.CreateSampler(LogicalDevice.device, &_samplerCI, null, samplerPtr);
    // }

    public void Dispose()
    {
        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.DestroySemaphore(LogicalDevice.device, renderCompleteSemaphores[i], null);
            CreateVulkan.vk.DestroyCommandPool(LogicalDevice.device, commandPools[i], null);
        }
    
    }
}