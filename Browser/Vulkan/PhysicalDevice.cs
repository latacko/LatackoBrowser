using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
namespace Vulkan;

public unsafe class PhysicalDevice
{
    internal static PhysicalDevice Instance {get; private set; }
    internal static Silk.NET.Vulkan.PhysicalDevice physicalDevice;
    Vk vk;
    Instance vulkanInstance;

    KhrSurface khrSurface;
    SurfaceKHR surface;

    public PhysicalDevice(Vk vk, Instance vulkanInstance, KhrSurface khrSurface, SurfaceKHR surface)
    {
        Instance = this;
        this.vk = vk;
        this.vulkanInstance = vulkanInstance;
        this.khrSurface = khrSurface;
        this.surface = surface;
    }

    private void Pick()
    {
        uint _deviceCount = 0;
        vk.EnumeratePhysicalDevices(vulkanInstance, &_deviceCount, null);
        if (_deviceCount == 0)
            throw new Exception("Failed to find GPUs with Vulkan support!");

        Silk.NET.Vulkan.PhysicalDevice[] _devices = new Silk.NET.Vulkan.PhysicalDevice[_deviceCount];
        fixed (Silk.NET.Vulkan.PhysicalDevice* devicesPtr = _devices)
        {
            vk.EnumeratePhysicalDevices(vulkanInstance, &_deviceCount, devicesPtr);
        }

        Silk.NET.Vulkan.PhysicalDevice _bestDevice = default;
        int _bestDeviceScore = 0;

        foreach (var device in _devices)
        {
            vk.GetPhysicalDeviceProperties2(device, out PhysicalDeviceProperties2 _physicalDeviceProperties);
            vk.GetPhysicalDeviceFeatures2(device, out PhysicalDeviceFeatures2 _physicalDeviceFeatures);

            if (!IsDeviceSuitable(device)) continue;

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
            vk.GetPhysicalDeviceProperties2(physicalDevice, out PhysicalDeviceProperties2 _physicalDeviceProperties);
            Console.WriteLine("Using " + SilkMarshal.PtrToString((nint)_physicalDeviceProperties.Properties.DeviceName) + " gpu");
        }
    }

    bool IsDeviceSuitable(Silk.NET.Vulkan.PhysicalDevice physicalDevice)
    {
        QueueFamilyIndices indices = FindQueueFamilies(physicalDevice);

        bool extensionsSupported = CheckDeviceExtensionSupport(physicalDevice);

        bool swapChainAdequate = false;
        if (extensionsSupported)
        {
            SwapChainSupportDetails swapChainSupport = Swapchain.Instance.QuerySwapChainSupport(physicalDevice);
            swapChainAdequate = !(swapChainSupport.Formats.Length == 0) && !(swapChainSupport.PresentModes.Length == 0);
        }

        vk.GetPhysicalDeviceFeatures2(physicalDevice, out var _supportedFeatures);

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
        vk.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)IntPtr.Zero, &_extensionCount, null);
        ExtensionProperties[] _extensionProperties = new ExtensionProperties[_extensionCount];
        fixed (ExtensionProperties* extensionsPtr = _extensionProperties)
        {
            vk.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)IntPtr.Zero, &_extensionCount, extensionsPtr);
        }

        var availableExtensionsNames = _extensionProperties.Select(layer => SilkMarshal.PtrToString((IntPtr)layer.ExtensionName)).ToHashSet();
        return LogicalDevice.deviceExtensions.All(availableExtensionsNames.Contains);
    }

    internal QueueFamilyIndices FindQueueFamilies(Silk.NET.Vulkan.PhysicalDevice physicalDevice)
    {
        QueueFamilyIndices indices = default;

        uint _queueFamiliesCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &_queueFamiliesCount, null);
        QueueFamilyProperties[] _queueFamilyProperties = new QueueFamilyProperties[_queueFamiliesCount];
        fixed (QueueFamilyProperties* proportiesPtr = _queueFamilyProperties)
        {
            vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &_queueFamiliesCount, proportiesPtr);
        }

        uint i = 0;
        foreach (var queueFamily in _queueFamilyProperties)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.GraphicsFamily = i;
            }

            khrSurface!.GetPhysicalDeviceSurfaceSupport(physicalDevice, i, surface, out var presentSupport);
            if (presentSupport)
            {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }


}