#!/bin/bash
# compile_shaders.sh

GLSLC=~/Dokumenty/vulkanSDK/x86_64/bin/glslc
FLAGS="-I. -O0 -g --target-env=vulkan1.2"

$GLSLC $FLAGS shader.vert -o vert.spv && echo "✅ vert compiled" || echo "❌ vert failed"
$GLSLC $FLAGS shader.frag -o frag.spv && echo "✅ frag compiled" || echo "❌ frag failed"