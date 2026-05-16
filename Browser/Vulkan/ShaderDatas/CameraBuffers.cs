using System.Runtime.CompilerServices;
using Browser;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
namespace Vulkan;

public unsafe class CameraBuffers
{
    internal ShaderDataBuffer[] shaderDataBuffersForCamera = new ShaderDataBuffer[VulkanManager.MAX_FRAMES_IN_FLIGHT];


    internal void CreateBuffers()
    {
        for (int i = 0; i < VulkanManager.MAX_FRAMES_IN_FLIGHT; i++)
        {
            BufferHelper.CreateBuffer((ulong)Unsafe.SizeOf<UICameraUBO>(), BufferUsageFlags.ShaderDeviceAddressBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref shaderDataBuffersForCamera[i].Buffer, ref shaderDataBuffersForCamera[i].Memory);
            void* data;
            CreateVulkan.vk.MapMemory(LogicalDevice.device, shaderDataBuffersForCamera[i].Memory, 0, (ulong)Unsafe.SizeOf<UICameraUBO>(), 0, &data);
            shaderDataBuffersForCamera[i].Mapped = data;

            BufferDeviceAddressInfo addrInfo = new()
            {
                SType = StructureType.BufferDeviceAddressInfo,
                Buffer = shaderDataBuffersForCamera[i].Buffer
            };

            shaderDataBuffersForCamera[i].DeviceAddress = CreateVulkan.vk.GetBufferDeviceAddress(LogicalDevice.device, ref addrInfo);
        }
    }

    public void Update(uint currentFrame, Matrix4X4<float> proj)
    {
        var ubo = new UICameraUBO { Proj = proj };

        new Span<UICameraUBO>(shaderDataBuffersForCamera[currentFrame].Mapped, 1)[0] = ubo;
    }

    public void Dispose()
    {
        for (int i = 0; i < VulkanManager.MAX_FRAMES_IN_FLIGHT; i++)
        {
            CreateVulkan.vk.UnmapMemory(LogicalDevice.device, shaderDataBuffersForCamera[i].Memory);
            BufferHelper.DestroyBuffer(shaderDataBuffersForCamera[i].Buffer, shaderDataBuffersForCamera[i].Memory);
        }
    }
}