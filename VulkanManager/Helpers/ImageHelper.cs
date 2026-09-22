using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;
namespace Vulkan;

public unsafe class ImageHelper
{
    public static void CreateImage(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, uint miplevels, ref Image image, ref DeviceMemory imageMemory)
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
            MipLevels = miplevels,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive
        };

        if (CreateVulkan.vk.CreateImage(LogicalDevice.device, &_imageInfo, null, out image) != Result.Success)
        {
            throw new Exception("Failed to create image!");
        }

        MemoryRequirements _memRequirements;
        CreateVulkan.vk.GetImageMemoryRequirements(LogicalDevice.device, image, &_memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,

            AllocationSize = _memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(_memRequirements.MemoryTypeBits, properties)
        };

        if (CreateVulkan.vk.AllocateMemory(LogicalDevice.device, &allocInfo, null, out imageMemory) != Result.Success)
        {
            throw new Exception("Failed to allocate image memory");
        }

        CreateVulkan.vk.BindImageMemory(LogicalDevice.device, image, imageMemory, 0);
    }

    public static uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        CreateVulkan.vk.GetPhysicalDeviceMemoryProperties(PhysicalDevice.physicalDevice, out var memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }
        throw new Exception("Failed to find suitable memory type!");
    }

    public static ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags = ImageAspectFlags.ColorBit, uint mipLevels = 1)
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
                LevelCount = mipLevels,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        if (CreateVulkan.vk.CreateImageView(LogicalDevice.device, &_viewInfo, null, out var imageView) != Result.Success)
        {
            throw new Exception("Failed to create image view!");
        }

        return imageView;
    }

    

    public static ImageMemoryBarrier2 TransitionImageLayout(Image image, ImageLayout oldLayout, ImageLayout newLayout, uint mipLevels = 1)
    {
        ImageMemoryBarrier2 _barrier2 = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.None,

            OldLayout = oldLayout,
            NewLayout = newLayout,

            Image = image,
            SubresourceRange = new()
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LevelCount = mipLevels,
                LayerCount = 1,
            },
        };

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            _barrier2.SrcStageMask = PipelineStageFlags2.None;
            _barrier2.SrcAccessMask = AccessFlags2.None;
            _barrier2.DstStageMask = PipelineStageFlags2.TransferBit;
            _barrier2.DstAccessMask = AccessFlags2.TransferWriteBit;
        }
        else if (oldLayout == ImageLayout.ShaderReadOnlyOptimal && newLayout == ImageLayout.TransferDstOptimal)
        {
            _barrier2.SrcStageMask = PipelineStageFlags2.FragmentShaderBit; // was being read in fragment shader
            _barrier2.SrcAccessMask = AccessFlags2.ShaderReadBit;
            _barrier2.DstStageMask = PipelineStageFlags2.TransferBit;
            _barrier2.DstAccessMask = AccessFlags2.TransferWriteBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            _barrier2.SrcStageMask = PipelineStageFlags2.TransferBit;
            _barrier2.SrcAccessMask = AccessFlags2.TransferWriteBit;
            _barrier2.DstStageMask = PipelineStageFlags2.FragmentShaderBit;
            _barrier2.DstAccessMask = AccessFlags2.ShaderReadBit;
        }
        else
        {
            throw new Exception("Unsupported layout transition!");
        }

        return _barrier2;
    }

    internal static void CopyBufferToImage(CommandPool commandPool, Buffer buffer, Image image, uint width, uint height)
    {
        CommandBuffer commandBuffer = CmdHelper.BeginSingleTimeCommands(commandPool);

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

        CreateVulkan.vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, &region);
    }

    public static void DestroyTexture(Image image, DeviceMemory imageMemory, ImageView imageView)
    {
        CreateVulkan.vk.DestroyImageView(LogicalDevice.device, imageView, null);
        CreateVulkan.vk.DestroyImage(LogicalDevice.device, image, null);
        CreateVulkan.vk.FreeMemory(LogicalDevice.device, imageMemory, null);
    }
}