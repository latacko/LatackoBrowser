#language slang 2026
module UIShader;
__include Common;

struct VertexInput {
    float3 Position;
    float2 UV;
}

SamplerState sampler;
Texture2D textures[];

struct VertexOutput {
    float4 Pos : SV_POSITION;
    float2 UV;
    RenderData RenderData;
};

// --- Bindings ---
[shader("vertex")]
VertexOutput VertexMain(VertexInput input, uniform CameraUBO *cameraDataPtr, uniform ObjectData *objectsData, uint instanceIndex: SV_VulkanInstanceID) {
    VertexOutput output;
    CameraUBO cameraData = *cameraDataPtr;
    ObjectData objData = objectsData[instanceIndex];

    float4x4 mvp = mul(cameraData.proj, objData.model);

    output.Pos = mul(mvp, float4(input.Position, 1));
    output.UV = input.UV;
    output.RenderData = objData.renderData;

    return output;
}

struct FragmentOutput {
    float4 color : SV_Target;
}

[shader("fragment")]
FragmentOutput FragmentMain(VertexOutput input) {
    FragmentOutput output;
    RenderData objData = input.RenderData;

    if (objData.hasTexture == 1) {
        output.color = textures[objData.textureIndex].Sample(sampler, input.UV);
    } else {
        output.color = objData.color;
    }

    return output;
}