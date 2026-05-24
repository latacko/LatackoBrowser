#language slang 2026
module Common;

public struct CameraUBO
{
    public float4x4 proj;
};

public struct VertexInput
{
    public float3 Position;
    public float2 UV;
}