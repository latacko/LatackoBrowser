#language slang 2026
module TextData;

public struct VertexInput
{
    public float3 Position;
    public float2 UV;
    public uint Char;
}

public struct TextData
{
    public float4 color;

    public uint fontIndex;
    public uint range;
    public float atlas_size;
    public float pad2;
};

public struct ModelData
{
    public float4x4 model;
};

public struct CharacterData
{
    public float2 UV;
    public float BearingY;
    public float Scale;
};

public struct S_MSDFMipLevelData
{
    public uint AtlasSize;
    public uint GlyphSize;
    public float Range;
    public uint Padding;
    public float CharMargin;
};