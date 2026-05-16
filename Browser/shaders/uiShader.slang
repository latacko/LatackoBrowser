#language slang 2026
module UIShader;
__include Common;

struct VertexInput {
    [vk_location(0)] float3 Position;
    [vk_location(1)] float2 UV;
}

SamplerState sampler;
Texture2D textures[];

struct VertexOutput {
    float4 Pos : SV_POSITION;
    float2 UV;
    uint32_t InstanceIndex;
};

// --- Bindings ---
[vk_binding(0, 0)] ConstantBuffer<CameraUBO> camera;

[vk_binding(0, 1)] Sampler2D uiTexture;

[vk_binding(0, 2)] StructuredBuffer<ObjectData> objects;

[vk_push_constant] PushConstants pc;

[shader("vertex")]
VertexOutput VertexMain(VertexInput input, uint instanceIndex: SV_VulkanInstanceID) {
    VertexOutput output;
    output.InstanceIndex = instanceIndex;

    ObjectData obj = objects[pc.objectIndex];

    float4x4 mvp = mul(camera.proj, obj.model);

    output.Pos = mul(mvp, float4(input.Position, 1));
    output.UV = input.UV;

    return output;
}

struct FragmentOutput {
    [vk_location(0)] float4 color : SV_Target;
}

[shader("fragment")]
FragmentOutput FragmentMain(VertexOutput input) {
    FragmentOutput output;

    ObjectData obj = objects[input.InstanceIndex];

    if (obj.hasTexture == 1) {
        output.color = textures[obj.textureIndex].Sample(sampler, input.UV);
    } else {
        output.color = obj.color;
    }

    return output;
}