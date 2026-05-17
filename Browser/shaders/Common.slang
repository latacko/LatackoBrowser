#language slang 2026
implementing uiShader;

struct CameraUBO
{
    float4x4 proj;
};

struct ObjectData
{
    float4x4 model;
    float4 color;

    float2 pos;
    float2 size;

    uint textureIndex;
    uint borderRadiusTopLeft;
    uint borderRadiusTopRight;
    uint borderRadiusBottomRight;
    uint borderRadiusBottomLeft;
    uint pad0;
    uint pad1;
    uint pad2;
};
struct PushConstants
{
    uint objectIndex;
};

float RoundedRectSDF(float2 pixelPos, float2 rectPos, float2 rectSize, float4 radii)
{
    // radii: x=topLeft, y=topRight, z=bottomRight, w=bottomLeft

    float2 center = rectPos + rectSize * 0.5;
    float2 halfSize = rectSize * 0.5;
    float2 p = pixelPos - center;

    // Pick radius based on quadrant
    float r = p.x > 0
                  ? (p.y > 0 ? radii.z : radii.y)  // right side
                  : (p.y > 0 ? radii.w : radii.x); // left side

    float2 q = abs(p) - halfSize + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}
