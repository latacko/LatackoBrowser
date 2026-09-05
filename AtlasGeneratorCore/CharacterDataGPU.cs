using System;
using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace AtlasGeneratorCore;

[StructLayout(LayoutKind.Sequential)]
public record struct CharacterDataGPU
{
    public Vector2D<float> UV;
    public float BearingY;
    public float Scale;
}
