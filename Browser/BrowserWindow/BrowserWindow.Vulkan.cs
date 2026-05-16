using System.Runtime.Serialization;
using Browser.DataTypes;
using Silk.NET.Assimp;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Units;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Browser;

public unsafe partial class BrowserWindow
{
    bool framebufferResized;
    Vulkan.CreateVulkan createVulkan = new();
    Vulkan.VulkanManager vulkanManager = new();
    Vulkan.Swapchain swapchain;

    void CreateVulkan()
    {
        createVulkan.Create(window.VkSurface.GetRequiredExtensions(out uint count), count);
        vulkanManager.Init();
        swapchain = new(window.FramebufferSize);

        OnStart?.Invoke();
    }

    #region Instance creation
    private void CreateInstance()
    {
        
    }

    private InstanceCreateInfo SetRequiredExtensions(InstanceCreateInfo createInfo)
    {
        createInfo.PpEnabledExtensionNames = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        createInfo.EnabledExtensionCount = (uint)glfwExtensionCount;

        return createInfo;
    }



    string[] GetRequiredExtensions()
    {
        var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

        if (enableValidationLayers)
        {
            return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
        }

        return extensions;
    }
    #endregion

    #region Debug Manager
    private DebugUtilsMessengerCreateInfoEXT PopulateDebugMessengerCreateInfo()
    {
        return new DebugUtilsMessengerCreateInfoEXT()
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
            MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
            PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback,
            PUserData = (void*)IntPtr.Zero
        };
    }
    private void SetUpDebugMessenger()
    {
        if (!enableValidationLayers) return;

        if (!vk!.TryGetInstanceExtension(vulkanInstance, out debugUtils)) throw new System.Exception("failed to get debug utils");

        var _createInfo = PopulateDebugMessengerCreateInfo();

        if (debugUtils!.CreateDebugUtilsMessenger(vulkanInstance, in _createInfo, null, out debugMessenger) != Result.Success)
        {
            throw new Exception("failed to set up debug messenger!");
        }
    }

    private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        if (messageSeverity < DebugUtilsMessageSeverityFlagsEXT.WarningBitExt)
        {
            return Vk.False;
        }
        Console.WriteLine($"validation layer:" + SilkMarshal.PtrToString((nint)pCallbackData->PMessage));

        return Vk.False;
    }

    #endregion

    #region Surface

    void CreateSurface()
    {
        if (!vk!.TryGetInstanceExtension(vulkanInstance, out khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        surface = window!.VkSurface!.Create<AllocationCallbacks>(vulkanInstance.ToHandle(), null).ToSurface();
    }

    #endregion

    #region Physical Device
    
    #endregion

    #region Logical Device
    
    #endregion

    #region Swap Chain
    SwapChainSupportDetails QuerySwapChainSupport(Silk.NET.Vulkan.PhysicalDevice physicalDevice)
    {
        SwapChainSupportDetails _details = new();

        khrSurface!.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out _details.Capabilities);

        uint _formatCount = 0;
        khrSurface!.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &_formatCount, null);

        if (_formatCount != 0)
        {
            _details.Formats = new SurfaceFormatKHR[_formatCount];
            fixed (SurfaceFormatKHR* formatsPtr = _details.Formats)
            {
                khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &_formatCount, formatsPtr);
            }
        }

        uint _presentModeCount = 0;
        khrSurface!.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, &_presentModeCount, null);

        if (_presentModeCount != 0)
        {
            _details.PresentModes = new PresentModeKHR[_presentModeCount];
            fixed (PresentModeKHR* modesPtr = _details.PresentModes)
            {
                khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, &_presentModeCount, modesPtr);
            }
        }

        return _details;
    }

    SurfaceFormatKHR ChooseSwapSurfaceFormat(SurfaceFormatKHR[] availableFormats)
    {
        foreach (var availableFormat in availableFormats)
        {
            if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }

    PresentModeKHR ChooseSwapPresentMode(PresentModeKHR[] availablePresentModes)
    {
        foreach (var presentMode in availablePresentModes)
        {
            if (presentMode == PresentModeKHR.MailboxKhr)
            {
                return presentMode;
            }
        }
        return PresentModeKHR.FifoKhr;
    }

    Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }
        else
        {
            var framebufferSize = window!.FramebufferSize;

            Extent2D actualExtent = new()
            {
                Width = (uint)framebufferSize.X,
                Height = (uint)framebufferSize.Y
            };

            actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
            actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

            return actualExtent;
        }
    }

    void CreateSwapChain()
    {
        SwapChainSupportDetails _swapChainSupport = QuerySwapChainSupport(physicalDevice);

        SurfaceFormatKHR _surfaceFormat = ChooseSwapSurfaceFormat(_swapChainSupport.Formats);
        PresentModeKHR _presentMode = ChooseSwapPresentMode(_swapChainSupport.PresentModes);
        Extent2D _extent = ChooseSwapExtent(_swapChainSupport.Capabilities);

        uint _imageCount = _swapChainSupport.Capabilities.MinImageCount;
        if (_swapChainSupport.Capabilities.MaxImageCount > 0 && _imageCount > _swapChainSupport.Capabilities.MaxImageCount)
            _imageCount = _swapChainSupport.Capabilities.MaxImageCount;

        SwapchainCreateInfoKHR _swapchainCreateInfoKHR = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = surface,

            MinImageCount = _imageCount,
            ImageFormat = _surfaceFormat.Format,
            ImageColorSpace = _surfaceFormat.ColorSpace,
            ImageExtent = _extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,

            PreTransform = _swapChainSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,

            PresentMode = _presentMode,
            Clipped = Vk.True,

            OldSwapchain = default,
        };

        QueueFamilyIndices indices = FindQueueFamilies(physicalDevice);
        uint[] _queueFamilyIndices = [indices.GraphicsFamily!.Value, indices.PresentFamily!.Value];

        fixed (uint* queueFamilyIndicesPtr = _queueFamilyIndices)

            if (indices.GraphicsFamily != indices.PresentFamily)
            {
                _swapchainCreateInfoKHR.ImageSharingMode = SharingMode.Concurrent;
                _swapchainCreateInfoKHR.QueueFamilyIndexCount = 2;
                _swapchainCreateInfoKHR.PQueueFamilyIndices = queueFamilyIndicesPtr;
            }
            else
            {
                _swapchainCreateInfoKHR.ImageSharingMode = SharingMode.Exclusive;
                _swapchainCreateInfoKHR.QueueFamilyIndexCount = 0; // Optional
                _swapchainCreateInfoKHR.PQueueFamilyIndices = null; // Optional
            }

        if (!vk!.TryGetDeviceExtension(vulkanInstance, device, out khrSwapChain))
        {
            throw new NotSupportedException("VK_KHR_swapchain extension not found.");
        }

        if (khrSwapChain!.CreateSwapchain(device, &_swapchainCreateInfoKHR, null, out swapChain) != Result.Success)
        {
            throw new Exception("Failed to create swap chain!");
        }

        khrSwapChain.GetSwapchainImages(device, swapChain, &_imageCount, null);
        swapChainImages = new Image[_imageCount];
        fixed (Image* imagesPtr = swapChainImages)
            khrSwapChain.GetSwapchainImages(device, swapChain, &_imageCount, imagesPtr);

        swapChainImageFormat = _surfaceFormat.Format;
        swapChainExtent = _extent;
        UnitsConverter.Update(swapChainExtent.Width, swapChainExtent.Height);
    }

    void RecreateSwapChain()
    {
        Vector2D<int> framebufferSize = window!.FramebufferSize;

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = window.FramebufferSize;
            window.DoEvents();
        }

        vk.DeviceWaitIdle(device);

        CleanUpSwapChain();

        CreateSwapChain();

        recreatedSwapChain = true;

        CreateImageViews();
        CreateDepthResources();
        CreateFrameBuffers();
    }

    void CleanUpSwapChain()
    {
        vk.DestroyImageView(device, depthImageView, null);
        vk.DestroyImage(device, depthImage, null);
        vk.FreeMemory(device, depthImageMemory, null);

        foreach (var framebuffer in swapChainFrameBuffers)
        {
            vk.DestroyFramebuffer(device, framebuffer, null);
        }

        foreach (var imageView in swapChainImageViews)
        {
            vk.DestroyImageView(device, imageView, null);
        }

        khrSwapChain?.DestroySwapchain(device, swapChain, null);
    }

    #endregion

    #region Image Views
    void CreateImageViews()
    {
        swapChainImageViews = new ImageView[swapChainImages.Length];

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            swapChainImageViews[i] = CreateImageView(swapChainImages[i], swapChainImageFormat);
        }
    }
    #endregion

    #region Render pass
    void CreateRenderPass()
    {
        AttachmentDescription _colorAttachment = new()
        {
            Format = swapChainImageFormat,
            Samples = SampleCountFlags.Count1Bit,

            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,

            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,

            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr,
        };

        AttachmentReference _colorAttachmentRef = new()
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        AttachmentDescription _depthAttachment = new()
        {
            Format = FindDepthFormat(),
            Samples = SampleCountFlags.Count1Bit,

            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,

            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
        };

        AttachmentReference _depthAttachmentRef = new()
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal,
        };

        SubpassDescription _subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,

            ColorAttachmentCount = 1,
            PColorAttachments = &_colorAttachmentRef,
            PDepthStencilAttachment = &_depthAttachmentRef
        };

        SubpassDependency _depedency = new()
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,

            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.LateFragmentTestsBit,
            SrcAccessMask = AccessFlags.DepthStencilAttachmentWriteBit,

            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.LateFragmentTestsBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit,
        };

        AttachmentDescription[] _attachments = [_colorAttachment, _depthAttachment];

        fixed (AttachmentDescription* attachmentsPtr = _attachments)
        {
            RenderPassCreateInfo _renderPassInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,

                AttachmentCount = (uint)_attachments.Length,
                PAttachments = attachmentsPtr,

                SubpassCount = 1,
                PSubpasses = &_subpass,

                DependencyCount = 1,
                PDependencies = &_depedency,
            };

            if (vk.CreateRenderPass(device, &_renderPassInfo, null, out renderPass) != Result.Success)
            {
                throw new Exception("Failed to create render pass!");
            }

        }
    }
    #endregion

    #region Descriptor set layout

    void CreateDescriptorSetLayout()
    {
        GlobalDescriptorPool.Init(1);

        cameraBuffer.Init();
        foreach (var shader in loadedShader)
        {
            shader.Init();
            shader.CreatePipeline(renderPass);
        }
    }

    #endregion

    #region Frame buffers
    void CreateFrameBuffers()
    {
        swapChainFrameBuffers = new Framebuffer[swapChainImageViews.Length];

        for (int i = 0; i < swapChainImageViews.Length; i++)
        {
            ImageView[] attachments = [
                swapChainImageViews[i],
                depthImageView
            ];

            fixed (Framebuffer* framePtr = &swapChainFrameBuffers[i])
            fixed (ImageView* attachmentPtr = attachments)
            {
                FramebufferCreateInfo frameBufferInfo = new()
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = renderPass,
                    AttachmentCount = (uint)attachments.Length,
                    PAttachments = attachmentPtr,
                    Width = swapChainExtent.Width,
                    Height = swapChainExtent.Height,
                    Layers = 1,
                };

                if (vk.CreateFramebuffer(device, &frameBufferInfo, null, framePtr) != Result.Success)
                {
                    throw new Exception("Failed to create framebuffer!");
                }
            }

        }
    }
    #endregion

    #region Command pool

    void CreateCommandPool()
    {
        var queueFamilyIndices = FindQueueFamilies(physicalDevice);

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value
        };

        if (vk.CreateCommandPool(device, &poolInfo, null, out commandPool) != Result.Success)
        {
            throw new Exception("Failed to create command pool!");
        }
    }

    #endregion

    #region Depth

    

    #endregion

    #region Create texture

    

    void CreateImage(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ref Image image, ref DeviceMemory imageMemory)
    {
        ImageCreateInfo _imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,

            ImageType = ImageType.Type2D,
            Extent = new()
            {
                Width = width,
                Height = height,
                Depth = 1,
            },
            MipLevels = 1,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive
        };

        if (vk.CreateImage(device, &_imageInfo, null, out image) != Result.Success)
        {
            throw new Exception("Failed to create image!");
        }

        MemoryRequirements _memRequirements;
        vk.GetImageMemoryRequirements(device, image, &_memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,

            AllocationSize = _memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(_memRequirements.MemoryTypeBits, properties)
        };

        if (vk.AllocateMemory(device, &allocInfo, null, out imageMemory) != Result.Success)
        {
            throw new Exception("Failed to allocate image memory");
        }

        vk.BindImageMemory(device, image, imageMemory, 0);
    }

    void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        ImageMemoryBarrier _barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,

            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,

            Image = image,
            SubresourceRange = new()
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },

            SrcAccessMask = 0, //? TODO
            DstAccessMask = 0, //? TODO
        };

        PipelineStageFlags _sourceStage;
        PipelineStageFlags _destinationStage;

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            _barrier.SrcAccessMask = 0;
            _barrier.DstAccessMask = AccessFlags.TransferWriteBit;

            _sourceStage = PipelineStageFlags.TopOfPipeBit;
            _destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            _barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            _barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            _sourceStage = PipelineStageFlags.TransferBit;
            _destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else
        {
            throw new Exception("Unsupported layout transition!");
        }

        vk.CmdPipelineBarrier(commandBuffer,
        _sourceStage, _destinationStage,
        0,
        0, null,
        0, null,
        1, &_barrier
        );

        EndSingleTimeCommands(commandBuffer);
    }

    void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        BufferImageCopy region = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,

            ImageSubresource = new()
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },

            ImageOffset = new(0, 0, 0),
            ImageExtent = new()
            {
                Width = width,
                Height = height,
                Depth = 1,
            },
        };

        vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, &region);

        EndSingleTimeCommands(commandBuffer);
    }

    #endregion

    #region Create texture view

    public ImageView CreateTextureImageView(Image textureImage)
    {
        return CreateImageView(textureImage, Format.R8G8B8A8Srgb);
    }

    ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags = ImageAspectFlags.ColorBit)
    {
        ImageViewCreateInfo _viewInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,

            ViewType = ImageViewType.Type2D,
            Format = format,

            SubresourceRange = new()
            {
                AspectMask = aspectFlags,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        if (vk.CreateImageView(device, &_viewInfo, null, out var imageView) != Result.Success)
        {
            throw new Exception("Failed to create image view!");
        }

        return imageView;
    }

    #endregion

    #region Create texture sampler

    void CreateTextureSampler()
    {
        SamplerCreateInfo _samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,

            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,

            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,

            AnisotropyEnable = Vk.True,

            BorderColor = BorderColor.IntOpaqueBlack,

            UnnormalizedCoordinates = Vk.False,

            CompareEnable = Vk.False,
            CompareOp = CompareOp.Always,

            MipmapMode = SamplerMipmapMode.Linear,
            MipLodBias = 0,
            MinLod = 0,
            MaxLod = 0,
        };

        vk.GetPhysicalDeviceProperties(physicalDevice, out var properties);
        _samplerInfo.MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy;

        if (vk.CreateSampler(device, &_samplerInfo, null, out textureSampler) != Result.Success)
        {
            throw new Exception("Failed to create texture sampler!");
        }
    }

    #endregion

    #region Vertex buffer

    uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        vk.GetPhysicalDeviceMemoryProperties(physicalDevice, out var memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }
        throw new Exception("Failed to find suitable memory type!");
    }

    void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Buffer buffer, ref DeviceMemory bufferMemory)
    {
        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,

            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        if (vk.CreateBuffer(device, &bufferInfo, null, out buffer) != Result.Success)
        {
            throw new Exception("Failed to create vertex buffer!");
        }

        MemoryRequirements memRequirements;
        vk.GetBufferMemoryRequirements(device, buffer, &memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        if (vk.AllocateMemory(device, &allocInfo, null, out bufferMemory) != Result.Success)
        {
            throw new Exception("Failed to allocate vertex buffer memory!");
        }

        vk.BindBufferMemory(device, buffer, bufferMemory, 0);
    }

    void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        CommandBuffer _commandBuffer = BeginSingleTimeCommands();

        BufferCopy copyRegion = new()
        {
            SrcOffset = 0,
            DstOffset = 0,
            Size = size,
        };

        vk.CmdCopyBuffer(_commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);

        EndSingleTimeCommands(_commandBuffer);
    }

    #endregion

    #region Index buffer

    internal void CreateIndexBuffer(uint[] indices, ref Buffer indexBuffer, ref DeviceMemory indexBufferMemory)
    {
        ulong _bufferSize = (ulong)(sizeof(uint) * indices.Length);
        Buffer _stagingBuffer = new();
        DeviceMemory _stagingBufferMemory = new();

        CreateBuffer(_bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref _stagingBuffer, ref _stagingBufferMemory);

        void* data;
        vk.MapMemory(device, _stagingBufferMemory, 0, _bufferSize, 0, &data);
        indices.CopyTo(new Span<uint>(data, indices.Length));
        vk.UnmapMemory(device, _stagingBufferMemory);

        CreateBuffer(_bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref indexBuffer, ref indexBufferMemory);
        CopyBuffer(_stagingBuffer, indexBuffer, _bufferSize);

        vk.DestroyBuffer(device, _stagingBuffer, null);
        vk.FreeMemory(device, _stagingBufferMemory, null);
    }

    #endregion

    #region Create model

    void CreateModel()
    {
        runtimeModelData = ObjectsManager.AddObject(loadedShader[0], primitiveModelsDb.Get(PrimitiveUIModel.Quad));
        runtimeModelData.SetSize(new (300,200));
        runtimeModelData.SetPosition(new (20,20));

        // using var assimp = Assimp.GetApi();
        // var scene = assimp.ImportFile("models/viking_room.obj", (uint)PostProcessPreset.TargetRealTimeMaximumQuality);

        // var vertexMap = new Dictionary<Vertex, uint>();
        // var vertices = new List<Vertex>();
        // var indices = new List<uint>();

        // VisitSceneNode(scene->MRootNode);

        // assimp.ReleaseImport(scene);


        // loadedModel = new(vertices.ToArray(), indices.ToArray(), "textures/viking_room.png");

        // void VisitSceneNode(Node* node)
        // {
        //     for (int m = 0; m < node->MNumMeshes; m++)
        //     {
        //         var mesh = scene->MMeshes[node->MMeshes[m]];

        //         for (int f = 0; f < mesh->MNumFaces; f++)
        //         {
        //             var face = mesh->MFaces[f];

        //             for (int i = 0; i < face.MNumIndices; i++)
        //             {
        //                 uint index = face.MIndices[i];

        //                 var position = mesh->MVertices[index];
        //                 var texture = mesh->MTextureCoords[0][(int)index];

        //                 Vertex vertex = new()
        //                 {
        //                     Pos = new Vector3D<float>(position.X, position.Y, position.Z),
        //                     //Flip Y for OBJ in Vulkan
        //                     TextCoord = new Vector2D<float>(texture.X, 1.0f - texture.Y)
        //                 };

        //                 if (vertexMap.TryGetValue(vertex, out var meshIndex))
        //                 {
        //                     indices.Add(meshIndex);
        //                 }
        //                 else
        //                 {
        //                     indices.Add((uint)vertices.Count);
        //                     vertexMap[vertex] = (uint)vertices.Count;
        //                     vertices.Add(vertex);
        //                 }
        //             }
        //         }
        //     }

        //     for (int c = 0; c < node->MNumChildren; c++)
        //     {
        //         VisitSceneNode(node->MChildren[c]);
        //     }
        // }


    }

    #endregion

    #region Command buffer

    void CreateCommandBuffers()
    {
        commandBuffers = new CommandBuffer[MAX_FRAMES_IN_FLIGHT];

        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,

            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = MAX_FRAMES_IN_FLIGHT,
        };

        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
        {
            if (vk.AllocateCommandBuffers(device, &allocateInfo, commandBuffersPtr) != Result.Success)
            {
                throw new Exception("Failed to allocate command buffers!");
            }
        }
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

        RenderPassBeginInfo _renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = renderPass,
            Framebuffer = swapChainFrameBuffers[imageIndex],

            RenderArea = new()
            {
                Offset = new(0, 0),
                Extent = swapChainExtent
            },
        };

        ClearValue[] _clearValues = [
            new(new(0, 0, 0, 1)),
            new(null, new(1,0)),
        ];
        fixed (ClearValue* clearValuesPtr = _clearValues)
        {
            _renderPassInfo.ClearValueCount = (uint)_clearValues.Length;
            _renderPassInfo.PClearValues = clearValuesPtr;

            vk.CmdBeginRenderPass(commandBuffer, &_renderPassInfo, SubpassContents.Inline);
        }

        Viewport _viewport = new()
        {
            X = 0,
            Y = 0,
            Width = swapChainExtent.Width,
            Height = swapChainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };

        vk.CmdSetViewport(commandBuffer, 0, 1, &_viewport);

        Rect2D _scissor = new()
        {
            Offset = new(0, 0),
            Extent = swapChainExtent
        };
        vk.CmdSetScissor(commandBuffer, 0, 1, &_scissor);

        var quadData = primitiveModelsDb.Get(PrimitiveUIModel.Quad);

        foreach (var shader in loadedShader)
        {
            shader.Render(commandBuffer, currentFrame, null);
        }

        if (recreatedSwapChain)
            recreatedSwapChain=false;


        // #region Render jednego modelu
        // Buffer[] _vertexBuffers = [quadData.vertexBuffer];
        // ulong[] _offsets = [0];

        // fixed (ulong* offsetsPtr = _offsets)
        // fixed (Buffer* vertexBuffersPtr = _vertexBuffers)
        // {
        //     vk!.CmdBindVertexBuffers(commandBuffer, 0, 1, ref quadData.vertexBuffer, offsetsPtr);
        // }

        // vk!.CmdBindIndexBuffer(commandBuffer, quadData.indexBuffer, 0, IndexType.Uint32);
        // fixed (DescriptorSet* descriptorSetsPtr = descriptorSets)
        //     vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, loadedShader[0].PipelineLayout, 0, 1, descriptorSetsPtr, 0, null);
        // vk.CmdDrawIndexed(commandBuffer, (uint)quadData.Indices.Length, 1, 0, 0, 0);
        // #endregion
        vk.CmdEndRenderPass(commandBuffer);

        if (vk.EndCommandBuffer(commandBuffer) != Result.Success)
        {
            throw new Exception("Failed to record command buffer");
        }
    }

    CommandBuffer BeginSingleTimeCommands()
    {
        CommandBufferAllocateInfo _allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = 1,
        };

        vk.AllocateCommandBuffers(device, &_allocInfo, out var _commandBuffer);

        CommandBufferBeginInfo _beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        vk.BeginCommandBuffer(_commandBuffer, &_beginInfo);

        return _commandBuffer;
    }

    void EndSingleTimeCommands(CommandBuffer commandBuffer)
    {
        vk.EndCommandBuffer(commandBuffer);

        SubmitInfo _submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        vk.QueueSubmit(graphicsQueue, 1, &_submitInfo, default);
        vk.QueueWaitIdle(graphicsQueue);

        vk.FreeCommandBuffers(device, commandPool, 1, &commandBuffer);
    }

    #endregion

    #region Sync objects

    void CreateSyncObjects()
    {
        imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];

        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit
        };

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            if (vk.CreateSemaphore(device, &semaphoreInfo, null, out imageAvailableSemaphores[i]) != Result.Success ||
                vk.CreateSemaphore(device, &semaphoreInfo, null, out renderFinishedSemaphores[i]) != Result.Success ||
                vk.CreateFence(device, &fenceInfo, null, out inFlightFences[i]) != Result.Success)
            {
                throw new Exception("failed to create synchronization objects for a frame!");
            }
        }
    }

    #endregion

    public void DestroyBuffer(Buffer buffer, DeviceMemory memory)
    {
        vk.DestroyBuffer(device, buffer, null);
        vk.FreeMemory(device, memory, null);
    }

    public void DestroyTexture(Image textureImage, DeviceMemory textureImageMemory, ImageView textureImageView)
    {
        vk.DestroyImageView(device, textureImageView, null);
        vk.DestroyImage(device, textureImage, null);
        vk.FreeMemory(device, textureImageMemory, null);
    }

    private void CleanUpVulcan()
    {
        CleanUpSwapChain();

        vk.DestroySampler(device, textureSampler, null);

        // runtimeModelData.Dispose();

        foreach (var shader in loadedShader)
        {
            shader.Dispose();
        }

        primitiveModelsDb.Dispose();

        cameraBuffer.Dispose();

        GlobalDescriptorPool.Dispose();

        // vk.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);

        vk.DestroyRenderPass(device, renderPass, null);

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            vk.DestroySemaphore(device, imageAvailableSemaphores[i], null);
            vk.DestroySemaphore(device, renderFinishedSemaphores[i], null);
            vk.DestroyFence(device, inFlightFences[i], null);
        }

        vk.DestroyCommandPool(device, commandPool, null);

        vk.DestroyDevice(device, null);

        if (enableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugUtils!.DestroyDebugUtilsMessenger(vulkanInstance, debugMessenger, null);
        }

        khrSurface.DestroySurface(vulkanInstance, surface, null);
        vk.DestroyInstance(vulkanInstance, null);
    }
}