using System;
using Vulkan;

namespace Renderer;

public struct FPSData
{
    public uint[] FPS;
    internal uint currentFrame;

    internal double _fpsTimer;
}
