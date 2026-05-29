#language slang 2026
module UIData;

public struct VertexInput
{
    public float3 Position;
    public float2 UV;
}

public struct ObjectData
{
    public float4x4 model;
    public float4 color;

    public float2 pos;
    public float2 size;

    public uint textureIndex;
    public float borderRadiusTopLeft;
    public float borderRadiusTopRight;
    public float borderRadiusBottomRight;
    public float borderRadiusBottomLeft;
    public uint pad0;
    public uint pad1;
    public uint pad2;
};

public struct PushConstants
{
    public uint objectIndex;
};

public float RoundedRectSDF(float2 uv, float2 rectSize, float4 radii)
{
    float2 p = (uv - 0.5) * rectSize; // uv [0,1] -> pixel space centered at 0
    float r = p.x > 0
                ? (p.y > 0 ? radii.z : radii.y)
                : (p.y > 0 ? radii.w : radii.x);
    float2 q = abs(p) - rectSize * 0.5 + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}
