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

    nointerpolation uint fontIndex;
    float char;
};

public struct PushConstants
{
    public CameraUBO *cameraBuffer;
    public TextData *textsData;
    public ModelData *modelsData;
    public CharacterData *characterDataPtr;
    public uint TextIndex;
};

[push_constant]
PushConstants pc;

// --- Bindings ---
[shader("vertex")]
VertexOutput VertexMain(VertexInput input, uint instanceIndex: SV_VulkanInstanceID)
{
    VertexOutput output;
    CameraUBO cameraData = *pc.cameraBuffer;
    TextData textData = pc.textsData[pc.TextIndex];
    ModelData modelData = pc.modelsData[instanceIndex];

    float4x4 mvp = mul(cameraData.proj, modelData.model);

    output.Pos = mul(mvp, float4(input.Position, 1));
    output.UV = input.UV;

    output.color = textData.color;

    output.fontIndex = textData.fontIndex;
    output.char = input.Char;

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

float IsChar(uint ascii) {
    return ascii == 84 ? 1 : 0;
}

#define ATLAS_WIDTH 1024.0
#define ATLAS_HEIGHT 1024.0
#define MSDF_RANGE 6.0

// [shader("fragment")]
// FragmentOutput FragmentMain(VertexOutput input)
// {

//     FragmentOutput output;

//     CharacterData characterData = pc.characterDataPtr[input.char];

//     float2 scaledUV = input.UV;

//     float t = (input.UV.y - characterData.UV.x) / (characterData.UV.y - characterData.UV.x);
//     t -= characterData.BearingY;
//     t *= characterData.Scale;

//     if (t < 0 || t > 1)
//     {
//         output.color = float4(0, 0, 0, 0);
//         return output;
//     }

//     scaledUV.y = lerp(characterData.UV.x, characterData.UV.y, t);

//     float3 msd = textures[input.fontIndex].Sample(sampler, scaledUV).rgb;
//     float sd = median(msd.x, msd.y, msd.z);

//     float2 uvPerPixel = fwidth(scaledUV);

//     float w = fwidth(sd);

//     // SHARPEN: Scale down 'w' to make the transition edge much tighter
//     float sharpness = 0.5; // Lower values = sharper. 0.0 = completely aliased.
//     float width = w * sharpness;

//     // Apply the tighter smoothing window
//     float opacity = smoothstep(0.5 - width, 0.5 + width, sd);

//     output.color = float4(input.color.xyz, opacity);

//     return output;
// }

[shader("fragment")]
FragmentOutput FragmentMain(VertexOutput input)
{
    FragmentOutput output;

    if (input.char != uint(input.char)) {
        output.color = float4(0, 0, 1, 0);
        return output;
    }


    CharacterData characterData = pc.characterDataPtr[uint(input.char)];

    float2 scaledUV = input.UV;

    float t = (input.UV.y - characterData.UV.x) / (characterData.UV.y - characterData.UV.x);
    t -= characterData.BearingY;
    t *= characterData.Scale;

    if (t < -0.01 || t > 1)
    {
        output.color = float4(1, 0, 0, 0);
        return output;
    }

    scaledUV.y = lerp(characterData.UV.x, characterData.UV.y, t);

    float3 msd = textures[input.fontIndex].Sample(sampler, scaledUV).rgb;
    float msdf = median(msd.x, msd.y, msd.z);

    float2 fwidthUV = float2(
        fwidth(input.UV.x),
        fwidth(input.UV.y) * characterData.Scale // korekta skali Y
    );

    float2 screenTexSize = 1.0 / fwidthUV;

    float2 unitRange = float2(MSDF_RANGE / ATLAS_WIDTH, MSDF_RANGE / ATLAS_HEIGHT);
    float screenPxRange = max(0.5f * dot(unitRange, screenTexSize), 1.0f);
    float screenPxDistance = screenPxRange * (msdf - 0.5f);

    float opacity = clamp(screenPxDistance + 0.5f, 0.0f, 1.0f);

    output.color = float4(input.color.xyz, opacity);

    return output;
}