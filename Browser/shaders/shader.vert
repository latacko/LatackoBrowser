#version 450

#extension GL_GOOGLE_include_directive : require
#include "common.glsl"

// Set 0: Camera (projection)
layout(set = 0, binding = 0) uniform CameraUBO
{
    mat4 proj;
} camera;

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec2 inUV;

layout(location = 0) out vec2 fragUV;

void main() {
    ObjectData obj = objects[pc.objectIndex];

    mat4 mvp = camera.proj * obj.model;

    gl_Position = mvp * vec4(inPosition, 1.0);

    fragUV = inUV;
}