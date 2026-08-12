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
    Vulkan.VulkanEngine vulkanManager = new();
    bool framebufferResized;

    Vulkan.Swapchain swapchain;
    internal KhrSurface khrSurface;
    internal SurfaceKHR surface;

    void SetupVulkan()
    {
        CreateSurface();

        swapchain = new(window.FramebufferSize, khrSurface, surface);
        swapchain.CreateSwapChain();
        swapchain.CreateImageViews();
        vulkanManager.Init();
    }

    void CreateSurface()
    {
        if (!Vulkan.CreateVulkan.vk!.TryGetInstanceExtension(Vulkan.CreateVulkan.vulkanInstance, out khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        surface = window!.VkSurface!.Create<AllocationCallbacks>(Vulkan.CreateVulkan.vulkanInstance.ToHandle(), null).ToSurface();
    }

    void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex)
    {
        CommandBufferBeginInfo _beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,

            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
            PInheritanceInfo = null,
        };



        if (Vulkan.CreateVulkan.vk.BeginCommandBuffer(commandBuffer, &_beginInfo) != Result.Success)
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
            new()
            {
                SType = StructureType.ImageMemoryBarrier2,
                SrcStageMask = PipelineStageFlags2.LateFragmentTestsBit,
                SrcAccessMask = AccessFlags2.DepthStencilAttachmentWriteBit,

                DstStageMask = PipelineStageFlags2.EarlyFragmentTestsBit,
                DstAccessMask = AccessFlags2.DepthStencilAttachmentWriteBit,

                OldLayout = ImageLayout.Undefined,
                NewLayout = ImageLayout.AttachmentOptimal,

                Image = swapchain.GetDepthImage(),
                SubresourceRange = new()
                {
                    AspectMask = ImageAspectFlags.DepthBit,
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

        RenderingAttachmentInfo _colorAttachmentInfo = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = swapchain.swapChainImageViews[imageIndex],
            ImageLayout = ImageLayout.AttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new()
            {
                Color = new(0, 0, 0, 1),
            }
        };

        RenderingAttachmentInfo _depthAttachmentInfo = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = swapchain.GetDepthImageView(),
            ImageLayout = ImageLayout.AttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            ClearValue = new()
            {
                DepthStencil = new(1, 0),
            }
        };

        RenderingInfo _renderingInfo = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new()
            {
                Offset = new(0, 0),
                Extent = swapchain.swapChainExtent
            },
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &_colorAttachmentInfo,
            PDepthAttachment = &_depthAttachmentInfo
        };

        Vulkan.CreateVulkan.vk.CmdBeginRendering(commandBuffer, &_renderingInfo);

        Viewport _vp = new()
        {
            X = 0,
            Y = 0,
            Width = swapchain.swapChainExtent.Width,
            Height = swapchain.swapChainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };
        Vulkan.CreateVulkan.vk.CmdSetViewport(commandBuffer, 0, 1, &_vp);
        Rect2D _scissors = new()
        {
            Offset = new(0, 0),
            Extent = swapchain.swapChainExtent
        };
        Vulkan.CreateVulkan.vk.CmdSetScissor(commandBuffer, 0, 1, &_scissors);


        foreach (var shader in loadedShaders)
        {
            shader.Render(commandBuffer, currentFrame, wireFrameRendering);
        }

        coreManager.RenderShader(commandBuffer, currentFrame, wireFrameRendering);

        if (swapchain.recreatedSwapChain)
            swapchain.recreatedSwapChain = false;

        Vulkan.CreateVulkan.vk.CmdEndRendering(commandBuffer);

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
        vulkanManager.Dispose();
        khrSurface.DestroySurface(Vulkan.CreateVulkan.vulkanInstance, surface, null);
    }
}