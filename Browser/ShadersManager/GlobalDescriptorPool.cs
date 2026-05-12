using Browser;
using Silk.NET.Vulkan;

public static class GlobalDescriptorPool
{
    public static DescriptorPool Pool;

    public static unsafe void Init(int shaderCount)
    {
        DescriptorPoolSize[] sizes = [
            new() { Type = DescriptorType.UniformBuffer,        DescriptorCount = BrowserWindow.MAX_FRAMES_IN_FLIGHT },
            new() { Type = DescriptorType.CombinedImageSampler, DescriptorCount = (uint)shaderCount },
            new() { Type = DescriptorType.StorageBuffer,        DescriptorCount = (uint)shaderCount },
        ];

        fixed (DescriptorPoolSize* ptr = sizes)
        {
            DescriptorPoolCreateInfo info = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)sizes.Length,
                PPoolSizes = ptr,
                MaxSets = (uint)(BrowserWindow.MAX_FRAMES_IN_FLIGHT + // Camera
                (1 + // Material
                 1  // Object,
                )*shaderCount) // total sets across everything
            };

            BrowserWindow.vk.CreateDescriptorPool(BrowserWindow.device, &info, null, out Pool);
        }
    }

    public static unsafe void Dispose()
    {
        BrowserWindow.vk.DestroyDescriptorPool(BrowserWindow.device, Pool, null);
        // destroying pool frees all sets allocated from it automatically
    }
}