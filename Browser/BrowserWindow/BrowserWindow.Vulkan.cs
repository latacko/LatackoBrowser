using System.Runtime.Serialization;
using Iced.Intel;
using ObjectCore;
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


    void SetupVulkan()
    {
        CreateSurface();

        swapchain = new(window.FramebufferSize, khrSurface, surface);
        swapchain.CreateSwapChain();
        swapchain.CreateImageViews();

        CreateSynchronizationObjects();
    }

    void CreateSynchronizationObjects()
    {
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

    void CreateSurface()
    {
        if (!Vulkan.CreateVulkan.vk!.TryGetInstanceExtension(Vulkan.CreateVulkan.vulkanInstance, out khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        surface = window!.VkSurface!.Create<AllocationCallbacks>(Vulkan.CreateVulkan.vulkanInstance.ToHandle(), null).ToSurface();
    }

    void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex, Image siteRendererImg)
    {
        CommandBufferBeginInfo _beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,

            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
            PInheritanceInfo = null,
        };



        if (CreateVulkan.vk.BeginCommandBuffer(commandBuffer, &_beginInfo) != Result.Success)
        {
            throw new Exception("Failed to begin recording command buffer!");
        }

        ImageMemoryBarrier2[] _outputBarriers = [
            new()
            {
                SType = StructureType.ImageMemoryBarrier2,
                SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
                SrcAccessMask=0,

                DstStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
                DstAccessMask = AccessFlags2.ColorAttachmentReadBit | AccessFlags2.ColorAttachmentWriteBit,

                OldLayout = ImageLayout.Undefined,
                NewLayout = ImageLayout.AttachmentOptimal,

                Image = swapchain.swapChainImages[imageIndex],
                SubresourceRange = new()
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    LevelCount = 1,
                    LayerCount = 1,
                },
            },
        ];

        fixed (ImageMemoryBarrier2* _outputBarriersPtr = _outputBarriers)
        {
            DependencyInfo _barrierDependencyInfo = new()
            {
                SType = StructureType.DependencyInfo,
                ImageMemoryBarrierCount = (uint)_outputBarriers.Length,
                PImageMemoryBarriers = _outputBarriersPtr,
            };
            Vulkan.CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, ref _barrierDependencyInfo);
        }

        var copyRegion = new ImageCopy
        {
            SrcSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            SrcOffset = new Offset3D(0, 0, 0),
            DstSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            DstOffset = new Offset3D(0, 0, 0),
            Extent = new Extent3D(swapchain.swapChainExtent.Width, swapchain.swapChainExtent.Height, 1)
        };

        CreateVulkan.vk.CmdCopyImage(
            commandBuffer,
            siteRendererImg, ImageLayout.TransferSrcOptimal,
            swapchain.swapChainImages[imageIndex], ImageLayout.TransferDstOptimal,
            1, &copyRegion);


        ImageMemoryBarrier2 _barrierPresent = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            SrcAccessMask = AccessFlags2.ColorAttachmentWriteBit,

            DstStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            DstAccessMask = 0,

            OldLayout = ImageLayout.AttachmentOptimal,
            NewLayout = ImageLayout.PresentSrcKhr,

            Image = swapchain.swapChainImages[imageIndex],
            SubresourceRange = new()
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LevelCount = 1,
                LayerCount = 1,
            },
        };

        DependencyInfo _barrierPresentDependencyInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &_barrierPresent,
        };

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