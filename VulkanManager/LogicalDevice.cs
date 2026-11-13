using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
namespace Vulkan;

public unsafe class LogicalDevice : IDisposable
{
    public static Device device;

    public static QueueFamilyIndices Indices { get; private set; }
    public static Queue GraphicsQueue;
    public static Queue TransferQueue;
    public static Queue PresentQueue;

    readonly string[] deviceExtensions;

    public LogicalDevice(string[] deviceExtensions)
    {
        this.deviceExtensions = deviceExtensions;
    }

    public uint[] GetIndices()
    {
        Indices = Vulkan.PhysicalDevice.Instance.FindQueueFamilies(Vulkan.PhysicalDevice.physicalDevice);

        return [Indices.GraphicsFamily!.Value, Indices.PresentFamily!.Value];
    }

    public void Create()
    {
        var uniqueQueueFamilies = GetIndices().Distinct().ToArray();

        DeviceQueueCreateInfo[] _queueCreateInfos = new DeviceQueueCreateInfo[uniqueQueueFamilies.Length];


        float queuePriority = 1.0f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            _queueCreateInfos[i] = new(
                queueFamilyIndex: uniqueQueueFamilies[i],
                queueCount: 1,
                pQueuePriorities: &queuePriority
            );
        }

        PhysicalDeviceVulkan12Features enabledVk12Features = new(
            descriptorIndexing: true,
            shaderSampledImageArrayNonUniformIndexing: true,
            descriptorBindingVariableDescriptorCount: true,
            descriptorBindingPartiallyBound: true,
            descriptorBindingSampledImageUpdateAfterBind: true,
            runtimeDescriptorArray: true,
            bufferDeviceAddress: true,
            timelineSemaphore: true
        );

        PhysicalDeviceVulkan13Features enabledVk13Features = new(
            pNext: &enabledVk12Features,
            synchronization2: true,
            dynamicRendering: true
        );


        PhysicalDeviceFeatures _deviceFeatures = new(
            samplerAnisotropy: Vk.True,
            fillModeNonSolid: true
        );

        fixed (DeviceQueueCreateInfo* queueCreateInfoPtr = _queueCreateInfos)
        {
            DeviceCreateInfo _createInfo = new(
                pNext: &enabledVk13Features,

                pQueueCreateInfos: queueCreateInfoPtr,
                queueCreateInfoCount: (uint)_queueCreateInfos.Length,

                pEnabledFeatures: &_deviceFeatures,

                enabledExtensionCount: (uint)deviceExtensions.Length,
                ppEnabledExtensionNames: (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions)
            );

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

            GetDeviceQueue();

            SilkMarshal.Free((nint)_createInfo.PpEnabledLayerNames);
            SilkMarshal.Free((nint)_createInfo.PpEnabledExtensionNames);
        }
    }

    protected void GetDeviceQueue()
    {
        CreateVulkan.vk.GetDeviceQueue(device, Indices.GraphicsFamily!.Value, 0, out GraphicsQueue);
        CreateVulkan.vk.GetDeviceQueue(device, Indices.PresentFamily!.Value, 0, out PresentQueue);
        CreateVulkan.vk.GetDeviceQueue(device, Indices.TransferFamily!.Value, 0, out TransferQueue);
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