using System;
using Vulkan;

namespace Renderer;

public struct FPSData
{
    public double fpsTimer;
    public int fpsFrames;
    public double worstFrameMs;
}
