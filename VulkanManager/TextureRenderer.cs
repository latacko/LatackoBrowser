using System;
using Silk.NET.Vulkan;
using Vulkan;
using VulkanManager.BufferManager;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanManager;

public abstract class TextureRenderer
{
    public abstract void Init(uint width, uint height);
    public abstract VulkanEngine GetVulkanEngine();

    public abstract uint GetId();
    public abstract ulong GetCameraBufferDeviceAddress();

    public abstract void CreateVulkanEngine();
    public abstract void DestroyVulkanEngine();
    public abstract void Resize(uint width, uint height);
    public abstract void Update(double deltaTime);
    public abstract (Image image, Semaphore semaphore) GetImage();
}
