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
    nointerpolation uint char;
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

float IsChar(uint8_t ascii) {
    return ascii == 65 ? 1 : 0;
}

[shader("fragment")]
FragmentOutput FragmentMain(VertexOutput input, uniform uint64_t _pad0, uniform uint64_t _pad1, uniform CharacterData *characterDataPtr)
// FragmentOutput FragmentMain(VertexOutput input, uniform float screenPixelRange)
{

    FragmentOutput output;

    CharacterData characterData = characterDataPtr[input.char];

    float2 scaledUV = input.UV;

    float t = (input.UV.y - characterData.UV.x) / (characterData.UV.y - characterData.UV.x);
    t -= characterData.BearingY;
    t *= characterData.Scale;

    scaledUV.y = lerp(characterData.UV.x, characterData.UV.y, t);

    // output.color = float4(t / 1, 0, 0, 1);
    // return output;

    // output.color = float4(characterData.Scale/2, 0, 0, 1);
    // return output;
    // scaledUV.y *= characterData.Scale;
    // scaledUV.y += characterData.BearingY;
    // scaledUV.y = (input.UV.y - characterData.BearingY) * characterData.Scale + characterData.UV.x;

    // float glyphStart = characterData.BearingY;                 // where glyph starts in 0..1 quad space
    // float glyphEnd = glyphStart + (1.0 / characterData.Scale); // where glyph ends

    // remap so glyph portion maps to full atlas UV range
    // float t = (input.UV.y - glyphStart) * characterData.Scale;

    // then map t (0..1) to atlas UV range
    // scaledUV.y = lerp(characterData.UV.x, characterData.UV.y, t);

    // float glyphStart = characterData.BearingY;
    // float glyphEnd = glyphStart + (1.0 / characterData.Scale);
    // float t = (input.UV.y - glyphStart) * characterData.Scale;
    // float sampledY = lerp(characterData.UV.x, characterData.UV.y, t);

    // output.color = float4(characterData.Scale/2, 0, 0, 1);
    // return output;

    float3 msd = textures[input.fontIndex].Sample(sampler, scaledUV).rgb;
    float sd = median(msd.x, msd.y, msd.z);

    float w = fwidth(sd);

    // SHARPEN: Scale down 'w' to make the transition edge much tighter
    float sharpness = 0.5; // Lower values = sharper. 0.0 = completely aliased.
    float width = w * sharpness;

    // Apply the tighter smoothing window
    float opacity = smoothstep(0.5 - width, 0.5 + width, sd);

    output.color = float4(input.color.xyz, opacity);

    // output.color = float4(IsChar(input.char), 0,0, opacity);

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
