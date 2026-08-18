using System;
using GraphicsCore;
using Silk.NET.Input.Sdl;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Vulkan;

namespace Renderer;

public class RenderEngine : IDisposable
{
    public RenderEngine Instance;
    CreateVulkan createVulkan = new();
    Vulkan.PhysicalDevice physicalDevice;
    LogicalDevice logicalDevice;

    public readonly string[] DeviceExtensions;

    readonly HashSet<BaseShader> shaders = [];

    public RenderEngine(string[] deviceExtensions, KhrSurface khrSurface, SurfaceKHR surfaceKHR)
    {
        if (Instance != null)
            throw new Exception("Only one render engine can exist.");
        Instance = this;
        DeviceExtensions = deviceExtensions;

        logicalDevice = new(DeviceExtensions);
        physicalDevice = new(DeviceExtensions, khrSurface, surfaceKHR);

        Window.PrioritizeSdl();
        Silk.NET.Windowing.Sdl.SdlWindowing.Use();
        SdlInput.RegisterPlatform();
        CursorManager.Init(Silk.NET.SDL.Sdl.GetApi());
    }

    #region Vulkan

    public unsafe void Init(byte** requiredExtensions, uint count)
    {
        createVulkan.Create(requiredExtensions, count);

        physicalDevice.Pick();
        logicalDevice.Create();


        foreach (var shader in shaders)
        {
            shader.Init();
        }
    }

    public void RegisterShader(BaseShader shader)
    {
        shaders.Add(shader);
    }

    public void RegisterSite(SiteRenderer site)
    {
        foreach (var shader in shaders)
        {
            shader.AddSite(site.UniqueId);
        }
    }


    public void Dispose()
    {
        foreach (var shader in shaders)
        {
            shader.Dispose();
        }

        logicalDevice.Dispose();
        createVulkan.Dispose();
    }
    #endregion
}
