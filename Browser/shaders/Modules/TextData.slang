#language slang 2026
module TextData;

public struct TextData
{
    public float4x4 model;
    public float4 color;

    public float2 pos;
    public float2 size;

    public uint fontIndex;
    public uint pad0;
    public uint pad1;
    public uint pad2;
};
