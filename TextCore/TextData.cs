using System;
using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace TextCore;

[StructLayout(LayoutKind.Sequential)]
public struct TextData
{
    public Matrix4X4<float> Model; //64
    public Vector4D<float> Color; //16

    // -------- 16 start ---------
    public Vector2D<float> pos; // 8
    public Vector2D<float> size; // 8
    // -------- 16 end ---------

    // -------- 16 start ---------
    public uint TextureIndex; // 4
    public uint pad0; // 4
    public uint pad1; // 4
    public uint pad2; // 4
    // -------- 16 end ---------
}
