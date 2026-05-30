using System;
using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace TextCore;

[StructLayout(LayoutKind.Sequential)]
public record struct TextContainerData
{
    public Vector4D<float> Color; //16
    
    // -------- 16 start ---------
    public uint TextureIndex; // 4
    public uint pad0; // 4
    public uint pad1; // 4
    public uint pad2; // 4
    // -------- 16 end ---------
}
