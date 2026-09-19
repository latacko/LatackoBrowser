using System;
using Vulkan;

namespace Renderer;

public struct FPSData
{
    public uint[] FPS;
    internal uint frameInFlight;

    internal double _fpsTimer;
}
