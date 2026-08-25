using System;
using GraphicsCore;
using GraphicsCore.Shaders;
using Silk.NET.Input.Sdl;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Vulkan;
using VulkanManager.BufferManager;

namespace Renderer;

public class RenderEngine : IDisposable
{
    public static RenderEngine? Instance;
    CreateVulkan createVulkan = new();
    Vulkan.PhysicalDevice physicalDevice;
    LogicalDevice logicalDevice;

    public readonly string[] DeviceExtensions;

    readonly HashSet<BaseShader> shaders = [];
    readonly HashSet<ElementsManager> uniqueObjectsManagers = [];

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
        uniqueObjectsManagers.Add(shader.objectsManager);
        shaders.Add(shader);
    }

    public void RegisterSite(SiteRenderer site)
    {
        foreach (var objectsManager in uniqueObjectsManagers)
        {
            objectsManager.RegisterBuffers(site.GetId());
        }

        foreach (var shader in shaders)
        {
            shader.AddSite(site);
        }

    }


    public void Dispose()
    {
        foreach (var shader in shaders)
        {
            shader.Dispose();
        }

        foreach (var objectsManager in uniqueObjectsManagers)
        {
            objectsManager.Dispose();
        }

        logicalDevice.Dispose();
        createVulkan.Dispose();
    }
    #endregion
}
