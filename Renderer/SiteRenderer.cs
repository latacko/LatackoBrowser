using System;
using GraphicsCore.Styles;
using PrimitiveCore;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Vulkan;
using VulkanManager;
using VulkanManager.BufferManager;
using VulkanManager.Structs;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Renderer;

public class SiteRenderer : TextureRenderer, IDisposable
{
    [Flags]
    public enum DirtyFlag
    {
        None,
        Size = 1 << 0
    }

    public readonly uint UniqueId;

    static uint LastUniqueId;

    uint width;
    uint height;
    Extent2D screenExtent;

    readonly StylesManager stylesManager = new();

    public readonly FPSData[] FPS = new FPSData[VulkanEngine.MAX_FRAMES_IN_FLIGHT];

    #region Vulkan elements
    readonly ImageData[] imagesData = new ImageData[VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    readonly Depth depth = new();

    DirtyFlag dirty;


    internal uint CurrentFrame { get; private set; } = 0;
    VulkanEngine? vulkanEngine;

    public CameraBuffers cameraBuffers = new();

    internal CommandBuffer[] commandBuffers = new CommandBuffer[VulkanEngine.MAX_FRAMES_IN_FLIGHT];

    #endregion

    #region Sub site renderers (e.g. iframes)
    SiteRenderer? parentSiteRenderer;
    SiteRenderer[]? subSiteRenderers;

    CommandBufferSubmitInfo[]? subSiteCommandBuffers;
    #endregion

    public SiteRenderer()
    {
        UniqueId = LastUniqueId++;
        if (RenderEngine.Instance == null)
            throw new System.NullReferenceException("The render engine hasn't been initialized");
        RenderEngine.Instance.RegisterSite(this);
    }

    #region Setup
    public override void Init(uint width, uint height)
    {

        CreateResources(width, height);
        cameraBuffers.CreateBuffers();
    }

    //TODO - Clear prev image and render to new texture
    public override void Resize(uint width, uint height)
    {
        dirty |= DirtyFlag.Size;
        this.width = width;
        this.height = height;
        screenExtent = new(width, height);

        DisposeImages();
        CreateResources(width, height);
    }

    void CreateResources(uint width, uint height)
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            ImageData _imageData = new();
            ImageHelper.CreateImage(width, height, Format.B8G8R8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferSrcBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, 1, ref _imageData.image, ref _imageData.deviceMemory);
            _imageData.imageView = ImageHelper.CreateImageView(_imageData.image, Format.B8G8R8A8Srgb, ImageAspectFlags.ColorBit);
            imagesData[i] = _imageData;
        }
        depth.CreateDepthResources(width, height);
    }

    void DisposeImages()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            imagesData[i].Dispose();
        }
        depth.Dispose();
    }

    unsafe void CreateCommandBuffers(VulkanEngine vulkanEngine)
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CommandBufferAllocateInfo commandBufferCI = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = vulkanEngine.commandPools[i],
                CommandBufferCount = 1
            };

            fixed (CommandBuffer* commandBufferPtr = &commandBuffers[i])
                CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, ref commandBufferCI, commandBufferPtr);
        }
    }

    public override void CreateVulkanEngine()
    {
        vulkanEngine = new();
        CreateCommandBuffers(vulkanEngine);
        subSiteCommandBuffers = new CommandBufferSubmitInfo[1];
    }

    public override void DestroyVulkanEngine()
    {
        vulkanEngine.Dispose();
    }

    public override VulkanEngine GetVulkanEngine()=>vulkanEngine;

    public override uint GetId()=>UniqueId;
    public override ulong GetCameraBufferDeviceAddress()=>cameraBuffers.shaderDataBuffersForCamera[CurrentFrame].DeviceAddress;

    #endregion

    #region Rendering
    //TODO - Render to texture
    public unsafe uint GetCommandBuffer()
    {
        UpdateCameraUBO(CurrentFrame);

        // coreManager.OnRender(CurrentFrame);
        stylesManager.ComputeStyles();

        RecordCommandBuffer(commandBuffers[CurrentFrame], CurrentFrame);
        stylesManager.SetFrameAsNotDirty(CurrentFrame);

        CurrentFrame = (CurrentFrame + 1) % Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT;

        return CurrentFrame;

        // SubmitInfo submitInfo = new()
        // {
        //     SType = StructureType.SubmitInfo,
        // };

        // PipelineStageFlags waitStages = PipelineStageFlags.ColorAttachmentOutputBit;

        // fixed (Semaphore* waitSemaphoresPtr = &vulkanEngine.imageAcquiredSemaphores[CurrentFrame])
        // fixed (CommandBuffer* commandBufferPtr = &vulkanEngine.commandBuffers[CurrentFrame])
        // fixed (Semaphore* renderCompleteSemaphoresPtr = &vulkanEngine.renderCompleteSemaphores[CurrentFrame])
        // {
        //     submitInfo.WaitSemaphoreCount = 1;
        //     submitInfo.PWaitSemaphores = waitSemaphoresPtr;

        //     submitInfo.PWaitDstStageMask = &waitStages;

        //     submitInfo.CommandBufferCount = 1;
        //     submitInfo.PCommandBuffers = commandBufferPtr;

        //     submitInfo.SignalSemaphoreCount = 1;
        //     submitInfo.PSignalSemaphores = renderCompleteSemaphoresPtr;

        //     if (Vulkan.CreateVulkan.vk.QueueSubmit(LogicalDevice.graphicsQueue, 1, &submitInfo, vulkanEngine.fences[CurrentFrame]) != Result.Success)
        //     {
        //         throw new Exception("Failed to submit command buffer!");
        //     }



        //     PresentInfoKHR presentInfo = new()
        //     {
        //         SType = StructureType.PresentInfoKhr,

        //         WaitSemaphoreCount = 1,
        //         PWaitSemaphores = renderCompleteSemaphoresPtr,

        //         SwapchainCount = 1,
        //         PSwapchains = swapChainPtr,
        //         PImageIndices = &imageIndex
        //     };

        //     _result = swapchain.khrSwapChain.QueuePresent(LogicalDevice.presentQueue, &presentInfo);

        //     if (_result == Result.ErrorOutOfDateKhr || _result == Result.SuboptimalKhr || framebufferResized)
        //     {
        //         framebufferResized = false;
        //         swapchain.RecreateSwapChain(GetFrameBufferSize, OnWindowMinimized);
        //         StylesManager.AddFlag(StylesManager.DirtyFlag.ScreenSize);
        //     }
        //     else if (_result != Result.Success)
        //     {
        //         throw new Exception("failed to present swap chain image!");
        //     }

        // }
        // diagnosticStopwatch.Stop();
        // if (CurrentFrame == 0)
        //     renderMicroseconds = diagnosticStopwatch.Elapsed.Microseconds;
        // else
        //     render2Microseconds = diagnosticStopwatch.Elapsed.Microseconds;

        // for (int i = 0; i < VUL; i++)
        // {

        // }
        // _frameCount++;
        // _frameCount++;
        // _fpsTimer += deltaTime;
        // _fpsTimer2 += deltaTime;

        // if (_fpsTimer >= 1.0) // every second
        // {
        //     lastSecondFps = _frameCount;
        //     _frameCount = 0;
        //     _fpsTimer = 0;
        // }

        // if (_fpsTimer2 >= 0.25)
        // {
        //     _fpsTimer2 = 0;
        //     window.Title = $"FPS: {lastSecondFps} update: {updateMicroseconds}μs render: {renderMicroseconds}μs render2: {render2Microseconds}μs";
        // }
    }

    //TODO - Rendering do zrobienia coding vibe o 2:45
    public unsafe uint Render()
    {
        SubmitInfo2 submitInfo = new()
        {
            SType = StructureType.SubmitInfo2,
            WaitSemaphoreInfoCount = 0,
        };

        CreateVulkan.vk.ResetCommandPool(LogicalDevice.device, vulkanEngine.commandPools[CurrentFrame], CommandPoolResetFlags.None);

        uint _currentFrame = GetCommandBuffer();
        subSiteCommandBuffers![0].CommandBuffer = commandBuffers[_currentFrame];

        if (subSiteRenderers != null)
        {
            int i = 1;
            foreach (var siteRenderer in subSiteRenderers)
            {
                // _renderSemaphores[i] = siteRenderer.vulkanEngine.renderCompleteSemaphores[siteRenderer.CurrentFrame];
                subSiteCommandBuffers[i].CommandBuffer = siteRenderer.commandBuffers[siteRenderer.GetCommandBuffer()];

                i++;
            }
        }


        submitInfo.CommandBufferInfoCount = (uint)subSiteCommandBuffers.Length;
        fixed (CommandBufferSubmitInfo* ptrCB = subSiteCommandBuffers)
            submitInfo.PCommandBufferInfos = ptrCB;

        SemaphoreSubmitInfo _signalSemaphoreInfo = new()
        {
            SType = StructureType.SemaphoreSubmitInfo,
            Semaphore = vulkanEngine.renderCompleteSemaphores[CurrentFrame],
        };

        submitInfo.SignalSemaphoreInfoCount = 1;
        submitInfo.PSignalSemaphoreInfos = &_signalSemaphoreInfo;

        if (Vulkan.CreateVulkan.vk.QueueSubmit2(LogicalDevice.graphicsQueue, 1, &submitInfo, default) != Result.Success)
        {
            throw new Exception("Failed to submit command buffer!");
        }

        return _currentFrame;
    }

    unsafe void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex)
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

                Image = imagesData[imageIndex].image,
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

                Image = depth.depthImage,
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
            ImageView = imagesData[imageIndex].imageView,
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
            ImageView = depth.depthImageView,
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
                Extent = screenExtent
            },
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &_colorAttachmentInfo,
            PDepthAttachment = &_depthAttachmentInfo
        };

        CreateVulkan.vk.CmdBeginRendering(commandBuffer, &_renderingInfo);

        Viewport _vp = new()
        {
            X = 0,
            Y = 0,
            Width = width,
            Height = height,
            MinDepth = 0,
            MaxDepth = 1,
        };
        CreateVulkan.vk.CmdSetViewport(commandBuffer, 0, 1, &_vp);
        Rect2D _scissors = new()
        {
            Offset = new(0, 0),
            Extent = screenExtent
        };
        CreateVulkan.vk.CmdSetScissor(commandBuffer, 0, 1, &_scissors);

        // foreach (var shader in loadedShaders)
        // {
        //     shader.Render(commandBuffer, CurrentFrame, wireFrameRendering);
        // }

        // coreManager.RenderShader(commandBuffer, CurrentFrame, wireFrameRendering);

        // if (swapchain.recreatedSwapChain)
        //     swapchain.recreatedSwapChain = false;

        Vulkan.CreateVulkan.vk.CmdEndRendering(commandBuffer);

        // ImageMemoryBarrier2 _barrierPresent = new()
        // {
        //     SType = StructureType.ImageMemoryBarrier2,
        //     SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
        //     SrcAccessMask = AccessFlags2.ColorAttachmentWriteBit,

        //     DstStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
        //     DstAccessMask = 0,

        //     OldLayout = ImageLayout.AttachmentOptimal,
        //     NewLayout = ImageLayout.PresentSrcKhr,

        //     Image = swapchain.swapChainImages[imageIndex],
        //     SubresourceRange = new()
        //     {
        //         AspectMask = ImageAspectFlags.ColorBit,
        //         LevelCount = 1,
        //         LayerCount = 1,
        //     },
        // };

        // DependencyInfo _barrierPresentDependencyInfo = new()
        // {
        //     SType = StructureType.DependencyInfo,
        //     ImageMemoryBarrierCount = 1,
        //     PImageMemoryBarriers = &_barrierPresent,
        // };

        // Vulkan.CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, ref _barrierPresentDependencyInfo);

        if (Vulkan.CreateVulkan.vk.EndCommandBuffer(commandBuffer) != Result.Success)
        {
            throw new Exception("Failed to record command buffer");
        }
    }

    void UpdateCameraUBO(uint currentImage)
    {
        if (!dirty.HasFlag(DirtyFlag.Size))
            return;
        dirty &= ~DirtyFlag.Size;

        Vulkan.UICameraUBO ubo = new()
        {
            Proj = Matrix4X4.CreateOrthographicOffCenter<float>(0, width, 0, height, -1000, 1000),
        };

        cameraBuffers.Update(currentImage, ubo.Proj);
    }

    #endregion

    #region Texture Renderer
    public override void Update(double deltaTime)
    {

    }

    public override (Image, Semaphore) GetImage()
    {
        uint _currentFrame = Render();
        return (imagesData[_currentFrame].image, vulkanEngine.renderCompleteSemaphores[_currentFrame]);
    }

    #endregion

    #region Sub renderer
    public void AddSubSiteRenderer(SiteRenderer siteRenderer)
    {

        siteRenderer.parentSiteRenderer = this;
        SiteRenderer _siteRendererWhichParentsAll = GetParentingSiteRenderer();
        _siteRendererWhichParentsAll.subSiteRenderers ??= new SiteRenderer[1];

        siteRenderer.CreateCommandBuffers(_siteRendererWhichParentsAll.vulkanEngine);

        var _subSiteRenderes = _siteRendererWhichParentsAll.subSiteRenderers;
        _siteRendererWhichParentsAll.subSiteRenderers = new SiteRenderer[_subSiteRenderes.Length + 1];
        Array.Copy(_subSiteRenderes, 0, _siteRendererWhichParentsAll.subSiteRenderers, 1, _subSiteRenderes.Length);
        _siteRendererWhichParentsAll.subSiteRenderers[0] = siteRenderer;
        _siteRendererWhichParentsAll.subSiteCommandBuffers = new CommandBufferSubmitInfo[_siteRendererWhichParentsAll.subSiteRenderers.Length + 1];
        Array.Fill(_siteRendererWhichParentsAll.subSiteCommandBuffers, new()
        {
            SType = StructureType.CommandBufferSubmitInfo
        });
    }

    SiteRenderer GetParentingSiteRenderer()
    {
        SiteRenderer _siteRendererWhichParentsAll = this;
        while (_siteRendererWhichParentsAll.parentSiteRenderer != null)
        {
            _siteRendererWhichParentsAll = _siteRendererWhichParentsAll.parentSiteRenderer;
        }
        return _siteRendererWhichParentsAll;
    }

    public void RemoveSubSiteRenderer(SiteRenderer siteRenderer)
    {
        SiteRenderer _siteRendererWhichParentsAll = GetParentingSiteRenderer();

        int _index = _siteRendererWhichParentsAll.subSiteRenderers.IndexOf(siteRenderer);

        var _subSiteRenderes = _siteRendererWhichParentsAll.subSiteRenderers;
        _siteRendererWhichParentsAll.subSiteRenderers = new SiteRenderer[_subSiteRenderes.Length - 1];

        Array.Copy(_subSiteRenderes, 0, _siteRendererWhichParentsAll.subSiteRenderers, 0, _index);
        Array.Copy(_subSiteRenderes, _index + 1, _siteRendererWhichParentsAll.subSiteRenderers, _index, _subSiteRenderes.Length - _index);
        _siteRendererWhichParentsAll.subSiteCommandBuffers = new CommandBufferSubmitInfo[_siteRendererWhichParentsAll.subSiteRenderers.Length + 1];
        Array.Fill(_siteRendererWhichParentsAll.subSiteCommandBuffers, new()
        {
            SType = StructureType.CommandBufferSubmitInfo
        });
    }
    #endregion

    public void Dispose()
    {
        cameraBuffers.Dispose();
        DisposeImages();
    }
}
