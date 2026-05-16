#language slang 2026
implementing uiShader;

struct CameraUBO
{
    float4x4 proj;
};

struct ObjectData
{
    float4x4 model;
    RenderData renderData;
};

struct RenderData
{
    float4 color;
    uint textureIndex;

    uint hasTexture;
    uint hasTexture2;
    uint pad0;
}

struct PushConstants
{
    uint objectIndex;
};
