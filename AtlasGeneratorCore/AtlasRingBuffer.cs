using System;
using Silk.NET.Vulkan;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace AtlasGeneratorCore;

public unsafe class AtlasRingBuffer : IDisposable
{
    readonly MSDFLevelData levelData;
    readonly uint glyphByteSize;
    readonly uint glyphsCount;
    readonly ulong bufferSize;
    public nint BufferData { get; private set; }

    public Buffer Buffer = new();
    DeviceMemory bufferMemory = new();
    int glyphIndex = 0;

    public AtlasRingBuffer(uint glyphsCount, MSDFLevelData levelData)
    {
        this.levelData = levelData;
        this.glyphsCount = glyphsCount;
        glyphByteSize = levelData.GlyphSize * levelData.GlyphSize * 4;

        bufferSize += glyphsCount * glyphByteSize;
    }

    public void Init()
    {
        BufferHelper.CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref Buffer, ref bufferMemory);
        nint _data;
        CreateVulkan.vk!.MapMemory(LogicalDevice.device, bufferMemory, 0, bufferSize, 0, (void**)&_data);
        BufferData = _data;
    }

    public ulong GetOffset()
    {
        ulong aligned = (ulong)glyphIndex * glyphByteSize;

        return aligned;
    }

    public void UploadPixels(byte[] pixels, List<BufferImageCopy> uploadList, int imageX, int imageY)
    {
        ulong _offset = GetOffset();
        pixels.AsSpan().CopyTo(new Span<byte>((void*)(BufferData + (nint)_offset), (int)glyphByteSize));

        uint _glyphSize = levelData.GlyphSize;

        uploadList.Add(new(
            bufferOffset: _offset,
            bufferRowLength: _glyphSize,
            bufferImageHeight: _glyphSize,

            imageSubresource: new(
                aspectMask: ImageAspectFlags.ColorBit,
                mipLevel: 0,
                baseArrayLayer: 0,
                layerCount: 1
            ),
            imageOffset: new((int)(imageX * _glyphSize), (int)(imageY * _glyphSize), 0),
            imageExtent: new(
                width: _glyphSize,
                height: _glyphSize,
                depth: 1
            )
        ));
        glyphIndex = (int)((glyphIndex + 1) % glyphsCount);
    }

    public void Dispose()
    {
        CreateVulkan.vk!.UnmapMemory(LogicalDevice.device, bufferMemory);
        BufferHelper.DestroyBuffer(Buffer, bufferMemory);
    }
}
