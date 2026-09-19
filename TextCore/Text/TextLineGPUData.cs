using System;
using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace TextCore.Text;

[StructLayout(LayoutKind.Sequential)]
public record struct TextLineGPUData
{
    public Matrix4X4<float> Model; //64
}