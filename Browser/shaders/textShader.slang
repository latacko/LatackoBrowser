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
    nointerpolation uint char;
};

public struct PushConstants
{
    public CameraUBO *cameraBuffer;
    public TextData *textsData;
    public ModelData *modelsData;
    public CharacterData *characterDataPtr;
    public uint TextIndex;
    public uint ShowMSDF;
    public uint ShowMipLevel;
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

float IsChar(uint ascii)
{
    return ascii == 84 ? 1 : 0;
}

static uint FLAG_FLIP_IF_END_CHAR = 1u << 31;

static S_MSDFMipLevelData MSDFMipLevelData[3] = {
    { 1024, 64, 6, 8, 0.1 },
    { 512, 32, 4, 4, 0.4 },
    { 256, 16, 3, 2, 0.4 },
};

S_MSDFMipLevelData GetMSDFData(VertexOutput input, float2 uv) {
    S_MSDFMipLevelData msdfMipLevelData;
    float mipLevel = textures[input.fontIndex].CalculateLevelOfDetail(sampler, uv);


    int lowMip = (int)floor(mipLevel);

    return MSDFMipLevelData[lowMip];
}

[shader("fragment")]
FragmentOutput FragmentMain(VertexOutput input)
{
    FragmentOutput output;

    // if (input.char != uint(input.char)) {
    //     output.color = float4(0, 0, 1, 1);
    //     return output;
    // }

    bool isGapQuad = (input.char &FLAG_FLIP_IF_END_CHAR) != 0;

    if (isGapQuad)
    {
        output.color = float4(0, 0, 1, 0); // blank space, no glyph to sample
        return output;
    }

    uint clearedChar = input.char &~FLAG_FLIP_IF_END_CHAR;
    CharacterData characterData = pc.characterDataPtr[clearedChar];

    float2 scaledUV = input.UV;

    if (pc.ShowMipLevel == 1)
    {
        float mipLevel = textures[input.fontIndex].CalculateLevelOfDetail(sampler, scaledUV);

        // // color-code by mip: 0=red, 1=green, 2=blue, blend between
        float3 mipColors[4] = {
            float3(1, 0, 0), // mip 0
            float3(0, 1, 0), // mip 1
            float3(0, 0, 1), // mip 2
            float3(1, 1, 0), // mip 3 (if it existed)
        };

        int lowMip = (int)floor(mipLevel);
        int highMip = min(lowMip + 1, 3);
        float frac = mipLevel - lowMip;

        output.color = float4(lerp(mipColors[lowMip], mipColors[highMip], frac), 1);
        return output;
    }

    float t = (input.UV.y - characterData.UV.x) / (characterData.UV.y - characterData.UV.x);
    t -= characterData.BearingY;
    t *= characterData.Scale;

    S_MSDFMipLevelData msdfMipLevelData = GetMSDFData(input, input.UV);

    if (t < -msdfMipLevelData.CharMargin || t > 1 + msdfMipLevelData.CharMargin)
    {
        output.color = float4(1, 0, 0, 0.2);
        return output;
    }

    float mipLevel = textures[input.fontIndex].CalculateLevelOfDetail(sampler, input.UV);

    scaledUV.y = lerp(characterData.UV.x, characterData.UV.y, t);


    int lowMip = (int)floor(mipLevel);

    float3 msd = textures[input.fontIndex].SampleLevel(sampler, scaledUV, lowMip).rgb;
    // float3 msd = textures[input.fontIndex].Sample(sampler, scaledUV).rgb;

    /// ==================  Poprzedni dobrze działający msdf (ale gorzej z wykrzyknikami)  ==================
    float msdf = median(msd.x, msd.y, msd.z);
    // float2 fwidthUV = float2(
    //     fwidth(input.UV.x),
    //     fwidth(input.UV.y) * characterData.Scale // korekta skali Y
    // );

    // float2 screenTexSize = 1.0 / fwidthUV;

    float2 screenTexSize = 1.0 / max(fwidth(input.UV), 1e-6);

    // float2 screenTexSize = 1.0 / fwidth(scaledUV);

    float2 unitRange = float2(msdfMipLevelData.Range / msdfMipLevelData.AtlasSize, msdfMipLevelData.Range / msdfMipLevelData.AtlasSize);
    float screenPxRange = max(0.5f * dot(unitRange, screenTexSize), 1.0f);
    float screenPxDistance = screenPxRange * (msdf - 0.5f);

    float opacity = clamp(screenPxDistance + 0.5f, 0.0f, 1.0f);

    if (pc.ShowMSDF == 1)
        output.color = float4(msd, 1);
    else
        output.color = float4(input.color.xyz, opacity);

    return output;
}
