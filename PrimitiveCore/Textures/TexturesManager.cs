using System;
using System.Net.Http.Headers;
using Silk.NET.Vulkan;
using Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace PrimitiveCore.Textures;

public class TexturesManager : IDisposable
{
    static uint id = 0;
    static Semaphore timelineSemaphore;
    static readonly Dictionary<string, Texture> textures = new();

    struct TextureStagingBuffer
    {
        public Silk.NET.Vulkan.Buffer buffer;
        public DeviceMemory deviceMemory;
        public uint id;
    }
    static List<TextureStagingBuffer> texturesStagingBuffer = new();
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
        TextureStagingBuffer _textureStagingBuffer = new()
        {
            id = id,
        };
        ImageHelper.CreateTextureImage(path, ref _texture.Image, ref _texture.Memory, id, timelineSemaphore, out _textureStagingBuffer.buffer, out _textureStagingBuffer.deviceMemory);
        texturesStagingBuffer.Add(_textureStagingBuffer);

        _texture.imageView = ImageHelper.CreateImageView(_texture.Image, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);
        PrimitiveInstancesManager.Instance.RegisterTexture(_texture.imageView, id);

        id++;
        textures.Add(path, _texture);
        return _texture;
    }

    public static void Tick()
    {
        CreateVulkan.vk.GetSemaphoreCounterValue(LogicalDevice.device, timelineSemaphore, out var _currentValue);
        for (int i = texturesStagingBuffer.Count-1; i >= 0; i--)
        {
            if (texturesStagingBuffer[i].id < _currentValue)
            {
                BufferHelper.DestroyBuffer(texturesStagingBuffer[i].buffer, texturesStagingBuffer[i].deviceMemory);
                texturesStagingBuffer.RemoveAt(i);
            }
        }
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
