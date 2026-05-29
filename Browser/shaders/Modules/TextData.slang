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
    public float4x4 model;
    public float4 color;

    public float2 pos;
    public float2 size;

    public uint fontIndex;
    public float pad0;
    public float pad1;
    public float pad2;
};

public struct CharacterData
{
    public float2 UV;
    public float BearingY;
    public float Scale;
};
