#language slang 2026
module TextData;

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
