#!/bin/bash
# compile_shaders.sh

SLANGC=/opt/slang/bin/slangc
FLAGS="-target glsl"

$SLANGC uiShader.slang $FLAGS -entry VertexMain -stage vertex -o uiShader.vert && echo "✅ shader compiled" || echo "❌ shader failed"
$SLANGC uiShader.slang $FLAGS -entry FragmentMain -stage fragment -o uiShader.frag && echo "✅ shader compiled" || echo "❌ shader failed"