using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace TextCore;

public class FontAtlas : IDisposable
{
    public const int GLYPH_SIZE = 48;
    public const int PADDING = 6;
    public const int RANGE = 6;

    public const int GLYPHD_IN_LINE = 21;
    public const int ATLAS_WIDTH = 1024;
    public const int ATLAS_HEIGHT = 1024;
    public const ulong ATLAS_SIZE = ATLAS_WIDTH * ATLAS_HEIGHT * 4;

    internal readonly Dictionary<char, GlyphData> Glyphs = new();

    public Vector2D<float> Size;
    ushort createdGlyphs;
    ushort recordedGlyphs;

    Image atlasImage = default;
    DeviceMemory atlasMemory = default;
    ImageView imageView;

    CommandBuffer actualCommandBuffer;

    Buffer stagingBuffer = new();
    DeviceMemory stagingBufferMemory = new();
    nint bufferData;

    internal float height = 0;
    internal float lineGap = 0;

    public void Create()
    {
        ImageHelper.CreateImage(ATLAS_WIDTH, ATLAS_HEIGHT, Silk.NET.Vulkan.Format.R8G8B8A8Unorm, Silk.NET.Vulkan.ImageTiling.Optimal, Silk.NET.Vulkan.ImageUsageFlags.TransferDstBit | Silk.NET.Vulkan.ImageUsageFlags.SampledBit, Silk.NET.Vulkan.MemoryPropertyFlags.DeviceLocalBit, ref atlasImage, ref atlasMemory);
        imageView = ImageHelper.CreateImageView(atlasImage, Format.R8G8B8A8Unorm, ImageAspectFlags.ColorBit);
    }


    public unsafe void StartRecording(out Fence oneTimeFence, int glyphCount)
    {
        recordedGlyphs = 0;
        ulong _bufferSize = (ulong)(glyphCount * GLYPH_SIZE * GLYPH_SIZE * 4);

        FenceCreateInfo _fenceOneTimeCI = new()
        {
            SType = StructureType.FenceCreateInfo,
        };
        CreateVulkan.vk.CreateFence(LogicalDevice.device, ref _fenceOneTimeCI, null, out oneTimeFence);

        actualCommandBuffer = CmdHelper.BeginSingleTimeCommands();

        BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);
        nint _data;
        CreateVulkan.vk!.MapMemory(LogicalDevice.device, stagingBufferMemory, 0, _bufferSize, 0, (void**)&_data);
        bufferData = _data;

        var srcLayout = createdGlyphs == 0
            ? ImageLayout.Undefined
            : ImageLayout.ShaderReadOnlyOptimal;
        var _barrierTexImage = ImageHelper.TransitionImageLayout(atlasImage, srcLayout, ImageLayout.TransferDstOptimal);
        DependencyInfo _barrierTexInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &_barrierTexImage
        };
        CreateVulkan.vk.CmdPipelineBarrier2(actualCommandBuffer, &_barrierTexInfo);
    }

    internal unsafe void AddGlyph(char character, byte[] pixels, ref GlyphData glyph)
    {
        int imageX = createdGlyphs % GLYPHD_IN_LINE;
        int imageY = createdGlyphs / GLYPHD_IN_LINE;

        ulong glyphOffset = (ulong)(recordedGlyphs * GLYPH_SIZE * GLYPH_SIZE * 4);

        pixels.AsSpan().CopyTo(new Span<byte>((void*)(bufferData + (nint)glyphOffset), GLYPH_SIZE * GLYPH_SIZE * 4));

        BufferImageCopy region = new()
        {
            BufferOffset = glyphOffset,
            BufferRowLength = GLYPH_SIZE,
            BufferImageHeight = GLYPH_SIZE,

            ImageSubresource = new()
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
            ImageOffset = new(imageX * GLYPH_SIZE, imageY * GLYPH_SIZE, 0),
            ImageExtent = new()
            {
                Width = GLYPH_SIZE,
                Height = GLYPH_SIZE,
                Depth = 1,
            },
        };

        CreateVulkan.vk.CmdCopyBufferToImage(actualCommandBuffer, stagingBuffer, atlasImage, ImageLayout.TransferDstOptimal, 1, &region);

        int visualSize = GLYPH_SIZE - PADDING * 2;
        glyph.UVMin = new Vector2D<float>(
            (imageX * GLYPH_SIZE + PADDING) / (float)ATLAS_WIDTH,
            (imageY * GLYPH_SIZE + PADDING) / (float)ATLAS_HEIGHT
        );
        glyph.UVMax = new Vector2D<float>(
            (imageX * GLYPH_SIZE + PADDING + visualSize) / (float)ATLAS_WIDTH,
            (imageY * GLYPH_SIZE + PADDING + visualSize) / (float)ATLAS_HEIGHT
        );

        Glyphs[character] = glyph;
        recordedGlyphs++;
        createdGlyphs++;
    }

    public unsafe void EndRecording(Fence fence)
    {

        var _barrierTexRead = ImageHelper.TransitionImageLayout(atlasImage, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
        DependencyInfo _barrierTexInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &_barrierTexRead
        };
        CreateVulkan.vk.CmdPipelineBarrier2(actualCommandBuffer, &_barrierTexInfo);
        CmdHelper.EndSingleTimeCommands(actualCommandBuffer, fence);

        CreateVulkan.vk.WaitForFences(LogicalDevice.device, 1, &fence, Vk.True, ulong.MaxValue);
        Console.WriteLine("Ended recording " + recordedGlyphs + " glyphs");

        CreateVulkan.vk!.UnmapMemory(LogicalDevice.device, stagingBufferMemory);
        CreateVulkan.vk.DestroyBuffer(LogicalDevice.device, stagingBuffer, null);
        CreateVulkan.vk.FreeMemory(LogicalDevice.device, stagingBufferMemory, null);
    }

    public unsafe void Dispose()
    {
        if (imageView.Handle != 0)
        {
            Console.WriteLine("Disposiing view");
            CreateVulkan.vk.DestroyImageView(LogicalDevice.device, imageView, null);
        }
        if (atlasImage.Handle != 0)
        {

            Console.WriteLine("Disposiing image");
            CreateVulkan.vk.DestroyImage(LogicalDevice.device, atlasImage, null);
        }
        if (atlasMemory.Handle != 0)
        {

            Console.WriteLine("Disposiing memory");
            CreateVulkan.vk.FreeMemory(LogicalDevice.device, atlasMemory, null);
        }
    }
}