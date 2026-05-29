using System;
using System.Net.Http.Headers;
using Silk.NET.Vulkan;
using Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace ObjectCore.Textures;

public class TexturesManager : IDisposable
{
    static uint id = 0;
    static Semaphore timelineSemaphore;
    static readonly Dictionary<string, Texture> textures = new();
    public unsafe void Init()
    {
        SemaphoreTypeCreateInfo _typeInfo = new()
        {
            SType = StructureType.SemaphoreTypeCreateInfo,
            SemaphoreType = SemaphoreType.Timeline,
            InitialValue = 0,
        };

        SemaphoreCreateInfo _semCI = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
            PNext = &_typeInfo,
        };

        CreateVulkan.vk.CreateSemaphore(LogicalDevice.device, &_semCI, null, out timelineSemaphore);
    }

    public static Texture LoadTexture(string path)
    {
        if (textures.TryGetValue(path, out var texture))
            return texture;

        Texture _texture = new()
        {
            id = id
        };
        ImageHelper.CreateTextureImage(path, ref _texture.Image, ref _texture.Memory, id, timelineSemaphore);
        _texture.imageView = ImageHelper.CreateImageView(_texture.Image, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);
        ObjectManager.Instance.RegisterTexture(_texture.imageView, id);

        id++;
        textures.Add(path, _texture);
        return _texture;
    }

    public unsafe void Dispose()
    {
        CreateVulkan.vk.DestroySemaphore(LogicalDevice.device, timelineSemaphore, null);
        foreach (var texture in textures)
        {
            texture.Value.Dispose();
        }
    }
}
