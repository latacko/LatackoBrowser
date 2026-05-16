using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Vulkan;

public unsafe class VulkanManager : IDisposable
{
    public const int MAX_FRAMES_IN_FLIGHT = 2;

    ShaderDataBuffer[] shaderDataBuffers = new ShaderDataBuffer[MAX_FRAMES_IN_FLIGHT];
    public CommandBuffer[] commandBuffers = new CommandBuffer[MAX_FRAMES_IN_FLIGHT];
    CameraBuffers cameraBuffers = new();

    internal static DescriptorPool descriptorPool;
    internal static DescriptorSetLayout descriptorSetLayoutForTextures;
    internal static DescriptorSet descriptorSetForTextures;
    internal static DescriptorSetLayout descriptorSetLayoutForObjects;
    internal static DescriptorSet descriptorSetForObjects;

    Sampler sampler;

    #region Syncing
    public Semaphore[] renderCompleteSemaphores;
    public Semaphore[] imageAcquiredSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
    public Fence[] fences = new Fence[MAX_FRAMES_IN_FLIGHT];
    #endregion

    static CommandPool commandPool;

    internal void Init()
    {
        CreateShaderDataBuffers();
        CreateSynchronizationObjects();

        CreateCommandPool();
        CreateCommandBuffers();

        CreateDescriptorSetLayoutForTextures();

        CreateDescriptorPool(0);
    }

    void CreateShaderDataBuffers()
    {
        cameraBuffers.CreateBuffers();
    }

    void CreateSynchronizationObjects()
    {
        renderCompleteSemaphores = new Semaphore[Swapchain.Instance.swapChainImages.Length];
        var _semaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        var _fences = new Fence[MAX_FRAMES_IN_FLIGHT];

        SemaphoreCreateInfo semaphoreCI = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        FenceCreateInfo fenceCI = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };

        fixed (Fence* fencesPtr = fences)
        fixed (Semaphore* semaphoresPtr = imageAcquiredSemaphores)
        fixed (Semaphore* renderCompleteSemaphoresPtr = renderCompleteSemaphores)
        {
            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                CreateVulkan.vk.CreateFence(LogicalDevice.device, &fenceCI, null, &fencesPtr[i]);
                CreateVulkan.vk.CreateSemaphore(LogicalDevice.device, &semaphoreCI, null, &semaphoresPtr[i]);
            }

            for (int i = 0; i < renderCompleteSemaphores.Length; i++)
            {
                CreateVulkan.vk.CreateSemaphore(LogicalDevice.device, &semaphoreCI, null, &renderCompleteSemaphoresPtr[i]);
            }
        }
    }

    void CreateCommandPool()
    {
        var queueFamilyIndices = PhysicalDevice.Instance.FindQueueFamilies(PhysicalDevice.physicalDevice);
        CommandPoolCreateInfo commandPoolCI = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
        };

        CreateVulkan.vk.CreateCommandPool(LogicalDevice.device, ref commandPoolCI, null, out commandPool);
    }

    void CreateCommandBuffers()
    {
        CommandBufferAllocateInfo commandPoolCI = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            CommandBufferCount = MAX_FRAMES_IN_FLIGHT
        };

        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
            CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, null, commandBuffersPtr);
    }

    void CreateImageSampler()
    {
        SamplerCreateInfo _samplerCI = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AnisotropyEnable = Vk.True,
            MaxLod = 0,
        };

        CreateVulkan.vk.GetPhysicalDeviceProperties(PhysicalDevice.physicalDevice, out var properties);
        _samplerCI.MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy;

        fixed (Sampler* samplerPtr = &sampler)
            CreateVulkan.vk.CreateSampler(LogicalDevice.device, &_samplerCI, null, samplerPtr);
    }

    void CreateDescriptorSetLayoutForTextures()
    {
        DescriptorBindingFlags[] _descVariableFlag = [0, DescriptorBindingFlags.VariableDescriptorCountBit];
        fixed (DescriptorBindingFlags* _descVariableFlagPtr = _descVariableFlag)
        {
            DescriptorSetLayoutBindingFlagsCreateInfo _descBindingFlags = new()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)_descVariableFlag.Length,
                PBindingFlags = _descVariableFlagPtr
            };

            DescriptorSetLayoutBinding _samplerLayoutBinding = new()
            {
                Binding = 0,
                DescriptorType = DescriptorType.Sampler,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.FragmentBit,
            };

            DescriptorSetLayoutBinding _sampledImageLayoutBinding = new()
            {
                Binding = 1,
                DescriptorType = DescriptorType.SampledImage,
                DescriptorCount = 1024,
                StageFlags = ShaderStageFlags.FragmentBit,
            };

            DescriptorSetLayoutBinding[] _binding = [_samplerLayoutBinding, _sampledImageLayoutBinding];
            fixed (DescriptorSetLayoutBinding* _bindingPtr = _binding)
            {
                DescriptorSetLayoutCreateInfo _layoutInfo = new()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,

                    BindingCount = (uint)_binding.Length,
                    PBindings = _bindingPtr,

                    PNext = &_descBindingFlags,
                };

                if (CreateVulkan.vk.CreateDescriptorSetLayout(LogicalDevice.device, &_layoutInfo, null, out descriptorSetLayoutForTextures) != Result.Success)
                {
                    throw new Exception("Failed to create descriptor set layout!");
                }
            }
        }
    }
    void CreateDescriptorSetLayoutForObject()
    {
        DescriptorBindingFlags[] _descVariableFlag = [DescriptorBindingFlags.VariableDescriptorCountBit];
        fixed (DescriptorBindingFlags* _descVariableFlagPtr = _descVariableFlag)
        {
            DescriptorSetLayoutBindingFlagsCreateInfo _descBindingFlags = new()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)_descVariableFlag.Length,
                PBindingFlags = _descVariableFlagPtr
            };

            DescriptorSetLayoutBinding _samplerLayoutBinding = new()
            {
                Binding = 0,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,

                StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            };

            DescriptorSetLayoutCreateInfo _layoutInfo = new()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,

                BindingCount = 1,
                PBindings = &_samplerLayoutBinding,

                PNext = &_descBindingFlags,
            };

            if (CreateVulkan.vk.CreateDescriptorSetLayout(LogicalDevice.device, &_layoutInfo, null, out descriptorSetLayoutForObjects) != Result.Success)
            {
                throw new Exception("Failed to create descriptor set layout!");
            }
        }
    }

    void CreateDescriptorPool(int shaderCount)
    {
        DescriptorPoolSize _poolSamplerSize = new()
        {
            Type = DescriptorType.Sampler,
            DescriptorCount = 1,
        };

        DescriptorPoolSize _poolImageSize = new()
        {
            Type = DescriptorType.SampledImage,
            DescriptorCount = 1024,
        };

        DescriptorPoolSize[] _pools = [_poolSamplerSize, _poolImageSize];

        fixed (DescriptorPoolSize* _poolsPtr = _pools)
        {
            DescriptorPoolCreateInfo _descPoolCI = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                MaxSets = MAX_FRAMES_IN_FLIGHT,
                PoolSizeCount = (uint)_pools.Length,
                PPoolSizes = _poolsPtr,
            };

            CreateVulkan.vk.CreateDescriptorPool(LogicalDevice.device, &_descPoolCI, null, out descriptorPool);
        }
    }

    private protected void CreateDescriptorSetsForTextures()
    {
        uint _variableDescCount = 1024;
        fixed (DescriptorSetLayout* layoutPtr = &descriptorSetLayoutForTextures)
        {
            DescriptorSetVariableDescriptorCountAllocateInfo _variableDescCountAI = new()
            {
                SType = StructureType.DescriptorSetVariableDescriptorCountAllocateInfoExt,
                DescriptorSetCount = 1,
                PDescriptorCounts = &_variableDescCount
            };

            DescriptorSetAllocateInfo _allocInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                PNext = &_variableDescCountAI,

                DescriptorPool = descriptorPool,
                DescriptorSetCount = 1,
                PSetLayouts = layoutPtr,
            };

            if (CreateVulkan.vk.AllocateDescriptorSets(LogicalDevice.device, &_allocInfo, out descriptorSetForTextures) != Result.Success)
            {
                throw new Exception("Failed to allocate descriptor sets!");
            }

            DescriptorImageInfo _samplerInfo = new()
            {
                Sampler = sampler,
            };

            DescriptorImageInfo _texInfo = new()
            {
                ImageView = default,
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            };

            WriteDescriptorSet[] _descriptorWrites = [
                new (){
                    SType = StructureType.WriteDescriptorSet,

                    DstSet = descriptorSetForTextures,
                    DstBinding = 0,
                    DstArrayElement = 0,

                    DescriptorType = DescriptorType.Sampler,
                    DescriptorCount = 1,

                    PImageInfo = &_samplerInfo,
                },
                new (){
                    SType = StructureType.WriteDescriptorSet,

                    DstSet = descriptorSetForTextures,
                    DstBinding = 1,
                    DstArrayElement = 0,

                    DescriptorType = DescriptorType.SampledImage,
                    DescriptorCount = 1,

                    PImageInfo = &_texInfo,
                },
            ];

            fixed (WriteDescriptorSet* descriptorWritesPtr = _descriptorWrites)
                CreateVulkan.vk.UpdateDescriptorSets(LogicalDevice.device, (uint)_descriptorWrites.Length, descriptorWritesPtr, 0, null);
        }
    }

    public static CommandBuffer BeginSingleTimeCommands()
    {
        CommandBufferAllocateInfo _allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = 1,
        };

        CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, &_allocInfo, out var _commandBuffer);

        CommandBufferBeginInfo _beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        CreateVulkan.vk.BeginCommandBuffer(_commandBuffer, &_beginInfo);

        return _commandBuffer;
    }

    public static void EndSingleTimeCommandsIdle(CommandBuffer commandBuffer)
    {
        CreateVulkan.vk.EndCommandBuffer(commandBuffer);

        SubmitInfo _submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        CreateVulkan.vk.QueueSubmit(LogicalDevice.graphicsQueue, 1, &_submitInfo, default);
        CreateVulkan.vk.QueueWaitIdle(LogicalDevice.graphicsQueue);

        CreateVulkan.vk.FreeCommandBuffers(LogicalDevice.device, commandPool, 1, &commandBuffer);
    }

    public static void EndSingleTimeCommands(CommandBuffer commandBuffer, Fence fence)
    {
        CreateVulkan.vk.EndCommandBuffer(commandBuffer);

        SubmitInfo _submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        CreateVulkan.vk.QueueSubmit(LogicalDevice.graphicsQueue, 1, &_submitInfo, fence);
    }

    public void UpdateCameraBuffer(uint currentFrame, Matrix4X4<float> proj)
    {
        cameraBuffers.Update(currentFrame, proj);
    }

    public void Dispose()
    {
        CreateVulkan.vk.DestroyDescriptorSetLayout(LogicalDevice.device, descriptorSetLayoutForTextures, null);
    }
}