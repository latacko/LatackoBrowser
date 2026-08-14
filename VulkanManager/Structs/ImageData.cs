using System;
using Silk.NET.Vulkan;
using Vulkan;

namespace VulkanManager.Structs;

public struct ImageData : IDisposable
{
    public Image image;
    public DeviceMemory deviceMemory;
    public ImageView imageView;

    public void Dispose()
    {
        unsafe
        {
            LogicalDevice.DestroyImageView(imageView, null);
            LogicalDevice.DestroyImage(image, null);
            LogicalDevice.FreeMemory(deviceMemory, null);
        }
    }
}
