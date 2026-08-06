using System;
using Silk.NET.Vulkan;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace TextCore.Fonts;

public unsafe class AtlasRingBuffer : IDisposable
{
    readonly uint[] glyphBytesSize;
    readonly uint glyphsCount;
    readonly MSDFMipLevelData[] miplevels;
    readonly ulong bufferSize;
    public nint BufferData { get; private set; }

    public Buffer Buffer = new();
    DeviceMemory bufferMemory = new();
    int glyphIndex = 0;

    public AtlasRingBuffer(uint glyphsCount, MSDFMipLevelData[] miplevels)
    {
        glyphBytesSize = new uint[miplevels.Length];
        this.glyphsCount = glyphsCount;
        this.miplevels = miplevels;

        for (int miplevel = 0; miplevel < miplevels.Length; miplevel++)
        {
            glyphBytesSize[miplevel] = miplevels[miplevel].GlyphSize * miplevels[miplevel].GlyphSize * 4;

            bufferSize += glyphsCount * glyphBytesSize[miplevel];
        }
    }

    public void Init()
    {
        BufferHelper.CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref Buffer, ref bufferMemory);
        nint _data;
        CreateVulkan.vk!.MapMemory(LogicalDevice.device, bufferMemory, 0, bufferSize, 0, (void**)&_data);
        BufferData = _data;
    }

    public ulong GetOffset(uint miplevel)
    {
        ulong aligned = 0;

        for (int i = 0; i < miplevel; i++)
        {
            aligned += glyphBytesSize[i] * glyphsCount;
        }

        aligned += (ulong)(glyphBytesSize[miplevel] * glyphIndex);

        return aligned;
    }

    public void UploadPixels(byte[][] pixels, List<BufferImageCopy> uploadList, int imageX, int imageY)
    {
        for (uint miplevel = 0; miplevel < miplevels.Length; miplevel++)
        {
            ulong _offset = GetOffset(miplevel);
            pixels[miplevel].AsSpan().CopyTo(new Span<byte>((void*)(BufferData + (nint)_offset), (int)glyphBytesSize[miplevel]));

            uint _glyphSize = miplevels[miplevel].GlyphSize;

            uploadList.Add(new()
            {
                BufferOffset = _offset,
                BufferRowLength = _glyphSize,
                BufferImageHeight = _glyphSize,

                ImageSubresource = new()
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = miplevel,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                },
                ImageOffset = new((int)(imageX * _glyphSize), (int)(imageY * _glyphSize), 0),
                ImageExtent = new()
                {
                    Width = _glyphSize,
                    Height = _glyphSize,
                    Depth = 1,
                },
            });
        }
        glyphIndex = (int)((glyphIndex + 1) % glyphsCount);
    }

    public void Dispose()
    {
        CreateVulkan.vk!.UnmapMemory(LogicalDevice.device, bufferMemory);
        BufferHelper.DestroyBuffer(Buffer, bufferMemory);
    }
}
