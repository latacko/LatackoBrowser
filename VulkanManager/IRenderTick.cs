using System;

namespace VulkanManager;

public interface IRenderTick
{
    public void RenderTick(uint frameInFlight);
}
