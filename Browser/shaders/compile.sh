#!/bin/bash
# compile_shaders.sh

SLANGC=/opt/slang/bin/slangc
FLAGS="-target spirv"

$SLANGC uiShader.slang $FLAGS -o Compiled/uiShader.spv && echo "✅ shader compiled" || echo "❌ shader failed"
$SLANGC textShader.slang $FLAGS -o Compiled/textShader.spv && echo "✅ shader compiled" || echo "❌ shader failed"
$SLANGC wireFrameShader.slang $FLAGS -o Compiled/wireFrameShader.spv && echo "✅ shader compiled" || echo "❌ shader failed"