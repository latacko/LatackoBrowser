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

    public static void CreateTextureImage(CommandPool commandPool, string path, ref Image textureImage, ref DeviceMemory textureImageMemory, ulong id, Semaphore timelineSemaphore, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory)
    {
        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(path);

        if (img == null)
        {
            throw new Exception("Failed to load texture image!");
        }

        ulong _imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);

        stagingBuffer = new();
        stagingBufferMemory = new();

        BufferHelper.CreateBuffer(_imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        CreateVulkan.vk!.MapMemory(LogicalDevice.device, stagingBufferMemory, 0, _imageSize, 0, &data);
        img.CopyPixelDataTo(new Span<byte>(data, (int)_imageSize));
        CreateVulkan.vk!.UnmapMemory(LogicalDevice.device, stagingBufferMemory);

        CommandBuffer commandBuffer = CmdHelper.BeginSingleTimeCommands(commandPool);

        CreateImage((uint)img.Width, (uint)img.Height, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, 1, ref textureImage, ref textureImageMemory);

        var _barrierTexImage = TransitionImageLayout(textureImage, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);

        DependencyInfo _barrierTexInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &_barrierTexImage
        };
        CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, &_barrierTexInfo);

        BufferImageCopy _bufferImageCopy = new()
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
            ImageOffset = new(0, 0),
            ImageExtent = new()
            {
                Width = (uint)img.Width,
                Height = (uint)img.Height,
                Depth = 1,
            },
        };

        CreateVulkan.vk.CmdCopyBufferToImage(commandBuffer, stagingBuffer, textureImage, ImageLayout.TransferDstOptimal, 1, &_bufferImageCopy);

        var _barrierTexRead = TransitionImageLayout(textureImage, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
        _barrierTexInfo.PImageMemoryBarriers = &_barrierTexRead;

        CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, &_barrierTexInfo);
        CreateVulkan.vk.EndCommandBuffer(commandBuffer);

        ulong _newId = id+1;
        TimelineSemaphoreSubmitInfo _timelineSubmit = new()
        {
            SType = StructureType.TimelineSemaphoreSubmitInfo,
            SignalSemaphoreValueCount = 1,
            PSignalSemaphoreValues = &_newId,
        };

        SubmitInfo _submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            PNext = &_timelineSubmit,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &timelineSemaphore,
        };
        CreateVulkan.vk.QueueSubmit(LogicalDevice.graphicsQueue, 1, &_submitInfo, default);

        // CreateVulkan.vk.DestroyBuffer(LogicalDevice.device, _stagingBuffer, null);
        // CreateVulkan.vk.FreeMemory(LogicalDevice.device, _stagingBufferMemory, null);
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