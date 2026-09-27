using System;
using Silk.NET.Vulkan;
using Vulkan;

namespace AtlasGeneratorCore;

public struct ImageData: IDisposable
{
    public Image atlasImage;
    public DeviceMemory atlasMemory;
    public ImageView imageView;

    public unsafe void Dispose()
    {
        CreateVulkan.vk.DestroyImageView(LogicalDevice.device, imageView, null);
        CreateVulkan.vk.DestroyImage(LogicalDevice.device, atlasImage, null);
        CreateVulkan.vk.FreeMemory(LogicalDevice.device, atlasMemory, null);
    }
}
