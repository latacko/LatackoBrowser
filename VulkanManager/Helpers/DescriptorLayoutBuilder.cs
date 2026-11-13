using System;
using Silk.NET.Vulkan;
using Vulkan;

namespace VulkanManager.Helpers;

public struct DescriptorLayoutBuilder
{
    List<DescriptorSetLayoutBinding> bindings = new();

    public DescriptorLayoutBuilder()
    {
    }

    public void AddBinding(DescriptorSetLayoutBinding binding)
    {
        bindings.Add(binding);
    }
    public void Clear()
    {
        bindings.Clear();
    }

    public unsafe DescriptorSetLayout Build(nint pNext, DescriptorSetLayoutCreateFlags flags)
    {
        var _bindingsArr = bindings.ToArray();
        DescriptorSetLayout _layout;
        fixed (DescriptorSetLayoutBinding* _bindingPtr = _bindingsArr)
        {
            DescriptorSetLayoutCreateInfo _layoutInfo = new(
                bindingCount: (uint)_bindingsArr.Length,
                pBindings: _bindingPtr,

                pNext: (void*)pNext,
                flags: flags
            );
            if (CreateVulkan.vk.CreateDescriptorSetLayout(LogicalDevice.device, &_layoutInfo, null, out _layout) != Result.Success)
            {
                throw new Exception("Failed to create descriptor set layout!");
            }
        }

        return _layout;
    }
}
