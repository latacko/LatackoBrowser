using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
namespace ObjectCore;

public unsafe class ObjectsManager
{
    public static ObjectsManager Instance;
    public const int MAX_OBJECTS = 1000;
    public ShaderDataBuffer[] shaderDataBuffersForObjects = new ShaderDataBuffer[VulkanManager.MAX_FRAMES_IN_FLIGHT];

    public ObjectsManager()
    {
        Instance = this;
    }

    public void RegisterBuffers()
    {
        ulong _bufferSize = (ulong)Unsafe.SizeOf<ObjectData>()*MAX_OBJECTS;
        for (int i = 0; i < VulkanManager.MAX_FRAMES_IN_FLIGHT; i++)
        {
            BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.ShaderDeviceAddressBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref shaderDataBuffersForObjects[i].Buffer, ref shaderDataBuffersForObjects[i].Memory);
            void* data;
            CreateVulkan.vk.MapMemory(LogicalDevice.device, shaderDataBuffersForObjects[i].Memory, 0, _bufferSize, 0, &data);
            shaderDataBuffersForObjects[i].Mapped = data;

            BufferDeviceAddressInfo addrInfo = new()
            {
                SType = StructureType.BufferDeviceAddressInfo,
                Buffer = shaderDataBuffersForObjects[i].Buffer
            };

            shaderDataBuffersForObjects[i].DeviceAddress = CreateVulkan.vk.GetBufferDeviceAddress(LogicalDevice.device, ref addrInfo);
        }
    }

    public void Update(uint currentFrame, uint objectIndex, ObjectData objectData)
    {
        new Span<ObjectData>(shaderDataBuffersForObjects[currentFrame].Mapped, MAX_OBJECTS)[(int)objectIndex] = objectData;
    }

    public void Dispose()
    {
        for (int i = 0; i < VulkanManager.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.UnmapMemory(LogicalDevice.device, shaderDataBuffersForObjects[i].Memory);
            BufferHelper.DestroyBuffer(shaderDataBuffersForObjects[i].Buffer, shaderDataBuffersForObjects[i].Memory);
        }
    }
}