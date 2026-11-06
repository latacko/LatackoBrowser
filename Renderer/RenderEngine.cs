using System;
using GraphicsCore;
using GraphicsCore.Buffers;
using GraphicsCore.Shaders;
using InstanceFinderCore;
using PrimitiveCore;
using Silk.NET.Input.Sdl;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using TextCore;
using Vulkan;
using VulkanManager;
using VulkanManager.BufferManager;

namespace Renderer;

public class RenderEngine : IDisposable
{
    public static RenderEngine? Instance;
    CreateVulkan createVulkan = new();
    Vulkan.PhysicalDevice physicalDevice;
    LogicalDevice logicalDevice;

    public readonly string[] DeviceExtensions;

    public readonly HashSet<BaseShader> Shaders = [];
    public readonly HashSet<TextShader> TextShaders = [];

    public RenderEngine(string[] deviceExtensions)
    {
        if (Instance != null)
            throw new Exception("Only one render engine can exist.");
        Instance = this;
        DeviceExtensions = deviceExtensions;
        // InstanceAutoLoad.InstancesBootstrap.CreateInstancessBootstrap.CreateInstances();

        Window.PrioritizeSdl();
        Silk.NET.Windowing.Sdl.SdlWindowing.Use();
        SdlInput.RegisterPlatform();
        CursorManager.Init(Silk.NET.SDL.Sdl.GetApi());
    }

    #region Vulkan

    public unsafe void Init(byte** requiredExtensions, uint count)
    {
        createVulkan.Create(requiredExtensions, count);
    }

    public void PickDevice(KhrSurface khrSurface, SurfaceKHR surfaceKHR)
    {
        logicalDevice = new(DeviceExtensions);
        physicalDevice = new(DeviceExtensions, khrSurface, surfaceKHR);

        physicalDevice.Pick();
        logicalDevice.Create();
    }

    public void RegisterShaders(params BaseShader[] shaders)
    {
        foreach (var shader in shaders)
        {
            if (shader is TextShader textShader)
            {
                TextShaders.Add(textShader);
            }
            if (this.Shaders.Add(shader))
                shader.Init();
        }
    }

    public void RegisterSite(SiteRenderer site)
    {
        // foreach (var objectsManager in BaseShader.InstancesManagerByType)
        // {
        //     objectsManager.Value.RegisterBuffers(site.GetId());
        // }
        foreach (var shader in Shaders)
        {
            shader.AddSite(site);
        }
    }

    public void Dispose()
    {
        foreach (var shader in Shaders)
        {
            shader.Dispose();
        }

        foreach (var objectsManager in BaseShader.InstancesManagerByType)
        {
            objectsManager.Value.Dispose();
        }

        logicalDevice.Dispose();
        createVulkan.Dispose();
    }
    #endregion
}
