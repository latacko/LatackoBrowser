using System;
using Silk.NET.Vulkan;
using Vulkan;

namespace PrimitiveCore.Textures;

public class Texture : IDisposable
{
    internal uint id;
    internal readonly string path;
    internal bool ready;

    internal ushort width;
    internal ushort height;
    internal byte bitsPerPixel;
    internal ulong imgSize=>(ulong)(width * height * bitsPerPixel / 8);
    

    internal Image Image = default;
    internal DeviceMemory Memory = default;
    internal ImageView imageView;

    public uint GetID() => id;

    public Texture(string path)
    {
        this.path = path;
    }

    public void Dispose()
    {
        ImageHelper.DestroyTexture(Image, Memory, imageView);
    }
}
