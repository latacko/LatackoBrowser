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
        DescriptorSetAllocateInfo _allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            PNext = (void*)pNext,

            DescriptorPool = _poolToUse,
            DescriptorSetCount = 1,
            PSetLayouts = &layout,
        };

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
            _poolSizes[i] = new()
            {
                Type = ratios[i].Type,
                DescriptorCount = (uint)(ratios[i].Ratio * setCount),
            };
        }

        fixed (DescriptorPoolSize* _poolsPtr = _poolSizes)
        {
            DescriptorPoolCreateInfo _poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                Flags =  DescriptorPoolCreateFlags.UpdateAfterBindBit,
                MaxSets = setCount,
                PoolSizeCount = (uint)_poolSizes.Length,
                PPoolSizes = _poolsPtr,
            };

            CreateVulkan.vk.CreateDescriptorPool(LogicalDevice.device, &_poolInfo, null, out _pool);
        }
        return _pool;
    }


}
