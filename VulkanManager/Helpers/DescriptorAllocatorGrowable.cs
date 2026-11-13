using System;
using Silk.NET.Vulkan;
using Vulkan;

namespace VulkanManager.Helpers;

public unsafe struct DescriptorAllocatorGrowable
{
    public struct PoolSizeRatio
    {
        public DescriptorType Type;
        public float Ratio;
    }

    List<PoolSizeRatio> ratios = new();
    List<DescriptorPool> fullPools = new();
    List<DescriptorPool> readyPools = new();
    uint setsPerPool;

    public DescriptorAllocatorGrowable()
    {
    }

    public void Init(uint maxSets, PoolSizeRatio[] ratios)
    {
        this.ratios.Clear();

        foreach (var ratio in ratios)
        {
            this.ratios.Add(ratio);
        }

        DescriptorPool _newPool = CreatePool(maxSets, ratios);

        setsPerPool = (uint)(maxSets * 2);

        readyPools.Add(_newPool);
    }

    public void ClearPools()
    {
        foreach (var pool in readyPools)
        {
            CreateVulkan.vk.ResetDescriptorPool(LogicalDevice.device, pool, 0);
        }

        foreach (var pool in fullPools)
        {
            CreateVulkan.vk.ResetDescriptorPool(LogicalDevice.device, pool, 0);
            readyPools.Add(pool);
        }

        fullPools.Clear();
    }

    public void DestroyPools()
    {
        foreach (var pool in readyPools)
        {
            CreateVulkan.vk.DestroyDescriptorPool(LogicalDevice.device, pool, null);
        }
        readyPools.Clear();

        foreach (var pool in fullPools)
        {
            CreateVulkan.vk.DestroyDescriptorPool(LogicalDevice.device, pool, null);
        }
        fullPools.Clear();
    }

    public DescriptorSet Allocate(DescriptorSetLayout layout, nint pNext)
    {
        var _poolToUse = GetPool();
        DescriptorSetAllocateInfo _allocInfo = new(
            pNext: (void*)pNext,

            descriptorPool: _poolToUse,
            descriptorSetCount: 1,
            pSetLayouts: &layout
        );

        DescriptorSet _ds;
        var _result = CreateVulkan.vk.AllocateDescriptorSets(LogicalDevice.device, &_allocInfo, out _ds);

        if (_result == Result.ErrorOutOfPoolMemory || _result == Result.ErrorFragmentedPool)
        {
            fullPools.Add(_poolToUse);

            _poolToUse = GetPool();
            _allocInfo.DescriptorPool = _poolToUse;

            if (CreateVulkan.vk.AllocateDescriptorSets(LogicalDevice.device, &_allocInfo, out _ds) != Result.Success)
            {
                throw new Exception("Failed to allocate descriptor sets!");
            }
        }

        readyPools.Add(_poolToUse);
        return _ds;
    }

    DescriptorPool GetPool()
    {
        DescriptorPool _newPool;
        if (readyPools.Count != 0)
        {
            _newPool = readyPools[readyPools.Count - 1];
            readyPools.Remove(_newPool);
        }
        else
        {
            _newPool = CreatePool(setsPerPool, ratios.ToArray());

            setsPerPool = (uint)(setsPerPool * 1.5);
            if (setsPerPool > 4092)
            {
                setsPerPool = 4092;
            }
        }

        return _newPool;
    }

    DescriptorPool CreatePool(uint setCount, PoolSizeRatio[] ratios)
    {
        DescriptorPool _pool;
        DescriptorPoolSize[] _poolSizes = new DescriptorPoolSize[ratios.Length];
        for (int i = 0; i < _poolSizes.Length; i++)
        {
            _poolSizes[i] = new(
                type: ratios[i].Type,
                descriptorCount: (uint)(ratios[i].Ratio * setCount)
            );
        }

        fixed (DescriptorPoolSize* _poolsPtr = _poolSizes)
        {
            DescriptorPoolCreateInfo _poolInfo = new(
                flags: DescriptorPoolCreateFlags.UpdateAfterBindBit,
                maxSets: setCount,
                poolSizeCount: (uint)_poolSizes.Length,
                pPoolSizes: _poolsPtr
            );

            Result result = CreateVulkan.vk.CreateDescriptorPool(LogicalDevice.device, &_poolInfo, null, out _pool);

            if (result != Result.Success)
                throw new Exception($"CreateDescriptorPool failed: {result}");
        }
        return _pool;
    }


}
