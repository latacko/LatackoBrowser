#version 450

#extension GL_GOOGLE_include_directive : require
#include "common.glsl"

layout(set = 1, binding = 0) uniform sampler2D uiTexture;

layout(location = 0) in vec2 fragUV;

layout(location = 0) out vec4 outColor;

void main() {
    ObjectData obj = objects[pc.objectIndex];

    if (obj.hasTexture == 1){
        outColor = texture(uiTexture, fragUV);
    } else {
        outColor = obj.color;
    }
}