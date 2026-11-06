using System;
using GraphicsCore;
using InstanceFinderCore;
using Silk.NET.Vulkan;
using Vulkan;
using VulkanManager.Helpers;

namespace PrimitiveCore;

[InstanceFinderCore.Instance]
public class PrimitiveInstancesManager : BufferManager, IInstanceInit
{
    public static PrimitiveInstancesManager Instance;

    public DescriptorAllocatorGrowable ObjectDescriptorAllocatorGrowable = new();
    internal DescriptorSetLayout objectDescriptorLayout;
    internal DescriptorSet objectDescriptorSet = new();

    Sampler imageSampler;

    public PrimitiveInstancesManager()
    {
        Instance = this;
    }

    public void Init()
    {
        RegisterBuffer();
    }

    public override void RegisterBuffer()
    {
        CreateImageSampler();
        CreateDescriptorsPool();
        RegisterDescriptor();
    }

    unsafe void CreateImageSampler()
    {
        SamplerCreateInfo _samplerCI = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AnisotropyEnable = false,
            MinLod = 0,
            MaxLod = 0, // = 1000.0f, allows all mip levels
            AddressModeU = SamplerAddressMode.ClampToEdge, // good for atlas
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
        };
        CreateVulkan.vk.GetPhysicalDeviceProperties(Vulkan.PhysicalDevice.physicalDevice, out var properties);
        _samplerCI.MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy;
        fixed (Sampler* samplerPtr = &imageSampler)
            CreateVulkan.vk.CreateSampler(LogicalDevice.device, &_samplerCI, null, samplerPtr);
    }

    public unsafe void RegisterDescriptor()
    {
        DescriptorBindingFlags[] _descVariableFlag = [0, DescriptorBindingFlags.VariableDescriptorCountBit | DescriptorBindingFlags.PartiallyBoundBit];
        fixed (DescriptorBindingFlags* _descVariableFlagPtr = _descVariableFlag)
        {
            DescriptorSetLayoutBindingFlagsCreateInfo _descBindingFlags = new()
            {
                SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
                BindingCount = (uint)_descVariableFlag.Length,
                PBindingFlags = _descVariableFlagPtr
            };

            DescriptorLayoutBuilder builder = new();
            builder.AddBinding(new()
            {
                Binding = 0,
                DescriptorType = DescriptorType.Sampler,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.FragmentBit,
            });
            builder.AddBinding(new()
            {
                Binding = 1,
                DescriptorType = DescriptorType.SampledImage,
                DescriptorCount = 256,
                StageFlags = ShaderStageFlags.FragmentBit,
            });

            objectDescriptorLayout = builder.Build((nint)(&_descBindingFlags), DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit);

        }

        uint _variableDescCount = 256;
        DescriptorSetVariableDescriptorCountAllocateInfo _variableDescCountAI = new()
        {
            SType = StructureType.DescriptorSetVariableDescriptorCountAllocateInfoExt,
            DescriptorSetCount = 1,
            PDescriptorCounts = &_variableDescCount
        };
        objectDescriptorSet = ObjectDescriptorAllocatorGrowable.Allocate(objectDescriptorLayout, (nint)(&_variableDescCountAI));

        DescriptorImageInfo _samplerInfo = new()
        {
            Sampler = imageSampler,
        };

        WriteDescriptorSet _descriptorWrites = new()
        {
            SType = StructureType.WriteDescriptorSet,

            DstSet = objectDescriptorSet,
            DstBinding = 0,
            DstArrayElement = 0,

            DescriptorType = DescriptorType.Sampler,
            DescriptorCount = 1,

            PImageInfo = &_samplerInfo,
        };

        CreateVulkan.vk.UpdateDescriptorSets(LogicalDevice.device, 1, &_descriptorWrites, 0, null);
    }

    void CreateDescriptorsPool()
    {
        DescriptorAllocatorGrowable.PoolSizeRatio[] _sizes = [
            new(){
                Type = DescriptorType.Sampler,
                Ratio = 1,
            },
            new(){
                Type = DescriptorType.SampledImage,
                Ratio = 256,
            }
        ];

        ObjectDescriptorAllocatorGrowable.Init(1, _sizes);
    }

    public unsafe void RegisterTexture(ImageView imageView, uint slot)
    {
        DescriptorImageInfo _imageInfo = new()
        {
            ImageView = imageView,
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
        };

        WriteDescriptorSet _write = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = objectDescriptorSet,
            DstBinding = 1,
            DstArrayElement = slot,
            DescriptorType = DescriptorType.SampledImage,
            DescriptorCount = 1,
            PImageInfo = &_imageInfo,
        };

        // Console.WriteLine("Registering texture at slot: " + slot);
        CreateVulkan.vk.UpdateDescriptorSets(LogicalDevice.device, 1, &_write, 0, null);
    }

    public override unsafe void Dispose()
    {
        ObjectDescriptorAllocatorGrowable.DestroyPools();
        CreateVulkan.vk.DestroyDescriptorSetLayout(LogicalDevice.device, objectDescriptorLayout, null);
    }
}
