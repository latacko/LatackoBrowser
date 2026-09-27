using System;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Microsoft.VisualBasic;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.Processing;
using Vulkan;
using VulkanManager;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace TextureCore;

public class TexturesManager : IDisposable
{
    CommandPool transferCommandPool;
    CommandBuffer commandBuffer;
    BufferData currentStagingBuffer;

    static TexturesManager? Instance;
    uint nextTextureId = 0;
    Fence transferDoneFance;

    readonly ConcurrentDictionary<string, Texture> textures = new();
    readonly ConcurrentQueue<Texture> texturesToLoad = new();
    private int isUploadRunning = 0; // 0 = idle, 1 = running

    public TexturesManager()
    {
        Instance = this;
    }

    public void Init()
    {
        CreateCommandPool();
        CreateCommandBuffer();
        CreateFence();
    }

    unsafe void CreateCommandPool()
    {
        Console.WriteLine(LogicalDevice.Indices);
        Console.WriteLine(LogicalDevice.Indices.TransferFamily.Value);
        CommandPoolCreateInfo commandPoolCI = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.TransientBit,
            QueueFamilyIndex = LogicalDevice.Indices.TransferFamily!.Value,
        };

        CreateVulkan.vk.CreateCommandPool(LogicalDevice.device, ref commandPoolCI, null, out transferCommandPool);
    }

    void CreateCommandBuffer()
    {
        CommandBufferAllocateInfo commandBufferCI = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = transferCommandPool,
            CommandBufferCount = 1
        };

        CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, ref commandBufferCI, out commandBuffer);
    }

    unsafe void CreateFence()
    {
        FenceCreateInfo fenceCI = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };
        CreateVulkan.vk.CreateFence(LogicalDevice.device, in fenceCI, null, out transferDoneFance);
    }

    internal void LoadRequestedTextures()
    {
        if (Interlocked.CompareExchange(ref isUploadRunning, 1, 0) != 0)
        {
            // Someone else already has it running — do nothing.
            return;
        }

        Task.Run(() =>
        {
            try
            {
                LoadRequestedTexturesThread();
            }
            finally
            {
                // Reset so a future request can start it again.
                Interlocked.Exchange(ref isUploadRunning, 0);
            }
        });
    }

    unsafe void RecordCommandBuffer()
    {
        ImageMemoryBarrier2* _barriersToTransfer = stackalloc ImageMemoryBarrier2[texturesToLoad.Count];
        ulong sizeOfTextures = 0;
        int i = 0;
        foreach (var textureInfo in texturesToLoad)
        {
            _barriersToTransfer[i] = LoadImageInfo(textureInfo, ref textureInfo.Image, ref textureInfo.Memory, ref sizeOfTextures);

            i++;
        }

        DependencyInfo _barrierTexInfoTransfer = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = (uint)texturesToLoad.Count,
            PImageMemoryBarriers = _barriersToTransfer
        };
        CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, &_barrierTexInfoTransfer);

        var (_stagingBuffer, size) = BufferHelper.CreateStagingBuffer<byte>((uint)sizeOfTextures);
        uint _elementHead = 0;

        currentStagingBuffer = _stagingBuffer;

        ImageMemoryBarrier2* _barriersToShaderRead = stackalloc ImageMemoryBarrier2[texturesToLoad.Count];
        i = 0;

        while (texturesToLoad.TryDequeue(out var textureInfo))
        {
            var (_bufferImgCopy, _barrier) = LoadImageToTexture(textureInfo, _stagingBuffer, ref _elementHead);
            _barriersToShaderRead[i] = _barrier;

            CopyBufferToImageInfo2 _copyBufferToImgInfo = new()
            {
                SrcBuffer = _stagingBuffer.Buffer,

                DstImage = textureInfo.Image,
                DstImageLayout = ImageLayout.TransferDstOptimal,

                RegionCount = 1,
                PRegions = &_bufferImgCopy
            };
            CreateVulkan.vk.CmdCopyBufferToImage2(commandBuffer, ref _copyBufferToImgInfo);
            i++;
        }

        DependencyInfo _barrierTexInfoShader = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = (uint)texturesToLoad.Count,
            PImageMemoryBarriers = _barriersToShaderRead
        };
        CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, &_barrierTexInfoShader);

        if (Vulkan.CreateVulkan.vk.EndCommandBuffer(commandBuffer) != Result.Success)
        {
            throw new Exception("Failed to record command buffer");
        }
    }

    unsafe void LoadRequestedTexturesThread()
    {
        CreateVulkan.vk.ResetCommandPool(LogicalDevice.device, transferCommandPool, CommandPoolResetFlags.None);
        RecordCommandBuffer();
        SubmitInfo2 submitInfo = new()
        {
            SType = StructureType.SubmitInfo2,
            WaitSemaphoreInfoCount = 0,
        };

        submitInfo.CommandBufferInfoCount = 1;
        CommandBufferSubmitInfo commandBufferSubmitInfo = new()
        {
            CommandBuffer = commandBuffer,
        };
        submitInfo.PCommandBufferInfos = &commandBufferSubmitInfo;

        if (CreateVulkan.vk.QueueSubmit2(LogicalDevice.GraphicsQueue, 1, &submitInfo, transferDoneFance) != Result.Success)
        {
            throw new Exception("Failed to submit command buffer!");
        }

        CreateVulkan.vk.WaitForFences(LogicalDevice.device, 1, in transferDoneFance, Vk.True, ulong.MaxValue);
        CreateVulkan.vk.ResetFences(LogicalDevice.device, 1, in transferDoneFance);

        BufferHelper.DestroyBuffer(currentStagingBuffer);

        if (!texturesToLoad.IsEmpty)
            LoadRequestedTexturesThread();
    }

    internal ImageMemoryBarrier2 LoadImageInfo(Texture texture, ref Image textureImage, ref DeviceMemory textureImageMemory, ref ulong sizeOfTextures)
    {
        var img = SixLabors.ImageSharp.Image.Identify(texture.path);

        if (img == null)
        {
            throw new Exception("Failed to load texture image!");
        }

        texture.width = (ushort)img.Width;
        texture.height = (ushort)img.Height;
        texture.bitsPerPixel = (byte)img.PixelType.BitsPerPixel;

        sizeOfTextures += texture.imgSize;

        ImageHelper.CreateImage((uint)img.Width, (uint)img.Height, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, 1, ref textureImage, ref textureImageMemory);

        var _barrierTexImage = ImageHelper.TransitionImageLayout(textureImage, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
        return _barrierTexImage;
    }

    internal unsafe (BufferImageCopy2, ImageMemoryBarrier2) LoadImageToTexture(Texture texture, BufferData stagingBuffer, ref uint elementHead)
    {
        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(texture.path);

        if (img == null)
        {
            throw new Exception("Failed to load texture image!");
        }

        img.CopyPixelDataTo(new Span<byte>((void*)stagingBuffer.Mapped, (int)texture.imgSize));

        BufferImageCopy2 _bufferImageCopy = new()
        {
            BufferOffset = elementHead,
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
                Width = texture.width,
                Height = texture.height,
                Depth = 1,
            },
        };
        elementHead += texture.imgSize;

        var _barrierTexRead = ImageHelper.TransitionImageLayout(texture.Image, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
        return (_bufferImageCopy, _barrierTexRead);
    }

    public static Texture? CreateTexture(string path)
    {
        if (Instance.textures.ContainsKey(path))
            return null;

        Texture _texture = new(path)
        {
            id = Instance.nextTextureId
        };

        return _texture;
    }

    public static Texture? GetTexture(string path)
    {
        if (Instance.textures.TryGetValue(path, out var texture))
            return texture;
        return null;
    }

    internal static Texture LoadTexture(Texture texture)
    {
        Instance.texturesToLoad.Append(texture);

        return texture;
    }

    public unsafe void Dispose()
    {
        CreateVulkan.vk.DestroyCommandPool(LogicalDevice.device, transferCommandPool, null);
        CreateVulkan.vk.DestroyFence(LogicalDevice.device, transferDoneFance, null);

        foreach (var texture in textures)
        {
            texture.Value.Dispose();
        }
    }
}
