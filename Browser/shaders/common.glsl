struct ObjectData
{
    mat4 model;
    vec4 color;

    uint hasTexture;
    uint hasTexture2;
    uint pad0;
    uint pad1;
};

layout(push_constant) uniform PushConstants
{
    uint objectIndex;
} pc;


layout(std430, set = 2, binding = 0) readonly buffer ObjectBuffer
{
    ObjectData objects[];
};