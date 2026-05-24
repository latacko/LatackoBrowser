using Silk.NET.Vulkan;
namespace Vulkan;

public unsafe class Depth : IDisposable
{
    public Image depthImage;
    DeviceMemory depthImageMemory;
    public ImageView depthImageView {get; private set; }
    internal static Format depthFormat;

    public void CreateDepthResources(uint width, uint height)
    {
        depthFormat = FindDepthFormat();

        ImageHelper.CreateImage(width, height, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, ref depthImage, ref depthImageMemory);
        depthImageView = ImageHelper.CreateImageView(depthImage, depthFormat, ImageAspectFlags.DepthBit);
    }

    bool HasStencilComponent(Format format)
    {
        return format == Format.D32SfloatS8Uint || format == Format.D32SfloatS8Uint;
    }

    Format FindDepthFormat()
    {
        return FindSupportedFormat([Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D32SfloatS8Uint], ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
    }

    Format FindSupportedFormat(Format[] candidates, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            CreateVulkan.vk.GetPhysicalDeviceFormatProperties2(PhysicalDevice.physicalDevice, format, out var props);

            if (tiling == ImageTiling.Linear && (props.FormatProperties.LinearTilingFeatures & features) == features)
            {
                return format;
            }
            else if (tiling == ImageTiling.Optimal && (props.FormatProperties.OptimalTilingFeatures & features) == features)
            {
                return format;
            }
        }

        throw new Exception("failed to find supported format!");
    }

    public void Dispose()
    {
        LogicalDevice.DestroyImageView(depthImageView, null);
        LogicalDevice.DestroyImage(depthImage, null);
        LogicalDevice.FreeMemory(depthImageMemory, null);
    }
}