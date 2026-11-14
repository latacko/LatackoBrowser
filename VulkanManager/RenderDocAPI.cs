using System;

namespace Vulkan;

public static class RenderDocAPI
{
    public static void StartCapture()
    {
        RenderDocIntegration.RenderDocApi.StartCapture(CreateVulkan.vulkanInstance.Handle, IntPtr.Zero);
    }

    public static void EndCapture()
    {
        RenderDocIntegration.RenderDocApi.EndCapture(CreateVulkan.vulkanInstance.Handle, IntPtr.Zero);
    }

    public static void TriggerCapture()
    {
        RenderDocIntegration.RenderDocApi.TriggerCapture();
    }
}
