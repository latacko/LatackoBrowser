using System.Runtime.InteropServices;
using Silk.NET.Maths;
namespace Vulkan;

[StructLayout(LayoutKind.Sequential)]
public struct ObjectData
{
    public Matrix4X4<float> Model;
    public Vector4D<float> Color;
    public uint TextureIndex;
    public uint hasTexture;
    public uint hasTexture2;
    public uint pad0;
}