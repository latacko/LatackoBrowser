using Microsoft.Diagnostics.Utilities;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
namespace Vulkan;

public unsafe class ImageHelper
{
    public static void CreateImage(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ref Image image, ref DeviceMemory imageMemory)
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

    public static ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags = ImageAspectFlags.ColorBit)
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

        if (CreateVulkan.vk.CreateImageView(LogicalDevice.device, &_viewInfo, null, out var imageView) != Result.Success)
        {
            throw new Exception("Failed to create image view!");
        }

        return imageView;
    }

    public void CreateTextureImage(string path, ref Image textureImage, ref DeviceMemory textureImageMemory)
    {
        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(path);

        if (img == null)
        {
            throw new Exception("Failed to load texture image!");
        }

        ulong _imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);

        Buffer _stagingBuffer = new();
        DeviceMemory _stagingBufferMemory = new();

        BufferHelper.CreateBuffer(_imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref _stagingBuffer, ref _stagingBufferMemory);

        void* data;
        CreateVulkan.vk!.MapMemory(LogicalDevice.device, _stagingBufferMemory, 0, _imageSize, 0, &data);
        img.CopyPixelDataTo(new Span<byte>(data, (int)_imageSize));
        CreateVulkan.vk!.UnmapMemory(LogicalDevice.device, _stagingBufferMemory);

        FenceCreateInfo _fenceOneTimeCI = new()
        {
            SType = StructureType.FenceCreateInfo,
        };

        CreateVulkan.vk.CreateFence(LogicalDevice.device, ref _fenceOneTimeCI, null, out Fence _fenceOneTime);
        CommandBuffer commandBuffer = VulkanManager.BeginSingleTimeCommands();

        CreateImage((uint)img.Width, (uint)img.Height, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, ref textureImage, ref textureImageMemory);

        var _barrierTexImage = TransitionImageLayout(textureImage, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
        DependencyInfo _barrierTexInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &_barrierTexImage
        };
        CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, &_barrierTexInfo);

        CopyBufferToImage(_stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);

        var _barrierTexRead = TransitionImageLayout(textureImage, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
        _barrierTexInfo.PImageMemoryBarriers = &_barrierTexRead;
        CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, &_barrierTexInfo);

        VulkanManager.EndSingleTimeCommands(commandBuffer, _fenceOneTime);
        CreateVulkan.vk.WaitForFences(LogicalDevice.device, 1, &_fenceOneTime, Vk.True, ulong.MaxValue);


        CreateVulkan.vk.DestroyBuffer(LogicalDevice.device, _stagingBuffer, null);
        CreateVulkan.vk.FreeMemory(LogicalDevice.device, _stagingBufferMemory, null);
    }

    ImageMemoryBarrier2 TransitionImageLayout(Image image, ImageLayout oldLayout, ImageLayout newLayout)
    {
        ImageMemoryBarrier2 _barrier2 = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.None,

            DstStageMask = PipelineStageFlags2.TransferBit,
            DstAccessMask = AccessFlags2.TransferWriteBit,

            OldLayout = oldLayout,
            NewLayout = newLayout,

            Image = image,
            SubresourceRange = new()
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LevelCount = 1,
                LayerCount = 1,
            },
        };

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            _barrier2.SrcAccessMask = AccessFlags2.None;
            _barrier2.DstAccessMask = AccessFlags2.TransferWriteBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            _barrier2.SrcAccessMask = AccessFlags2.TransferWriteBit;
            _barrier2.DstAccessMask = AccessFlags2.ShaderReadBit;
        }
        else
        {
            throw new Exception("Unsupported layout transition!");
        }

        return _barrier2;
    }

    void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
    {
        CommandBuffer commandBuffer = VulkanManager.BeginSingleTimeCommands();

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


        VulkanManager.EndSingleTimeCommandsIdle(commandBuffer);
    }
}