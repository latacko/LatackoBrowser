using System.Net.Http.Headers;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace TextCore;

public class FontAtlas : IDisposable
{
    internal uint id { get; private set; } = 0;
    internal string name { get; private set; } = "";
    struct UploadRegion
    {
        internal ulong startOffset => endOffset - size;
        internal ulong endOffset;
        internal ulong size;
        internal ulong timelineValue;
    }

    public struct WaitingCharacter
    {
        public char character;
        public byte[] pixels;
        public int index;
    }
    #region CONSTS
    public const int GLYPH_SIZE = 64;
    public const int PADDING = 8;
    public const int RANGE = 6;

    public const int GLYPHD_IN_LINE = 16;
    public const int ATLAS_WIDTH = 1024;
    public const int ATLAS_HEIGHT = 1024;
    public const ulong ATLAS_SIZE = ATLAS_WIDTH * ATLAS_HEIGHT * 4;

    public const uint STAGING_BUFFER_GLYPH_COUNT = 10;

    const int BUFFER_GLYPH_SIZE = GLYPH_SIZE * GLYPH_SIZE * 4;
    const ulong BUFFER_SIZE = STAGING_BUFFER_GLYPH_COUNT * BUFFER_GLYPH_SIZE;
    #endregion

    internal readonly Dictionary<char, GlyphData> Glyphs = new();

    public Vector2D<float> Size;
    ushort createdGlyphs;

    Image atlasImage = default;
    DeviceMemory atlasMemory = default;
    ImageView imageView;
    bool _atlasInitialized = false;


    CommandBuffer actualCommandBuffer;

    Semaphore timelineSemaphore;
    ulong timelineValue = 0;


    Buffer stagingBuffer = new();
    DeviceMemory stagingBufferMemory = new();
    nint bufferData;

    ulong ringOffset = 0;
    ulong gpuSafeOffset = 0;

    List<UploadRegion> inFlight = new();


    internal float height = 0;
    internal float lineGap = 0;
    internal float baseline = 0;

    List<BufferImageCopy> uploadList = new();

    public Queue<WaitingCharacter> WaitingCharacters = new();

    internal BufferInfo<CharacterDataGPU> charactersBuffer;
    public unsafe FontAtlas(uint id, string name)
    {
        this.id = id;
        this.name = name;

        charactersBuffer = new(1114111, 0);
        ImageHelper.CreateImage(ATLAS_WIDTH, ATLAS_HEIGHT, Silk.NET.Vulkan.Format.R8G8B8A8Unorm, Silk.NET.Vulkan.ImageTiling.Optimal, Silk.NET.Vulkan.ImageUsageFlags.TransferDstBit | Silk.NET.Vulkan.ImageUsageFlags.SampledBit, Silk.NET.Vulkan.MemoryPropertyFlags.DeviceLocalBit, ref atlasImage, ref atlasMemory);
        imageView = ImageHelper.CreateImageView(atlasImage, Format.R8G8B8A8Unorm, ImageAspectFlags.ColorBit);

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

        BufferHelper.CreateBuffer(BUFFER_SIZE, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);
        nint _data;
        CreateVulkan.vk!.MapMemory(LogicalDevice.device, stagingBufferMemory, 0, BUFFER_SIZE, 0, (void**)&_data);
        bufferData = _data;

        TextManager.Instance.RegisterTexture(imageView, id);
    }

    public void Tick()
    {
        CreateVulkan.vk.GetSemaphoreCounterValue(LogicalDevice.device, timelineSemaphore, out var _currentValue);
        int _removed = inFlight.RemoveAll(r => r.timelineValue <= _currentValue);

        if (WaitingCharacters.Count > 0 && _removed > 0)
        {
            int _remaining = WaitingCharacters.Count > 10 ? 10 : WaitingCharacters.Count;
            StartRecording(_remaining);
            for (int i = 0; i < _remaining; i++)
            {
                var waiting = WaitingCharacters.Peek();

                GlyphData glyph = Glyphs[waiting.character];
                if (AddGlyph(waiting.character, waiting.pixels, ref glyph, waiting.index))
                {
                    WaitingCharacters.Dequeue();
                    Glyphs[waiting.character] = glyph;
                }
                else
                {
                    break;
                }
            }
            EndRecording();
        }
    }

    ulong Allocate()
    {
        ulong aligned = (ringOffset + 15) & ~15UL; // optional alignment

        if (aligned + BUFFER_GLYPH_SIZE > BUFFER_SIZE)
            aligned = 0; // wrap

        return aligned;
    }

    public unsafe void StartRecording(int glyphCount)
    {
        if (glyphCount > STAGING_BUFFER_GLYPH_COUNT)
            throw new Exception("Max glyph count in one recording is 10");

        uploadList.Clear();
    }

    internal unsafe bool AddGlyph(char character, byte[] pixels, ref GlyphData glyph, int index = -1)
    {
        ulong _offset = Allocate();
        bool _isBlocked = inFlight.Any(r => _offset >= r.startOffset && _offset < r.endOffset);

        int _glyphIndex = index == -1 ? createdGlyphs : index;

        int imageX = _glyphIndex % GLYPHD_IN_LINE;
        int imageY = _glyphIndex / GLYPHD_IN_LINE;

        int visualSize = GLYPH_SIZE - PADDING * 2;
        if (index == -1)
        {
            glyph.UVMin = new Vector2D<float>(
                (imageX * GLYPH_SIZE + PADDING) / (float)ATLAS_WIDTH,
                (imageY * GLYPH_SIZE + PADDING) / (float)ATLAS_HEIGHT
            );
            glyph.UVMax = new Vector2D<float>(
                (imageX * GLYPH_SIZE + PADDING + visualSize) / (float)ATLAS_WIDTH,
                (imageY * GLYPH_SIZE + PADDING + visualSize) / (float)ATLAS_HEIGHT
            );
            createdGlyphs += 1;
        }

        if (_isBlocked)
        {
            WaitingCharacters.Enqueue(new()
            {
                character = character,
                pixels = pixels,
                index = _glyphIndex
            });
            return false;
        }

        ringOffset = _offset + BUFFER_GLYPH_SIZE;

        pixels.AsSpan().CopyTo(new Span<byte>((void*)(bufferData + (nint)_offset), BUFFER_GLYPH_SIZE));

        // Console.WriteLine($"First 12 bytes of '{character}': " + 
        // string.Join(",", pixels.Take(12)));

        byte* ptr = (byte*)(bufferData + (nint)_offset);

        // for (int i = 0; i < BUFFER_GLYPH_SIZE; i += 4)
        // {
        //     ptr[i + 0] = 255;   // R
        //     ptr[i + 1] = 255; // G
        //     ptr[i + 2] = 255;   // B
        //     ptr[i + 3] = 255; // A
        // }



        uploadList.Add(new()
        {
            BufferOffset = _offset,
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
        });





        // Console.WriteLine("Attempting to write on: " + (uint)character + " char: " + character);
        ((CharacterDataGPU*)charactersBuffer.Mapped)[(uint)character] = new()
        {
            UV = new Vector2D<float>(glyph.UVMin.Y, glyph.UVMax.Y),
            Scale = height / glyph.Height,
            BearingY = (baseline - glyph.BearingY) / height
        };

        // Console.WriteLine("Glyph for " + character + " ascii " + (uint)character + " is: " + glyph);
        // Console.WriteLine("Scale for " +character + " ascii " + (uint)character +" is: " + height/glyph.Height);
        // Console.WriteLine("BearingY for " +character+" is: " + (glyph.Height-glyph.BearingY)/height);

        return true;
    }

    public unsafe void EndRecording()
    {
        if (uploadList.Count == 0)
            return;

        actualCommandBuffer = CmdHelper.BeginSingleTimeCommands();

        var srcLayout = !_atlasInitialized
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


        fixed (BufferImageCopy* uploadPtr = uploadList.ToArray())
            CreateVulkan.vk.CmdCopyBufferToImage(actualCommandBuffer, stagingBuffer, atlasImage, ImageLayout.TransferDstOptimal, (uint)uploadList.Count, uploadPtr);

        var _barrierTexRead = ImageHelper.TransitionImageLayout(atlasImage, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

        _barrierTexInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &_barrierTexRead
        };

        CreateVulkan.vk.CmdPipelineBarrier2(actualCommandBuffer, &_barrierTexInfo);
        CreateVulkan.vk.EndCommandBuffer(actualCommandBuffer);

        timelineValue++; // e.g. 1 after first upload, 2 after second...
        ulong _signalValue = timelineValue;

        inFlight.Add(new UploadRegion
        {
            endOffset = ringOffset,
            size = (ulong)(BUFFER_GLYPH_SIZE * uploadList.Count),
            timelineValue = timelineValue
        });

        var _cb = actualCommandBuffer;
        var _timelineSemaphore = timelineSemaphore;

        TimelineSemaphoreSubmitInfo _timelineSubmit = new()
        {
            SType = StructureType.TimelineSemaphoreSubmitInfo,
            SignalSemaphoreValueCount = 1,
            PSignalSemaphoreValues = &_signalValue,
        };

        SubmitInfo _submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            PNext = &_timelineSubmit,
            CommandBufferCount = 1,
            PCommandBuffers = &_cb,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &_timelineSemaphore,
        };

        uploadList.Clear();

        CreateVulkan.vk.QueueSubmit(LogicalDevice.graphicsQueue, 1, &_submitInfo, default);

        _atlasInitialized = true;

        // CreateVulkan.vk.WaitForFences(LogicalDevice.device, 1, &fence, Vk.True, ulong.MaxValue);
        // Console.WriteLine("Ended recording " + recordedGlyphs + " glyphs");

    }

    public void ScanText(string text)
    {
        string toLoad = "";
        for (int i = 0; i < text.Length; i++)
        {
            if (Glyphs.ContainsKey(text[i])) continue;
            Console.WriteLine("Nie ma " + text[i]);
            toLoad += text[i];
        }
        if (toLoad.Length > 0)
            FontManager.Instance.LoadFont(name, toLoad);
    }

    public unsafe void Dispose()
    {
        CreateVulkan.vk.DestroySemaphore(LogicalDevice.device, timelineSemaphore, null);
        charactersBuffer.Dispose();

        CreateVulkan.vk!.UnmapMemory(LogicalDevice.device, stagingBufferMemory);
        BufferHelper.DestroyBuffer(stagingBuffer, stagingBufferMemory);

        if (imageView.Handle != 0)
            CreateVulkan.vk.DestroyImageView(LogicalDevice.device, imageView, null);
        if (atlasImage.Handle != 0)
            CreateVulkan.vk.DestroyImage(LogicalDevice.device, atlasImage, null);
        if (atlasMemory.Handle != 0)
            CreateVulkan.vk.FreeMemory(LogicalDevice.device, atlasMemory, null);
    }
}