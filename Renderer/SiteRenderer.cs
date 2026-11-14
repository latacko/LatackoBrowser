using System;
using GraphicsCore.Styles;
using Vulkan;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Vulkan;
using VulkanManager;
using VulkanManager.BufferManager;
using VulkanManager.Structs;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using GraphicsCore;
using GraphicsCore.Buffers;
using InstanceFinderCore;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

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

    #region Vulkan elements
    readonly ImageData[] imagesData = new ImageData[VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    readonly Depth depth = new();

    DirtyFlag dirty;


    internal uint FrameInFlight { get; private set; } = 0;
    VulkanEngine? vulkanEngine;

    public CameraBuffers cameraBuffers = new();

    internal CommandBuffer[] commandBuffers = new CommandBuffer[VulkanEngine.MAX_FRAMES_IN_FLIGHT];

    #endregion

    #region Sub site renderers (e.g. iframes)
    SiteRenderer? parentSiteRenderer;
    SiteRenderer[]? subSiteRenderers;

    CommandBufferSubmitInfo[]? subSiteCommandBuffers;
    #endregion

    ISiteController siteController;

    Document document;

    NodeCreator nodeCreator;

    public bool wireframe;

    readonly List<VisualElement.ObjectComileInfo> visualElementsToCompile = [];


    public SiteRenderer(ISiteController siteController)
    {
        UniqueId = LastUniqueId++;
        this.siteController = siteController;
        if (RenderEngine.Instance == null)
            throw new System.NullReferenceException("The render engine hasn't been initialized");
        RenderEngine.Instance.RegisterSite(this);
        VisualElement.ObjectsToCompile.Add(UniqueId, visualElementsToCompile);

        nodeCreator = new(UniqueId);
        document = new(this, nodeCreator);

        siteController.SetDocument(document);
    }

    #region Setup
    public override void Init(uint width, uint height)
    {
        Resize(width, height);
        cameraBuffers.CreateBuffers();
        siteController.Start();

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
            ImageHelper.CreateImage(width, height, Swapchain.swapChainImageFormat, ImageTiling.Optimal, ImageUsageFlags.TransferSrcBit | ImageUsageFlags.SampledBit | ImageUsageFlags.ColorAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, 1, ref _imageData.image, ref _imageData.deviceMemory);
            _imageData.imageView = ImageHelper.CreateImageView(_imageData.image, Swapchain.swapChainImageFormat, ImageAspectFlags.ColorBit);
            imagesData[i] = _imageData;
        }
        depth.CreateDepthResources(width, height);
    }

    void DisposeImages()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            if (imagesData[i].image.Handle != 0)
                imagesData[i].Dispose();
        }
        if (depth.depthImage.Handle != 0)
        {
            Console.WriteLine("Dispose depth");
            depth.Dispose();
        }
    }

    unsafe void CreateCommandBuffers(VulkanEngine vulkanEngine)
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CommandBufferAllocateInfo commandBufferCI = new(
                commandPool: vulkanEngine.commandPools[i],
                commandBufferCount: 1
            );

            fixed (CommandBuffer* commandBufferPtr = &commandBuffers[i])
                CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, ref commandBufferCI, commandBufferPtr);
        }
    }

    public override unsafe void CreateVulkanEngine()
    {
        vulkanEngine = new();
        CreateCommandBuffers(vulkanEngine);
        subSiteCommandBuffers = new CommandBufferSubmitInfo[1];
        subSiteCommandBuffers[0] = new(
            commandBuffer: null
        );
    }

    public override void DestroyVulkanEngine()
    {
        vulkanEngine.Dispose();
    }

    public override VulkanEngine GetVulkanEngine() => vulkanEngine;

    public override uint GetId() => UniqueId;
    public override ulong GetCameraBufferDeviceAddress() => cameraBuffers.shaderDataBuffersForCamera[FrameInFlight].DeviceAddress;

    #endregion

    #region Rendering
    //TODO - Render to texture
    public unsafe void GetCommandBuffer(uint? frameInFlight)
    {
        using (SpeedscopeTrace.Begin("Camera ubo"))
        {
            if (frameInFlight.HasValue)
                FrameInFlight = frameInFlight.Value;
            UpdateCameraUBO(FrameInFlight);
        }

        using (SpeedscopeTrace.Begin("Compute Styles"))
        {
            stylesManager.ComputeStyles();
        }
        using (SpeedscopeTrace.Begin("Record command buffer"))
        {
            RecordCommandBuffer(commandBuffers[FrameInFlight], FrameInFlight);
        }
        using (SpeedscopeTrace.Begin("Set frame as not dirty"))
        {
            stylesManager.SetFrameAsNotDirty(FrameInFlight);
        }
    }

    //TODO - Rendering do zrobienia coding vibe o 2:45
    public unsafe void Render(uint frameInFlight)
    {
        using var _ = SpeedscopeTrace.Begin("Site Renderer Render");
        FrameInFlight = frameInFlight;
        SubmitInfo2 submitInfo = new(
            waitSemaphoreInfoCount: 0
        );

        CreateVulkan.vk.ResetCommandPool(LogicalDevice.device, vulkanEngine.commandPools[FrameInFlight], CommandPoolResetFlags.None);

        using (SpeedscopeTrace.Begin("Get Command Buffer"))
        {
            GetCommandBuffer(null);
        }
        subSiteCommandBuffers![0].CommandBuffer = commandBuffers[FrameInFlight];

        //REVIEW - Temponary disabled until normal rendering will work
        // using (SpeedscopeTrace.Begin("Get sub renderers"))
        // {
        //     if (subSiteRenderers != null)
        //     {
        //         int i = 1;
        //         foreach (var siteRenderer in subSiteRenderers)
        //         {
        //             // _renderSemaphores[i] = siteRenderer.vulkanEngine.renderCompleteSemaphores[siteRenderer.FrameInFlight];
        //             siteRenderer.GetCommandBuffer(FrameInFlight);
        //             subSiteCommandBuffers[i].CommandBuffer = siteRenderer.commandBuffers[FrameInFlight];

        //             i++;
        //         }
        //     }
        // }


        using (SpeedscopeTrace.Begin("Submit"))
        {
            submitInfo.CommandBufferInfoCount = (uint)subSiteCommandBuffers.Length;
            fixed (CommandBufferSubmitInfo* ptrCB = subSiteCommandBuffers)
                submitInfo.PCommandBufferInfos = ptrCB;

            SemaphoreSubmitInfo _signalSemaphoreInfo = new(
                semaphore: vulkanEngine.renderCompleteSemaphores[FrameInFlight]
            );

            submitInfo.SignalSemaphoreInfoCount = 1;
            submitInfo.PSignalSemaphoreInfos = &_signalSemaphoreInfo;

            var _result = Vulkan.CreateVulkan.vk.QueueSubmit2(LogicalDevice.GraphicsQueue, 1, &submitInfo, default);

            if (_result != Result.Success)
            {
                throw new Exception("Failed to submit command buffer! " + _result);
            }
        }
    }

    unsafe void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex)
    {
        using (SpeedscopeTrace.Begin("Begin command buffer"))
        {
            CommandBufferBeginInfo _beginInfo = new(
                flags: CommandBufferUsageFlags.SimultaneousUseBit
            );

            if (Vulkan.CreateVulkan.vk.BeginCommandBuffer(commandBuffer, &_beginInfo) != Result.Success)
            {
                throw new Exception("Failed to begin recording command buffer!");
            }
        }

        using (SpeedscopeTrace.Begin("Begin rendering"))
        {
            ImageMemoryBarrier2* _outputBarriers = stackalloc ImageMemoryBarrier2[2];
            _outputBarriers[0] = new(
                srcStageMask: PipelineStageFlags2.ColorAttachmentOutputBit,
                srcAccessMask: 0,

                dstStageMask: PipelineStageFlags2.ColorAttachmentOutputBit,
                dstAccessMask: AccessFlags2.ColorAttachmentReadBit | AccessFlags2.ColorAttachmentWriteBit,

                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.AttachmentOptimal,

                image: imagesData[imageIndex].image,
                subresourceRange: new(
                    aspectMask: ImageAspectFlags.ColorBit,
                    levelCount: 1,
                    layerCount: 1
                )
            );
            _outputBarriers[1] = new(
                srcStageMask: PipelineStageFlags2.LateFragmentTestsBit,
                srcAccessMask: AccessFlags2.DepthStencilAttachmentWriteBit,

                dstStageMask: PipelineStageFlags2.EarlyFragmentTestsBit,
                dstAccessMask: AccessFlags2.DepthStencilAttachmentWriteBit,

                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.AttachmentOptimal,

                image: depth.depthImage,
                subresourceRange: new(
                    aspectMask: ImageAspectFlags.DepthBit,
                    levelCount: 1,
                    layerCount: 1
                )
            );

            DependencyInfo _barrierDependencyInfo = new(
                imageMemoryBarrierCount: 2,
                pImageMemoryBarriers: _outputBarriers
            );

            CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, ref _barrierDependencyInfo);

            RenderingAttachmentInfo _colorAttachmentInfo = new(
                imageView: imagesData[imageIndex].imageView,
                imageLayout: ImageLayout.AttachmentOptimal,
                loadOp: AttachmentLoadOp.Clear,
                storeOp: AttachmentStoreOp.Store,
                clearValue: new(
                    color: new(1, 0, 0, 1)
                )
            );

            RenderingAttachmentInfo _depthAttachmentInfo = new(
                imageView: depth.depthImageView,
                imageLayout: ImageLayout.AttachmentOptimal,
                loadOp: AttachmentLoadOp.Clear,
                storeOp: AttachmentStoreOp.DontCare,
                clearValue: new(
                    depthStencil: new(1, 0)
                )
            );

            RenderingInfo _renderingInfo = new(
                renderArea: new(
                    offset: new(0, 0),
                    extent: screenExtent
                ),
                layerCount: 1,
                colorAttachmentCount: 1,
                pColorAttachments: &_colorAttachmentInfo,
                pDepthAttachment: &_depthAttachmentInfo
            );

            CreateVulkan.vk.CmdBeginRendering(commandBuffer, &_renderingInfo);
        }

        using (SpeedscopeTrace.Begin("Viewport & scissors"))
        {
            Viewport _vp = new(
                x: 0,
                y: 0,
                width: width,
                height: height,
                minDepth: 0,
                maxDepth: 1
            );
            CreateVulkan.vk.CmdSetViewport(commandBuffer, 0, 1, &_vp);
            Rect2D _scissors = new(
                offset: new(0, 0),
                extent: screenExtent
            );
            CreateVulkan.vk.CmdSetScissor(commandBuffer, 0, 1, &_scissors);
        }

        if (visualElementsToCompile.Count > 0)
        {
            Console.WriteLine("Elements to compile in shaders: " + VisualElement.ObjectsToCompile.Count);
            CompileVisuaElements();
        }

        foreach (var shader in RenderEngine.Instance.Shaders)
        {
            shader.Render(this, commandBuffer, imageIndex, wireframe);
        }

        // coreManager.RenderShader(commandBuffer, FrameInFlight, wireFrameRendering);

        // if (swapchain.recreatedSwapChain)
        //     swapchain.recreatedSwapChain = false;
        using (SpeedscopeTrace.Begin("End rendering"))
        {
            Vulkan.CreateVulkan.vk.CmdEndRendering(commandBuffer);

            ImageMemoryBarrier2 _barrierPresent = new(
                srcStageMask: PipelineStageFlags2.ColorAttachmentOutputBit,
                srcAccessMask: AccessFlags2.ColorAttachmentWriteBit,

                dstStageMask: PipelineStageFlags2.ColorAttachmentOutputBit,
                dstAccessMask: 0,

                oldLayout: ImageLayout.AttachmentOptimal,
                newLayout: ImageLayout.PresentSrcKhr,

                image: imagesData[imageIndex].image,
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
    }

    void CompileVisuaElements()
    {
        int _objectToCompileCount = visualElementsToCompile.Count >= 10 ? 10 : visualElementsToCompile.Count;
        for (int i = 0; i < _objectToCompileCount; i++)
        {
            var _objectInfo = visualElementsToCompile[i];
            _objectInfo.shader.CompileElement(this, _objectInfo.runtimeModel);
        }
        visualElementsToCompile.RemoveRange(0, _objectToCompileCount);
    }

    void UpdateCameraUBO(uint currentImage)
    {
        using (SpeedscopeTrace.Begin("Clear flag"))
        {
            // if (!dirty.HasFlag(DirtyFlag.Size))
            //     return;
        }
        dirty &= ~DirtyFlag.Size;

        using (SpeedscopeTrace.Begin("Updating camera ubo"))
        {
            Vulkan.UICameraUBO ubo = new()
            {
                Proj = Matrix4X4.CreateOrthographicOffCenter<float>(0, width, 0, height, -1000, 1000),
            };

            // Console.WriteLine("Proj width: " + width + "x" + height);
            // Console.WriteLine("Proj ubo: " + ubo.Proj);
            cameraBuffers.Update(currentImage, ubo);
        }
    }

    #endregion

    #region Texture Renderer
    public override void Update(double deltaTime)
    {
        siteController.Update();
    }

    public override (Image, Semaphore) GetImage(uint frameInFlight)
    {
        Render(frameInFlight);
        return (imagesData[frameInFlight].image, vulkanEngine.renderCompleteSemaphores[frameInFlight]);
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
        unsafe
        {
            Array.Fill(_siteRendererWhichParentsAll.subSiteCommandBuffers, new(
                commandBuffer: null
            ));
        }
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

        unsafe
        {
            Array.Fill(_siteRendererWhichParentsAll.subSiteCommandBuffers, new(
                commandBuffer: null
            ));
        }
    }
    #endregion

    public void Dispose()
    {
        foreach (var item in RenderEngine.Instance.TextShaders)
        {
            item.UnregisterBufferForSite(GetId());
        }
        cameraBuffers.Dispose();
        DisposeImages();
    }
}
