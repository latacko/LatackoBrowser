using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
namespace Vulkan;

public unsafe class LogicalDevice : IDisposable
{
    public static Device device;

    internal static Queue graphicsQueue;
    internal static Queue presentQueue;

    internal readonly static string[] deviceExtensions = new[]
    {
        KhrSwapchain.ExtensionName,
    };

    public void Create()
    {
        QueueFamilyIndices indices = PhysicalDevice.Instance.FindQueueFamilies(PhysicalDevice.physicalDevice);

        var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };
        uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

        DeviceQueueCreateInfo[] _queueCreateInfos = new DeviceQueueCreateInfo[uniqueQueueFamilies.Length];


        float queuePriority = 1.0f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            _queueCreateInfos[i] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = indices.GraphicsFamily!.Value,
                QueueCount = 1,
                PQueuePriorities = &queuePriority,
                PNext = null
            };
        }

        PhysicalDeviceVulkan12Features enabledVk12Features = new()
        {
            SType = StructureType.PhysicalDeviceVulkan12Features,
            DescriptorIndexing = true,
            ShaderSampledImageArrayNonUniformIndexing = true,
            DescriptorBindingVariableDescriptorCount = true,
            DescriptorBindingPartiallyBound = true,
            DescriptorBindingSampledImageUpdateAfterBind = true,
            RuntimeDescriptorArray = true,
            BufferDeviceAddress = true,
            TimelineSemaphore = true,
        };

        PhysicalDeviceVulkan13Features enabledVk13Features = new()
        {
            SType = StructureType.PhysicalDeviceVulkan13Features,
            PNext = &enabledVk12Features,
            Synchronization2 = true,
            DynamicRendering = true,
        };


        PhysicalDeviceFeatures _deviceFeatures = new()
        {
            SamplerAnisotropy = Vk.True,
            FillModeNonSolid = true,
        };

        fixed (DeviceQueueCreateInfo* queueCreateInfoPtr = _queueCreateInfos)
        {
            DeviceCreateInfo _createInfo = new()
            {
                SType = StructureType.DeviceCreateInfo,
                PNext = &enabledVk13Features,

                PQueueCreateInfos = queueCreateInfoPtr,
                QueueCreateInfoCount = (uint)_queueCreateInfos.Length,

                PEnabledFeatures = &_deviceFeatures,

                EnabledExtensionCount = (uint)deviceExtensions.Length,
                PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions),
            };

            if (CreateVulkan.ENABLE_VALIDATION_LAYERS)
            {
                _createInfo.EnabledLayerCount = (uint)CreateVulkan.ValidationLayers.Length;
                _createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(CreateVulkan.ValidationLayers);

                var _debugCreateInfo = CreateVulkan.PopulateDebugMessengerCreateInfo();
            }

            if (CreateVulkan.vk.CreateDevice(PhysicalDevice.physicalDevice, &_createInfo, null, out device) != Result.Success)
            {
                throw new Exception("Failed to create logical device!");
            }

            CreateVulkan.vk.GetDeviceQueue(device, indices.GraphicsFamily.Value, 0, out graphicsQueue);
            CreateVulkan.vk.GetDeviceQueue(device, indices.PresentFamily.Value, 0, out presentQueue);

            SilkMarshal.Free((nint)_createInfo.PpEnabledLayerNames);
            SilkMarshal.Free((nint)_createInfo.PpEnabledExtensionNames);
        }

    }

    public static void DestroyImageView(ImageView imageView, AllocationCallbacks* pAllocator)
    {
        CreateVulkan.vk.DestroyImageView(device, imageView, pAllocator);
    }

    public static void DestroyImage(Image image, AllocationCallbacks* pAllocator)
    {
        CreateVulkan.vk.DestroyImage(device, image, pAllocator);
    }

    public static void FreeMemory(DeviceMemory memory, AllocationCallbacks* pAllocator)
    {
        CreateVulkan.vk.FreeMemory(device, memory, pAllocator);
    }

    public static void DestroyFramebuffer(Framebuffer framebuffer, AllocationCallbacks* pAllocator)
    {
        CreateVulkan.vk.DestroyFramebuffer(device, framebuffer, pAllocator);
    }

    public void Dispose()
    {
        CreateVulkan.vk.DestroyDevice(device, null);
    }
}