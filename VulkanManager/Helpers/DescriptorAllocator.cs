using System;
using Silk.NET.Vulkan;
using Vulkan;

namespace VulkanManager.Helpers;

public unsafe struct DescriptorAllocator
{
    public struct PoolSizeRatio
    {
        public DescriptorType Type;
        public float Ratio;
    }

    DescriptorPool pool;

    public void InitPool(uint maxSets, PoolSizeRatio[] ratios)
    {
        DescriptorPoolSize[] _poolSizes = new DescriptorPoolSize[ratios.Length];
        for (int i = 0; i < _poolSizes.Length; i++)
        {
            _poolSizes[i] = new()
            {
                Type = ratios[i].Type,
                DescriptorCount = (uint)(ratios[i].Ratio * maxSets),
            };
        }

        fixed (DescriptorPoolSize* _poolsPtr = _poolSizes)
        {
            DescriptorPoolCreateInfo _poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                Flags = 0,
                MaxSets = maxSets,
                PoolSizeCount = (uint)_poolSizes.Length,
                PPoolSizes = _poolsPtr,
            };

            CreateVulkan.vk.CreateDescriptorPool(LogicalDevice.device, &_poolInfo, null, out pool);
        }
    }

    public void ClearDescriptors()
    {
        CreateVulkan.vk.ResetDescriptorPool(LogicalDevice.device, pool, 0);
    }

    public void DestroyPool()
    {
        CreateVulkan.vk.DestroyDescriptorPool(LogicalDevice.device, pool, null);
    }

    public DescriptorSet Allocate(DescriptorSetLayout layout, nint pNext)
    {
        DescriptorSetAllocateInfo _allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            PNext = (void*)pNext,

            DescriptorPool = pool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout,
        };


        if (CreateVulkan.vk.AllocateDescriptorSets(LogicalDevice.device, &_allocInfo, out DescriptorSet _ds) != Result.Success)
        {
            throw new Exception("Failed to allocate descriptor sets!");
        }

        return _ds;
    }
}
