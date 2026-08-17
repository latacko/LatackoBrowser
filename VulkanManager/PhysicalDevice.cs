using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
namespace Vulkan;

public unsafe class PhysicalDevice
{
    internal static PhysicalDevice Instance { get; private set; }
    internal static Silk.NET.Vulkan.PhysicalDevice physicalDevice;
    readonly string[] deviceExtensions;

    KhrSurface khrSurface;
    SurfaceKHR surfaceKHR;


    public PhysicalDevice(string[] deviceExtensions, KhrSurface khrSurface, SurfaceKHR surfaceKHR)
    {
        Instance = this;
        this.deviceExtensions = deviceExtensions;
        this.khrSurface = khrSurface;
        this.surfaceKHR = surfaceKHR;
    }

    internal void Pick()
    {
        uint _deviceCount = 0;
        CreateVulkan.vk.EnumeratePhysicalDevices(CreateVulkan.vulkanInstance, &_deviceCount, null);
        if (_deviceCount == 0)
            throw new Exception("Failed to find GPUs with Vulkan support!");

        Silk.NET.Vulkan.PhysicalDevice[] _devices = new Silk.NET.Vulkan.PhysicalDevice[_deviceCount];
        fixed (Silk.NET.Vulkan.PhysicalDevice* devicesPtr = _devices)
        {
            CreateVulkan.vk.EnumeratePhysicalDevices(CreateVulkan.vulkanInstance, &_deviceCount, devicesPtr);
        }

        Silk.NET.Vulkan.PhysicalDevice _bestDevice = default;
        int _bestDeviceScore = 0;

        foreach (var device in _devices)
        {
            CreateVulkan.vk.GetPhysicalDeviceProperties2(device, out PhysicalDeviceProperties2 _physicalDeviceProperties);
            CreateVulkan.vk.GetPhysicalDeviceFeatures2(device, out PhysicalDeviceFeatures2 _physicalDeviceFeatures);
            // Console.WriteLine(SilkMarshal.PtrToString((nint)_physicalDeviceProperties.Properties.DeviceName) + " is found");
            if (!IsDeviceSuitable(device)) continue;
            // Console.WriteLine(SilkMarshal.PtrToString((nint)_physicalDeviceProperties.Properties.DeviceName) + " is suitable");

            int _score = RateDeviceSuitability(_physicalDeviceProperties, _physicalDeviceFeatures);
            if (_score > _bestDeviceScore)
            {
                _bestDevice = device;
                _bestDeviceScore = _score;
            }
        }

        physicalDevice = _bestDevice;

        if (physicalDevice.Handle == 0)
        {
            throw new Exception("Failed to find a suitable GPU!");
        }
        else
        {
            CreateVulkan.vk.GetPhysicalDeviceProperties2(physicalDevice, out PhysicalDeviceProperties2 _physicalDeviceProperties);
            // Console.WriteLine("Using " + SilkMarshal.PtrToString((nint)_physicalDeviceProperties.Properties.DeviceName) + " gpu");
        }
    }

    bool IsDeviceSuitable(Silk.NET.Vulkan.PhysicalDevice physicalDevice)
    {
        QueueFamilyIndices indices = FindQueueFamilies(physicalDevice);

        bool extensionsSupported = CheckDeviceExtensionSupport(physicalDevice);

        bool swapChainAdequate = false;
        if (extensionsSupported)
        {
            SwapChainSupportDetails swapChainSupport = Swapchain.QuerySwapChainSupport(physicalDevice);
            swapChainAdequate = !(swapChainSupport.Formats.Length == 0) && !(swapChainSupport.PresentModes.Length == 0);
        }

        CreateVulkan.vk.GetPhysicalDeviceFeatures2(physicalDevice, out var _supportedFeatures);
        return indices.IsComplete() && extensionsSupported && swapChainAdequate && _supportedFeatures.Features.SamplerAnisotropy;
    }

    int RateDeviceSuitability(PhysicalDeviceProperties2 physicalDeviceProperties, PhysicalDeviceFeatures2 physicalDeviceFeatures)
    {
        int score = 0;

        if (physicalDeviceProperties.Properties.DeviceType == PhysicalDeviceType.DiscreteGpu)
        {
            score += 1000;
        }

        score += (int)physicalDeviceProperties.Properties.Limits.MaxImageDimension2D;

        if (!physicalDeviceFeatures.Features.GeometryShader)
        {
            return 0;
        }

        return score;
    }

    bool CheckDeviceExtensionSupport(Silk.NET.Vulkan.PhysicalDevice physicalDevice)
    {
        uint _extensionCount = 0;
        CreateVulkan.vk.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)IntPtr.Zero, &_extensionCount, null);
        ExtensionProperties[] _extensionProperties = new ExtensionProperties[_extensionCount];
        fixed (ExtensionProperties* extensionsPtr = _extensionProperties)
        {
            CreateVulkan.vk.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)IntPtr.Zero, &_extensionCount, extensionsPtr);
        }

        var availableExtensionsNames = _extensionProperties.Select(layer => SilkMarshal.PtrToString((IntPtr)layer.ExtensionName)).ToHashSet();
        return deviceExtensions.All(availableExtensionsNames.Contains);
    }

    internal QueueFamilyIndices FindQueueFamilies(Silk.NET.Vulkan.PhysicalDevice physicalDevice)
    {
        QueueFamilyIndices indices = default;

        uint _queueFamiliesCount = 0;
        CreateVulkan.vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &_queueFamiliesCount, null);
        QueueFamilyProperties[] _queueFamilyProperties = new QueueFamilyProperties[_queueFamiliesCount];
        fixed (QueueFamilyProperties* proportiesPtr = _queueFamilyProperties)
        {
            CreateVulkan.vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &_queueFamiliesCount, proportiesPtr);
        }

        uint i = 0;
        foreach (var queueFamily in _queueFamilyProperties)
        {
            bool _hasGraphics = queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit);
            bool _hasTransfer = queueFamily.QueueFlags.HasFlag(QueueFlags.TransferBit);
            bool _hasCompute = queueFamily.QueueFlags.HasFlag(QueueFlags.ComputeBit);

            khrSurface!.GetPhysicalDeviceSurfaceSupport(physicalDevice, i, surfaceKHR, out var presentSupport);

            if (_hasGraphics && presentSupport)
            {
                // Ideal: same family does both — always prefer this, overwrite freely
                indices.GraphicsFamily = i;
                indices.PresentFamily = i;
            }
            else
            {
                if (_hasGraphics && !indices.GraphicsFamily.HasValue)
                    indices.GraphicsFamily = i;

                if (presentSupport && !indices.PresentFamily.HasValue)
                    indices.PresentFamily = i;
            }

            if (_hasTransfer && !_hasGraphics && !_hasCompute)
            {
                indices.TransferFamily = i;
            }
            
            i++;
        }

        if (!indices.TransferFamily.HasValue && indices.GraphicsFamily.HasValue)
        {
            indices.TransferFamily = indices.GraphicsFamily;
        }


        return indices;
    }
}