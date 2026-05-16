using Silk.NET.Maths;

public struct ObjectData
{
    public Matrix4X4<float> Model;
    public Vector4D<float> Color;
    public uint TextureIndex;
    public uint hasTexture2;
    public uint pad0;
    public uint pad1;
}