using System.Runtime.Serialization;
using Iced.Intel;
using Vulkan;
using Silk.NET.Assimp;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using TextCore;
using Units;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Browser;

public unsafe partial class BrowserWindow
{
    bool framebufferResized;

    Vulkan.Swapchain swapchain;
    internal KhrSurface khrSurface;
    internal SurfaceKHR surface;

    readonly CommandBuffer[] commandBuffers = new CommandBuffer[VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    readonly Fence[] fences = new Fence[VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    readonly Semaphore[] renderCompleteSemaphores = new Semaphore[VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    readonly Semaphore[] imageAcquiredSemaphores = new Semaphore[VulkanEngine.MAX_FRAMES_IN_FLIGHT];


    public void SetupVulkan()
    {
        swapchain.CreateSwapChain();
        swapchain.CreateImageViews();

        CreateSynchronizationObjects();
    }

    void CreateSynchronizationObjects()
    {
        SemaphoreCreateInfo semaphoreCI = new(flags: null);

        FenceCreateInfo fenceCI = new(
            flags: FenceCreateFlags.SignaledBit
        );

        fixed (Fence* fencesPtr = fences)
        fixed (Semaphore* renderCompleteSemaphoresPtr = renderCompleteSemaphores)
        fixed (Semaphore* imgSemaphorePtr = imageAcquiredSemaphores)
        {
            for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
            {
                CreateVulkan.vk.CreateFence(LogicalDevice.device, &fenceCI, null, &fencesPtr[i]);
                CreateVulkan.vk.CreateSemaphore(LogicalDevice.device, &semaphoreCI, null, &imgSemaphorePtr[i]);
            }

            for (int i = 0; i < renderCompleteSemaphores.Length; i++)
            {
                CreateVulkan.vk.CreateSemaphore(LogicalDevice.device, &semaphoreCI, null, &renderCompleteSemaphoresPtr[i]);
            }
        }
    }

    public void CreateSurfaceAndSwapchain()
    {
        if (!Vulkan.CreateVulkan.vk!.TryGetInstanceExtension(Vulkan.CreateVulkan.vulkanInstance, out khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        surface = window!.VkSurface!.Create<AllocationCallbacks>(Vulkan.CreateVulkan.vulkanInstance.ToHandle(), null).ToSurface();
        swapchain = new(window.FramebufferSize, khrSurface, surface);
    }

    void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex, Image siteRendererImg)
    {
        CommandBufferBeginInfo _beginInfo = new(
            flags: CommandBufferUsageFlags.OneTimeSubmitBit

        );

        if (CreateVulkan.vk.BeginCommandBuffer(commandBuffer, &_beginInfo) != Result.Success)
        {
            throw new Exception("Failed to begin recording command buffer!");
        }

        ImageMemoryBarrier2 _outputBarriers = new(
            srcStageMask: PipelineStageFlags2.ColorAttachmentOutputBit,
            srcAccessMask: 0,

            dstStageMask: PipelineStageFlags2.ColorAttachmentOutputBit,
            dstAccessMask: AccessFlags2.ColorAttachmentReadBit | AccessFlags2.ColorAttachmentWriteBit,

            oldLayout: ImageLayout.Undefined,
            newLayout: ImageLayout.AttachmentOptimal,

            image: swapchain.swapChainImages[imageIndex],
            subresourceRange: new(
                aspectMask: ImageAspectFlags.ColorBit,
                levelCount: 1,
                layerCount: 1
            )
        );

        DependencyInfo _barrierDependencyInfo = new(
            imageMemoryBarrierCount: 1,
            pImageMemoryBarriers: &_outputBarriers
        );

        Vulkan.CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, ref _barrierDependencyInfo);

        var copyRegion = new ImageCopy(
            srcSubresource: new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            srcOffset: new Offset3D(0, 0, 0),
            dstSubresource: new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            dstOffset: new Offset3D(0, 0, 0),
            extent: new Extent3D(swapchain.swapChainExtent.Width, swapchain.swapChainExtent.Height, 1)
        );

        CreateVulkan.vk.CmdCopyImage(
            commandBuffer,
            siteRendererImg, ImageLayout.TransferSrcOptimal,
            swapchain.swapChainImages[imageIndex], ImageLayout.TransferDstOptimal,
            1, &copyRegion);


        ImageMemoryBarrier2 _barrierPresent = new(
            srcStageMask: PipelineStageFlags2.ColorAttachmentOutputBit,
            srcAccessMask: AccessFlags2.ColorAttachmentWriteBit,

            dstStageMask: PipelineStageFlags2.ColorAttachmentOutputBit,
            dstAccessMask: 0,

            oldLayout: ImageLayout.AttachmentOptimal,
            newLayout: ImageLayout.PresentSrcKhr,

            image: swapchain.swapChainImages[imageIndex],
            subresourceRange: new(
                aspectMask: ImageAspectFlags.ColorBit,
                levelCount: 1,
                layerCount: 1
            )
        );

        DependencyInfo _barrierPresentDependencyInfo = new(
            imageMemoryBarrierCount: 1,
            pImageMemoryBarriers: &_barrierPresent
        );

        Vulkan.CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, ref _barrierPresentDependencyInfo);

        if (Vulkan.CreateVulkan.vk.EndCommandBuffer(commandBuffer) != Result.Success)
        {
            throw new Exception("Failed to record command buffer");
        }
    }

    void Dispose()
    {
        swapchain.Dispose();
        khrSurface.DestroySurface(Vulkan.CreateVulkan.vulkanInstance, surface, null);
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.DestroySemaphore(LogicalDevice.device, imageAcquiredSemaphores[i], null);
            CreateVulkan.vk.DestroySemaphore(LogicalDevice.device, renderCompleteSemaphores[i], null);
            CreateVulkan.vk.DestroyFence(LogicalDevice.device, fences[i], null);
        }
    }
}