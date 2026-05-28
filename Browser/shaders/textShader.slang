#language slang 2026
import Modules.Common;
import Modules.TextData;

SamplerState sampler;
Texture2D textures[];

struct VertexOutput
{
    float4 Pos : SV_POSITION;
    float2 UV;

    nointerpolation float4 color;

    nointerpolation float2 pos;
    nointerpolation float2 size;

    nointerpolation uint fontIndex;
};

// --- Bindings ---
[shader("vertex")]
VertexOutput VertexMain(VertexInput input, uniform CameraUBO *cameraDataPtr, uniform TextData *textsData, uint instanceIndex: SV_VulkanInstanceID)
{
    VertexOutput output;
    CameraUBO cameraData = *cameraDataPtr;
    TextData textData = textsData[instanceIndex];

    float4x4 mvp = mul(cameraData.proj, textData.model);

    output.Pos = mul(mvp, float4(input.Position, 1));
    output.UV = input.UV;

    output.pos = textData.pos;
    output.size = textData.size;

    output.color = textData.color;

    output.fontIndex = textData.fontIndex;

    return output;
}

struct FragmentOutput
{
    float4 color : SV_Target;
}

float median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}

[shader("fragment")]
FragmentOutput FragmentMain(VertexOutput input)
// FragmentOutput FragmentMain(VertexOutput input, uniform float screenPixelRange)
{

    FragmentOutput output;

    float3 msd = textures[input.fontIndex].Sample(sampler, input.UV).rgb;
    float sd = median(msd.x, msd.y, msd.z);

    float w = fwidth(sd);

    // SHARPEN: Scale down 'w' to make the transition edge much tighter
    float sharpness = 0.5; // Lower values = sharper. 0.0 = completely aliased.
    float width = w * sharpness;

    // Apply the tighter smoothing window
    float opacity = smoothstep(0.5 - width, 0.5 + width, sd);

    output.color = float4(input.color.xyz, opacity);

    // float2 dx = ddx(input.UV);
    // float2 dy = ddy(input.UV);

    // float2 fw = abs(dx) + abs(dy);

    // float2 screenTexSize = 48;

    // float screenPxRange = max(dot(unitRange, screenTexSize * 0.5), 2.0);
    // float screenPxDistance = screenPxRange * (sd - 0.5);

    // float opacity = smoothstep(0.0, 1.0, screenPxDistance + 0.5);
    // output.color = float4(input.color.xyz, opacity);

    return output;
}
