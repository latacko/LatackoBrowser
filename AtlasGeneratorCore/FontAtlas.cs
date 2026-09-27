using System;
using MsdfAtlasGen;
using Msdfgen;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Vulkan;
using VulkanManager;
using VulkanManager.BufferManager;

namespace AtlasGeneratorCore;

public class FontAtlas : IDisposable
{
    public uint Id { get; private set; } = 0;
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

    ImageData mainAtlas;
    ImageData stageUploadAtlas;
    readonly HashSet<BufferImageCopy2> uploadList = new();
    readonly HashSet<BufferImageCopy2> prevUploadList = new();


    bool _atlasInitialized = false;

    public readonly Queue<WaitingCharacter> WaitingCharacters = new();

    public CharactersBuffer charactersBuffer;
    IProgress<uint>? progress = null;

    public FontAtlas(uint id, string name, Action<ImageView, uint> registerTextureCB, IProgress<uint>? progress = null)
    {
        this.Id = id;
        this.name = name;
        this.progress = progress;

        charactersBuffer = new();
        ImageHelper.CreateImage(MSDFLevelData.AtlasSize, MSDFLevelData.AtlasSize, Silk.NET.Vulkan.Format.R8G8B8A8Unorm, Silk.NET.Vulkan.ImageTiling.Optimal, Silk.NET.Vulkan.ImageUsageFlags.TransferDstBit | Silk.NET.Vulkan.ImageUsageFlags.SampledBit, Silk.NET.Vulkan.MemoryPropertyFlags.DeviceLocalBit, 1, ref mainAtlas.atlasImage, ref mainAtlas.atlasMemory);
        mainAtlas.imageView = ImageHelper.CreateImageView(mainAtlas.atlasImage, Format.R8G8B8A8Unorm, ImageAspectFlags.ColorBit, 1);

        ImageHelper.CreateImage(MSDFLevelData.AtlasSize, MSDFLevelData.AtlasSize, Silk.NET.Vulkan.Format.R8G8B8A8Unorm, Silk.NET.Vulkan.ImageTiling.Optimal, Silk.NET.Vulkan.ImageUsageFlags.TransferDstBit | Silk.NET.Vulkan.ImageUsageFlags.SampledBit, Silk.NET.Vulkan.MemoryPropertyFlags.DeviceLocalBit, 1, ref stageUploadAtlas.atlasImage, ref stageUploadAtlas.atlasMemory);
        stageUploadAtlas.imageView = ImageHelper.CreateImageView(stageUploadAtlas.atlasImage, Format.R8G8B8A8Unorm, ImageAspectFlags.ColorBit, 1);

        registerTextureCB.Invoke(stageUploadAtlas.imageView, id);
    }

    #region Atlas functions
    public void RecordTransfer(CommandBuffer commandBuffer, BufferData stagingBufferData, ref uint elementHead)
    {
        if (WaitingCharacters.Count > 0)
        {
            uint _remaining = WaitingCharacters.Count > STAGING_BUFFER_GLYPH_COUNT ? STAGING_BUFFER_GLYPH_COUNT : (uint)WaitingCharacters.Count;
            for (int i = 0; i < _remaining; i++)
            {
                var waiting = WaitingCharacters.Peek();

                var _glyphGeometry = Glyphs[waiting.character];
                if (AddGlyph(_glyphGeometry, waiting.pixels, stagingBufferData, ref elementHead))
                {
                    WaitingCharacters.Dequeue();
                    Glyphs[waiting.character] = _glyphGeometry;
                }
                else
                {
                    break;
                }
            }
            EndRecording(commandBuffer, stagingBufferData);
        }

        charactersBuffer.RenderTick(0);
    }

    public void ReportProgress()
    {
        progress?.Report((uint)WaitingCharacters.Count);
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

            WaitingCharacters.Enqueue(new()
            {
                character = _character.GetCharacter(),
                pixels = _pixels,
                index = createdGlyphs
            });
        });
    }

    unsafe bool AddGlyph(GlyphGeometry character, byte[] pixels, BufferData stagingBufferData, ref uint elementHead)
    {
        int _glyphIndex = createdGlyphs;
        createdGlyphs++;

        int imageX = _glyphIndex % GLYPHD_IN_LINE;
        int imageY = _glyphIndex / GLYPHD_IN_LINE;


        uint visualSize = MSDFLevelData.GlyphSize - MSDFLevelData.Padding * 2;
        // if (index == -1)
        // {
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
        // }

        uploadList.Add(new()
        {
            BufferOffset = elementHead,
            BufferRowLength = MSDFLevelData.GlyphSize,
            BufferImageHeight = MSDFLevelData.GlyphSize,

            ImageSubresource = new()
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
            ImageOffset = new((int)(imageX * MSDFLevelData.GlyphSize), (int)(imageY * MSDFLevelData.GlyphSize), 0),
            ImageExtent = new()
            {
                Width = MSDFLevelData.GlyphSize,
                Height = MSDFLevelData.GlyphSize,
                Depth = 1,
            },
        });

        pixels.AsSpan().CopyTo(new Span<byte>((void*)((nint)stagingBufferData.Mapped + (nint)elementHead), (int)(MSDFLevelData.GlyphSize * MSDFLevelData.GlyphSize * 4)));

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

    public ImageMemoryBarrier2 GetTransferImageBarrier()
    {
        var srcLayout = !_atlasInitialized
            ? ImageLayout.Undefined
            : ImageLayout.ShaderReadOnlyOptimal;
        return ImageHelper.TransitionImageLayout(stageUploadAtlas.atlasImage, srcLayout, ImageLayout.TransferDstOptimal);
    }

    public ImageMemoryBarrier2 GetShaderOptimalImageBarrier()
    {
        return ImageHelper.TransitionImageLayout(stageUploadAtlas.atlasImage, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
    }

    unsafe void EndRecording(CommandBuffer commandBuffer, BufferData stagingBufferData)
    {
        BufferImageCopy2* _uploadRegions = stackalloc BufferImageCopy2[uploadList.Count + prevUploadList.Count];
        int i = 0;
        foreach (var item in prevUploadList)
        {
            _uploadRegions[i] = item;
            i++;
        }
        prevUploadList.Clear();

        foreach (var item in uploadList)
        {
            _uploadRegions[i] = item;
            prevUploadList.Add(item);
            i++;
        }

        uploadList.Clear();

        CopyBufferToImageInfo2 _copyBufferToImageInfo = new()
        {
            SrcBuffer = stagingBufferData.Buffer,
            DstImage = stageUploadAtlas.atlasImage,

            PRegions = _uploadRegions,
            RegionCount = (uint)(uploadList.Count + prevUploadList.Count),
            DstImageLayout = ImageLayout.TransferDstOptimal,
        };

        CreateVulkan.vk.CmdCopyBufferToImage2(commandBuffer, in _copyBufferToImageInfo);


        // inFlight.Add(new UploadRegion
        // {
        //     endOffset = ringOffset,
        //     size = (ulong)(BUFFER_GLYPH_SIZE * uploadList.Count),
        //     timelineValue = timelineValue
        // });

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

    #region Getters
    public GlyphGeometry GetGlyph(char character)
    {
        return Glyphs[character];
    }
    #endregion
    public void Dispose()
    {
        charactersBuffer.Dispose();

        mainAtlas.Dispose();
        stageUploadAtlas.Dispose();
    }
}
