using System;
using Silk.NET.Vulkan;
using Vulkan;

namespace PrimitiveCore.Textures;

public class Texture : IDisposable
{
    internal uint id;
    internal Image Image = default;
    internal DeviceMemory Memory = default;
    internal ImageView imageView;

    public uint GetID() => id;

    public void Dispose()
    {
        ImageHelper.DestroyTexture(Image, Memory, imageView);
    }
}
