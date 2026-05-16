#!/bin/bash
# compile_shaders.sh

SLANGC=/opt/slang/bin/slangc
FLAGS="-target spirv"

$SLANGC uiShader.slang $FLAGS -o uiShader.spv && echo "✅ shader compiled" || echo "❌ shader failed"