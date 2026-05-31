using System;
using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace TextCore;

[StructLayout(LayoutKind.Sequential)]
public record struct ModelData
{
    public Matrix4X4<float> Model; //64
}