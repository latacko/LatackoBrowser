using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Units;
namespace Vulkan;

public unsafe class Swapchain
{
    public static Swapchain Instance;
    KhrSurface khrSurface;
    SurfaceKHR surface;
    Vector2D<int> framebufferSize;

    internal KhrSwapchain? khrSwapChain;
    internal SwapchainKHR swapChain;
    internal Extent2D swapChainExtent;

    internal Image[] swapChainImages;
    internal static Format swapChainImageFormat;
    internal static bool recreatedSwapChain;

    ImageView[] swapChainImageViews;
    // Framebuffer[] swapChainFrameBuffers;

    Depth depth = new();


    public Swapchain(Vector2D<int> framebufferSize)
    {
        Instance = this;
        this.framebufferSize = framebufferSize;
    }

    internal SwapChainSupportDetails QuerySwapChainSupport(Silk.NET.Vulkan.PhysicalDevice physicalDevice)
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
        SwapChainSupportDetails _swapChainSupport = QuerySwapChainSupport(PhysicalDevice.physicalDevice);

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

        QueueFamilyIndices indices = PhysicalDevice.Instance.FindQueueFamilies(PhysicalDevice.physicalDevice);
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

        if (CreateVulkan.vk!.TryGetDeviceExtension(CreateVulkan.vulkanInstance, LogicalDevice.device, out khrSwapChain))
        {
            throw new NotSupportedException("VK_KHR_swapchain extension not found.");
        }

        if (khrSwapChain!.CreateSwapchain(LogicalDevice.device, &_swapchainCreateInfoKHR, null, out swapChain) != Result.Success)
        {
            throw new Exception("Failed to create swap chain!");
        }

        khrSwapChain.GetSwapchainImages(LogicalDevice.device, swapChain, &_imageCount, null);
        swapChainImages = new Image[_imageCount];
        fixed (Image* imagesPtr = swapChainImages)
            khrSwapChain.GetSwapchainImages(LogicalDevice.device, swapChain, &_imageCount, imagesPtr);

        swapChainImageFormat = _surfaceFormat.Format;
        swapChainExtent = _extent;
        UnitsConverter.Update(swapChainExtent.Width, swapChainExtent.Height);
    }

    void RecreateSwapChain(Func<Vector2D<int>> GetFrameBufferSize, Action? OnMinimized = null)
    {
        this.framebufferSize = GetFrameBufferSize();

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = GetFrameBufferSize();
            OnMinimized?.Invoke();
        }

        CreateVulkan.vk.DeviceWaitIdle(LogicalDevice.device);

        CleanUpSwapChain();

        CreateSwapChain();

        recreatedSwapChain = true;

        CreateImageViews();
        depth.CreateDepthResources(swapChainExtent.Width, swapChainExtent.Height);
        // CreateFrameBuffers();
    }

    void CreateImageViews()
    {
        swapChainImageViews = new ImageView[swapChainImages.Length];

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            swapChainImageViews[i] = ImageHelper.CreateImageView(swapChainImages[i], swapChainImageFormat);
        }
    }

    // void CreateFrameBuffers()
    // {
    //     swapChainFrameBuffers = new Framebuffer[swapChainImageViews.Length];

    //     for (int i = 0; i < swapChainImageViews.Length; i++)
    //     {
    //         ImageView[] attachments = [
    //             swapChainImageViews[i],
    //             depth.depthImageView
    //         ];

    //         fixed (Framebuffer* framePtr = &swapChainFrameBuffers[i])
    //         fixed (ImageView* attachmentPtr = attachments)
    //         {
    //             FramebufferCreateInfo frameBufferInfo = new()
    //             {
    //                 SType = StructureType.FramebufferCreateInfo,
    //                 RenderPass = renderPass,
    //                 AttachmentCount = (uint)attachments.Length,
    //                 PAttachments = attachmentPtr,
    //                 Width = swapChainExtent.Width,
    //                 Height = swapChainExtent.Height,
    //                 Layers = 1,
    //             };

    //             if (CreateVulkan.vk.CreateFramebuffer(LogicalDevice.device, &frameBufferInfo, null, framePtr) != Result.Success)
    //             {
    //                 throw new Exception("Failed to create framebuffer!");
    //             }
    //         }

    //     }
    // }

    void CleanUpSwapChain()
    {
        depth.Dispose();

        // foreach (var framebuffer in swapChainFrameBuffers)
        // {
        //     LogicalDevice.DestroyFramebuffer(framebuffer, null);
        // }

        foreach (var imageView in swapChainImageViews)
        {
            LogicalDevice.DestroyImageView(imageView, null);
        }

        khrSwapChain?.DestroySwapchain(LogicalDevice.device, swapChain, null);
    }
}