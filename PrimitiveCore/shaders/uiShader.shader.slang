#language slang 2026
import Modules.Common;
import Modules.UIData;

SamplerState sampler;
Texture2D textures[];

struct VertexOutput
{
    float4 Pos : SV_POSITION;
    float2 UV;

    nointerpolation float4 color;

    nointerpolation float2 pos;
    nointerpolation float2 size;

    nointerpolation uint textureIndex;
    nointerpolation float borderRadiusTopLeft;
    nointerpolation float borderRadiusTopRight;
    nointerpolation float borderRadiusBottomRight;
    nointerpolation float borderRadiusBottomLeft;
};

// --- Bindings ---
[shader("vertex")]
VertexOutput VertexMain(VertexInput input, uniform CameraUBO *cameraDataPtr, uniform ObjectData *objectsData, uint instanceIndex: SV_VulkanInstanceID)
{
    VertexOutput output;
    CameraUBO cameraData = *cameraDataPtr;
    ObjectData objData = objectsData[instanceIndex];

    float4x4 mvp = mul(cameraData.proj, objData.model);

    output.Pos = mul(mvp, float4(input.Position, 1));
    output.UV = input.UV;

    output.pos = objData.pos;
    output.size = objData.size;

    output.color = objData.color;

    output.textureIndex = objData.textureIndex;
    output.borderRadiusTopLeft = objData.borderRadiusTopLeft;
    output.borderRadiusTopRight = objData.borderRadiusTopRight;
    output.borderRadiusBottomRight = objData.borderRadiusBottomRight;
    output.borderRadiusBottomLeft = objData.borderRadiusBottomLeft;

    return output;
}

struct FragmentOutput
{
    float4 color : SV_Target;
}

[shader("fragment")]
FragmentOutput FragmentMain(VertexOutput input)
{
    FragmentOutput output;

    // RenderData objData = input;

    float4 radii = float4(
        input.borderRadiusTopLeft,
        input.borderRadiusTopRight,
        input.borderRadiusBottomRight,
        input.borderRadiusBottomLeft);

    float dist = RoundedRectSDF(input.UV, input.size, radii);
    // Smooth edge — anti-aliased
    float alpha = 1.0 - smoothstep(-0.5, 0.5, dist);

    if (alpha <= 0.0)
        discard;

    // float alpha = 1;

    float4 baseColor;
    if (input.textureIndex != uint.maxValue)
    {
        baseColor = textures[input.textureIndex].Sample(sampler, input.UV);
    }
    else
    {
        baseColor = input.color;
    }

    output.color = float4(baseColor.rgb, baseColor.a * alpha);

    return output;
}
