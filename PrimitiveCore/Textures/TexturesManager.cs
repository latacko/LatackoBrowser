using System;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Silk.NET.Vulkan;
using Vulkan;
using VulkanManager;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace PrimitiveCore.Textures;

public class TexturesManager : IDisposable, IRenderTick
{
    static TexturesManager? Instance;
    static uint nextTextureId = 0;
    static Semaphore timelineSemaphore;
    static readonly ConcurrentDictionary<string, Texture> textures = new();
    readonly ConcurrentQueue<Texture> texturesToLoad = new();
    Task uploadTexturesTask;

    struct TextureStagingBuffer
    {
        public Silk.NET.Vulkan.Buffer buffer;
        public DeviceMemory deviceMemory;
        public uint id;
    }
    static List<TextureStagingBuffer> texturesStagingBuffer = new();

    public TexturesManager()
    {
        Instance = this;
    }

    public unsafe void Init()
    {
        SemaphoreTypeCreateInfo _typeInfo = new()
        {
            SType = StructureType.SemaphoreTypeCreateInfo,
            SemaphoreType = SemaphoreType.Timeline,
            InitialValue = 0,
        };

        SemaphoreCreateInfo _semCI = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
            PNext = &_typeInfo,
        };

        CreateVulkan.vk.CreateSemaphore(LogicalDevice.device, &_semCI, null, out timelineSemaphore);
    }

    public unsafe void LoadRequestedTextures()
    {
        CommandBuffer commandBuffer =new();
        ImageMemoryBarrier2* _barriers = stackalloc ImageMemoryBarrier2[texturesToLoad.Count];
        ulong sizeOfTextures = 0;
        int i = 0;
        foreach (var textureInfo in texturesToLoad)
        {
            _barriers[i] = CreateTextureImage(textureInfo, ref textureInfo.Image, ref textureInfo.Memory, ref sizeOfTextures);

            i++;
            // texturesStagingBuffer.Add(_textureStagingBuffer);

            // _texture.imageView = ImageHelper.CreateImageView(_texture.Image, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);
            // PrimitiveInstancesManager.Instance.RegisterTexture(_texture.imageView, nextTextureId);

            // nextTextureId++;
            // textures.Add(path, _texture);
        }
        var _stagingBuffer = BufferHelper.CreateStagingBuffer<byte>((uint)sizeOfTextures);

        DependencyInfo _barrierTexInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = (uint)texturesToLoad.Count,
            PImageMemoryBarriers = _barriers
        };
        CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, &_barrierTexInfo);


    }

    public unsafe ImageMemoryBarrier2 CreateTextureImage(Texture texture, ref Image textureImage, ref DeviceMemory textureImageMemory, ref ulong sizeOfTextures)
    {
        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(texture.path);

        if (img == null)
        {
            throw new Exception("Failed to load texture image!");
        }

        texture.width = (ushort)img.Width;
        texture.height = (ushort)img.Height;
        texture.bitsPerPixel = (byte)img.PixelType.BitsPerPixel;

        sizeOfTextures+= texture.imgSize;


        // stagingBuffer = new();
        // stagingBufferMemory = new();

        // BufferHelper.CreateBuffer(_imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        // void* data;
        // CreateVulkan.vk!.MapMemory(LogicalDevice.device, stagingBufferMemory, 0, _imageSize, 0, &data);
        // img.CopyPixelDataTo(new Span<byte>(data, (int)_imageSize));
        // CreateVulkan.vk!.UnmapMemory(LogicalDevice.device, stagingBufferMemory);

        ImageHelper.CreateImage((uint)img.Width, (uint)img.Height, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, 1, ref textureImage, ref textureImageMemory);

        var _barrierTexImage = ImageHelper.TransitionImageLayout(textureImage, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
        return _barrierTexImage;
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

        // CopyBufferToImageInfo2 copyBufferToImageInfo2 = new()
        // {

        // }

        // CreateVulkan.vk.CmdCopyBufferToImage2(commandBuffer, stagingBuffer, textureImage, ImageLayout.TransferDstOptimal, 1, &_bufferImageCopy);

        var _barrierTexRead = TransitionImageLayout(textureImage, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
        _barrierTexInfo.PImageMemoryBarriers = &_barrierTexRead;

        CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, &_barrierTexInfo);
        CreateVulkan.vk.EndCommandBuffer(commandBuffer);

        ulong _newId = id + 1;
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
        CreateVulkan.vk.QueueSubmit(LogicalDevice.GraphicsQueue, 1, &_submitInfo, default);

        // CreateVulkan.vk.DestroyBuffer(LogicalDevice.device, _stagingBuffer, null);
        // CreateVulkan.vk.FreeMemory(LogicalDevice.device, _stagingBufferMemory, null);
    }

    public static Texture LoadTexture(string path)
    {
        if (textures.TryGetValue(path, out var texture))
            return texture;

        Texture _texture = new(path)
        {
            id = nextTextureId
        };

        Instance.texturesToLoad.Append(_texture);

        return _texture;

        TextureStagingBuffer _textureStagingBuffer = new()
        {
            id = nextTextureId,
        };

        return _texture;
    }

    public void RenderTick(uint frameInFlight)
    {
        CreateVulkan.vk.GetSemaphoreCounterValue(LogicalDevice.device, timelineSemaphore, out var _currentValue);
        for (int i = texturesStagingBuffer.Count - 1; i >= 0; i--)
        {
            if (texturesStagingBuffer[i].id < _currentValue)
            {
                BufferHelper.DestroyBuffer(texturesStagingBuffer[i].buffer, texturesStagingBuffer[i].deviceMemory);
                texturesStagingBuffer.RemoveAt(i);
            }
        }
    }

    public unsafe void Dispose()
    {
        CreateVulkan.vk.DestroySemaphore(LogicalDevice.device, timelineSemaphore, null);
        foreach (var texture in textures)
        {
            texture.Value.Dispose();
        }
    }
}
