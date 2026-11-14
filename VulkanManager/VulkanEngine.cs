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

    public CommandPool[] commandPools = new CommandPool[MAX_FRAMES_IN_FLIGHT];


    public VulkanEngine()
    {
        CreateCommandPools();
        CreateSynchronizationObjects();
    }

    void CreateSynchronizationObjects()
    {
        SemaphoreCreateInfo semaphoreCI = new(flags:null);

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
        CommandPoolCreateInfo commandPoolCI = new(
            flags: CommandPoolCreateFlags.None,
            queueFamilyIndex: LogicalDevice.Indices.GraphicsFamily!.Value
        );

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.CreateCommandPool(LogicalDevice.device, ref commandPoolCI, null, out commandPools[i]);
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.DestroySemaphore(LogicalDevice.device, renderCompleteSemaphores[i], null);
            CreateVulkan.vk.DestroyCommandPool(LogicalDevice.device, commandPools[i], null);
        }
    
    }
}