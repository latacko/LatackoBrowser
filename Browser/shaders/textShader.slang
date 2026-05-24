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

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}


[shader("fragment")]
FragmentOutput FragmentMain(VertexOutput input, uniform float screenPixelRange)
{
    FragmentOutput output;
    // RenderData objData = input;

    float3 msd = textures[input.fontIndex].Sample(sampler, input.UV).rgb;
    float sd = median(msd.x, msd.y, msd.z);
    float screenPxDistance = screenPixelRange * (sd - 0.5);
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);

    output.color = float4(input.color.rgb, opacity);

    return output;
}
