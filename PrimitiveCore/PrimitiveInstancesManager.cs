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
        SamplerCreateInfo _samplerCI = new(
            magFilter: Filter.Linear,
            minFilter: Filter.Linear,
            mipmapMode: SamplerMipmapMode.Linear,
            anisotropyEnable: false,
            minLod: 0,
            maxLod: 0, // = 1000.0f, allows all mip levels
            addressModeU: SamplerAddressMode.ClampToEdge, // good for atlas
            addressModeV: SamplerAddressMode.ClampToEdge,
            addressModeW: SamplerAddressMode.ClampToEdge
        );
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
            DescriptorSetLayoutBindingFlagsCreateInfo _descBindingFlags = new(
                bindingCount: (uint)_descVariableFlag.Length,
                pBindingFlags: _descVariableFlagPtr
            );

            DescriptorLayoutBuilder builder = new();
            builder.AddBinding(new(
                binding: 0,
                descriptorType: DescriptorType.Sampler,
                descriptorCount: 1,
                stageFlags: ShaderStageFlags.FragmentBit
            ));
            builder.AddBinding(new(
                binding: 1,
                descriptorType: DescriptorType.SampledImage,
                descriptorCount: 256,
                stageFlags: ShaderStageFlags.FragmentBit
            ));

            objectDescriptorLayout = builder.Build((nint)(&_descBindingFlags), DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit);

        }

        uint _variableDescCount = 256;
        DescriptorSetVariableDescriptorCountAllocateInfo _variableDescCountAI = new(
            descriptorSetCount: 1,
            pDescriptorCounts: &_variableDescCount
        );
        objectDescriptorSet = ObjectDescriptorAllocatorGrowable.Allocate(objectDescriptorLayout, (nint)(&_variableDescCountAI));

        DescriptorImageInfo _samplerInfo = new(
            sampler: imageSampler
        );

        WriteDescriptorSet _descriptorWrites = new(
            dstSet: objectDescriptorSet,
            dstBinding: 0,
            dstArrayElement: 0,

            descriptorType: DescriptorType.Sampler,
            descriptorCount: 1,

            pImageInfo: &_samplerInfo
        );

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
        DescriptorImageInfo _imageInfo = new(
            imageView: imageView,
            imageLayout: ImageLayout.ShaderReadOnlyOptimal
        );

        WriteDescriptorSet _write = new(
            dstSet: objectDescriptorSet,
            dstBinding: 1,
            dstArrayElement: slot,
            descriptorType: DescriptorType.SampledImage,
            descriptorCount: 1,
            pImageInfo: &_imageInfo
        );

        // Console.WriteLine("Registering texture at slot: " + slot);
        CreateVulkan.vk.UpdateDescriptorSets(LogicalDevice.device, 1, &_write, 0, null);
    }

    public override unsafe void Dispose()
    {
        ObjectDescriptorAllocatorGrowable.DestroyPools();
        CreateVulkan.vk.DestroyDescriptorSetLayout(LogicalDevice.device, objectDescriptorLayout, null);
    }
}
