using System;
using MsdfAtlasGen;
using Msdfgen;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Vulkan;
using VulkanManager.BufferManager;

namespace AtlasGeneratorCore;

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
        internal uint miplevel;
    }

    public struct WaitingCharacter
    {
        public char character;
        public byte[] pixels;
        public int index;
    }
    #region CONSTS
    public const int GLYPHD_IN_LINE = 16;
    public const uint STAGING_BUFFER_GLYPH_COUNT = 10;

    public readonly static MSDFLevelData MSDFLevelData = new()
    {
        AtlasSize = 1024,
        Padding = 8,
        Range = 6,
        GlyphSize = 64,
    };

    #endregion

    #region Glyph attributes

    double glyphScale;
    Msdfgen.Range glyphUnitRange;
    Msdfgen.Range glyphPxRange;
    Padding glyphInnerPadding;
    Padding glyphInnerPxPadding;
    Padding glyphOuterPadding;
    Padding glyphOuterPxPadding;
    double glyphMiterLimit;
    bool glyphAlignOriginX;
    bool glyphAlignOriginY;

    #endregion

    internal readonly Dictionary<char, GlyphGeometry> Glyphs = new();

    ushort createdGlyphs;

    Image atlasImage = default;
    DeviceMemory atlasMemory = default;
    ImageView imageView;
    bool _atlasInitialized = false;

    readonly AtlasRingBuffer stagingBuffer = new(STAGING_BUFFER_GLYPH_COUNT, MSDFLevelData);

    readonly List<UploadRegion> inFlight = new();


    internal float height = 0;
    internal float lineGap = 0;
    internal float baseline = 0;

    readonly List<BufferImageCopy> uploadList = new();

    public readonly Queue<WaitingCharacter> WaitingCharacters = new();

    internal CharactersBuffer charactersBuffer;
    IProgress<double>? progress = null;

    public FontAtlas(uint id, string name, Action<ImageView, uint> registerTextureCB, IProgress<double>? progress = null)
    {
        this.id = id;
        this.name = name;
        this.progress = progress;

        charactersBuffer = new();
        ImageHelper.CreateImage(MSDFLevelData.AtlasSize, MSDFLevelData.AtlasSize, Silk.NET.Vulkan.Format.R8G8B8A8Unorm, Silk.NET.Vulkan.ImageTiling.Optimal, Silk.NET.Vulkan.ImageUsageFlags.TransferDstBit | Silk.NET.Vulkan.ImageUsageFlags.SampledBit, Silk.NET.Vulkan.MemoryPropertyFlags.DeviceLocalBit, 1, ref atlasImage, ref atlasMemory);
        imageView = ImageHelper.CreateImageView(atlasImage, Format.R8G8B8A8Unorm, ImageAspectFlags.ColorBit, 1);

        stagingBuffer.Init();

        registerTextureCB.Invoke(imageView, id);
    }

    #region Atlas functions
    public void Tick(uint frameInFlight)
    {
        var _currentValue = FontAtlasesManager.Instance!.GetCurrentDoneSignalValue();
        int _removed = inFlight.RemoveAll(r => r.timelineValue <= _currentValue);
        progress?.Report((double)_removed / (inFlight.Count + _removed + WaitingCharacters.Count));

        if (WaitingCharacters.Count > 0 && _removed > 0)
        {
            uint _remaining = WaitingCharacters.Count > STAGING_BUFFER_GLYPH_COUNT ? STAGING_BUFFER_GLYPH_COUNT : (uint)WaitingCharacters.Count;
            StartRecording(_remaining);
            for (int i = 0; i < _remaining; i++)
            {
                var waiting = WaitingCharacters.Peek();

                var _glyphGeometry = Glyphs[waiting.character];
                if (AddGlyph(_glyphGeometry, waiting.pixels, waiting.index))
                {
                    WaitingCharacters.Dequeue();
                    Glyphs[waiting.character] = _glyphGeometry;
                }
                else
                {
                    break;
                }
            }
            EndRecording(frameInFlight);
        }

        charactersBuffer.RenderTick(frameInFlight);
    }

    public void StartRecording(uint glyphCount)
    {
        if (glyphCount > STAGING_BUFFER_GLYPH_COUNT)
            throw new Exception("Max glyph count in one recording is 10");

        uploadList.Clear();
    }

    internal void AddCharacters(GlyphGeometry[] characters)
    {
        Parallel.For(0, characters.Length, (i) =>
        {
            var _character = characters[i];

            double pixelsPerUnit = glyphScale * _character.GetGeometryScale();
            var attribs = new GlyphGeometry.GlyphAttributes
            {
                // Pixels-per-font-unit: packer scale * glyph geometry scale.
                Scale = pixelsPerUnit, // will be overridden per glyph below
                Range = glyphUnitRange + glyphPxRange / pixelsPerUnit, // base; per-glyph addition below
                InnerPadding = glyphInnerPadding + glyphInnerPxPadding / pixelsPerUnit,
                OuterPadding = glyphOuterPadding + glyphOuterPxPadding / pixelsPerUnit,
                MiterLimit = glyphMiterLimit,
                PxAlignOriginX = glyphAlignOriginX,
                PxAlignOriginY = glyphAlignOriginY
            };

            _character.WrapBox(attribs);

            var _glyphBitmap = new Bitmap<float>((int)MSDFLevelData.GlyphSize, (int)MSDFLevelData.GlyphSize, 4);
            MsdfGenerator.GenerateMSDF(_glyphBitmap, _character.GetShape(), _character.GetBoxProjection(), _character.GetBoxRange(), new());

            byte[] _pixels = new byte[MSDFLevelData.GlyphSize * MSDFLevelData.GlyphSize * 4];
            for (int k = 0; k < MSDFLevelData.GlyphSize * MSDFLevelData.GlyphSize; k++)
            {
                _pixels[k * 4 + 0] = (byte)Math.Clamp(_glyphBitmap.Pixels[k * 4 + 0] * 255f, 0, 255);
                _pixels[k * 4 + 1] = (byte)Math.Clamp(_glyphBitmap.Pixels[k * 4 + 1] * 255f, 0, 255);
                _pixels[k * 4 + 2] = (byte)Math.Clamp(_glyphBitmap.Pixels[k * 4 + 2] * 255f, 0, 255);
                _pixels[k * 4 + 3] = 255;
            }

            AddGlyph(_character, _pixels);
        });
    }

    internal bool AddGlyph(GlyphGeometry character, byte[] pixels, int index = -1)
    {
        int _glyphIndex = index == -1 ? createdGlyphs : index;

        int imageX = _glyphIndex % GLYPHD_IN_LINE;
        int imageY = _glyphIndex / GLYPHD_IN_LINE;


        uint visualSize = MSDFLevelData.GlyphSize - MSDFLevelData.Padding * 2;
        if (index == -1)
        {
            //TODO - To implement uv save
            // float _uvWidthPx = Math.Min(glyph.OccupiedWidthPx, visualSize);
            // float _uvHeightPx = Math.Min(glyph.OccupiedHeightPx, visualSize);

            // glyph.UVMin = new Vector2D<float>(
            //     (imageX * MSDFLevelData.GlyphSize + MSDFLevelData.Padding) / (float)MSDFLevelData.AtlasSize,
            //     (imageY * MSDFLevelData.GlyphSize + MSDFLevelData.Padding) / (float)MSDFLevelData.AtlasSize
            // );

            // glyph.UVMax = new Vector2D<float>(
            //     (imageX * MSDFLevelData.GlyphSize + MSDFLevelData.Padding + _uvWidthPx) / (float)MSDFLevelData.AtlasSize,
            //     (imageY * MSDFLevelData.GlyphSize + MSDFLevelData.Padding + _uvHeightPx) / (float)MSDFLevelData.AtlasSize
            // );
            createdGlyphs += 1;
        }

        ulong _offset = stagingBuffer.GetOffset();
        bool _isBlocked = inFlight.Any(r => _offset >= r.startOffset && _offset < r.endOffset);

        if (_isBlocked)
        {
            WaitingCharacters.Enqueue(new()
            {
                character = character.GetCharacter(),
                pixels = pixels,
                index = _glyphIndex
            });
            return false;
        }

        stagingBuffer.UploadPixels(pixels, uploadList, imageX, imageY);

        charactersBuffer.EnqueueCharacter(character);

        // ((CharacterDataGPU*)charactersBuffer.GetBuffer().Mapped)[(uint)character] = new()
        // {
        //     UV = new Vector2D<float>(glyph.UVMin.Y, glyph.UVMax.Y),
        //     Scale = height / glyph.Height,
        //     BearingY = (baseline - glyph.BearingY) / height
        // };

        // Console.WriteLine("Attempting to write on: " + (uint)character + " char: " + character);

        // Console.WriteLine("Glyph for " + character + " ascii " + (uint)character + " is: " + glyph);
        // Console.WriteLine("Scale for " +character + " ascii " + (uint)character +" is: " + height/glyph.Height);
        // Console.WriteLine("BearingY for " +character+" is: " + (glyph.Height-glyph.BearingY)/height);

        return true;
    }

    public unsafe void EndRecording(uint frameInFlight)
    {
        if (uploadList.Count == 0)
            return;

        var _actualCommandBuffer = CmdHelper.BeginSingleTimeCommands(FontAtlasesManager.Instance!.CommandPools[frameInFlight]);

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
        CreateVulkan.vk.CmdPipelineBarrier2(_actualCommandBuffer, &_barrierTexInfo);


        fixed (BufferImageCopy* uploadPtr = uploadList.ToArray())
            CreateVulkan.vk.CmdCopyBufferToImage(_actualCommandBuffer, stagingBuffer.Buffer, atlasImage, ImageLayout.TransferDstOptimal, (uint)uploadList.Count, uploadPtr);

        var _barrierTexRead = ImageHelper.TransitionImageLayout(atlasImage, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

        _barrierTexInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &_barrierTexRead
        };

        CreateVulkan.vk.CmdPipelineBarrier2(_actualCommandBuffer, &_barrierTexInfo);
        CreateVulkan.vk.EndCommandBuffer(_actualCommandBuffer);


        // inFlight.Add(new UploadRegion
        // {
        //     endOffset = ringOffset,
        //     size = (ulong)(BUFFER_GLYPH_SIZE * uploadList.Count),
        //     timelineValue = timelineValue
        // });

        uploadList.Clear();

        FontAtlasesManager.RegisterCommandBuffer(_actualCommandBuffer);

        _atlasInitialized = true;

        // CreateVulkan.vk.WaitForFences(LogicalDevice.device, 1, &fence, Vk.True, ulong.MaxValue);
        // Console.WriteLine("Ended recording " + recordedGlyphs + " glyphs");

    }

    #endregion

    #region Generation functions

    /// <summary>
    /// Sets the distance field range in font units.
    /// </summary>
    public void SetUnitRange(Msdfgen.Range range) { glyphUnitRange = range; }

    /// <summary>
    /// Sets the distance field range in pixels.
    /// </summary>
    public void SetPixelRange(Msdfgen.Range range) { glyphPxRange = range; }

    /// <summary>
    /// Sets the miter limit for glyph boundaries.
    /// </summary>
    public void SetMiterLimit(double val) { glyphMiterLimit = val; }

    /// <summary>
    /// Sets the fixed scale for glyphs.
    /// </summary>
    public void SetScale(double scale) { glyphScale = scale; }


    #endregion
    public unsafe void Dispose()
    {
        charactersBuffer.Dispose();
        stagingBuffer.Dispose();

        if (imageView.Handle != 0)
            CreateVulkan.vk.DestroyImageView(LogicalDevice.device, imageView, null);
        if (atlasImage.Handle != 0)
            CreateVulkan.vk.DestroyImage(LogicalDevice.device, atlasImage, null);
        if (atlasMemory.Handle != 0)
            CreateVulkan.vk.FreeMemory(LogicalDevice.device, atlasMemory, null);
    }
}
