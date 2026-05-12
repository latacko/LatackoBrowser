using Browser;
using Browser.DataTypes;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe abstract class BaseShader : IDisposable
{
    public Pipeline Pipeline;
    public PipelineLayout PipelineLayout;
    public List<RuntimeModelData> elements = new();
    public int LastCreatedIndex = 0;

    private protected DescriptorSetLayout descriptorSetLayoutForMaterial;
    private protected DescriptorSetLayout descriptorSetLayoutForObject;

    private protected DescriptorSet materialDescriptorSet;
    private protected DescriptorSet objectDescriptorSet;

    private protected Buffer objectBuffer;
    private protected DeviceMemory objectBufferMemory;
    public void* persistentMappedObjectBuffer;

    private protected virtual int GetMaxObjectForShader() => 10000;
    private protected virtual ulong GetSizeOfObjectDatas() => (ulong)(sizeof(ObjectData) * GetMaxObjectForShader());

    protected abstract string VertexShaderPath { get; }
    protected abstract string FragmentShaderPath { get; }


    #region Init
    public virtual void Init()
    {
        CreateDescriptorSetLayoutForMaterial();
        CreateDescriptorSetLayoutForObject();

        BufferHelper.CreateBuffer(GetSizeOfObjectDatas(), BufferUsageFlags.StorageBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref objectBuffer, ref objectBufferMemory);
        fixed (void** ptr = &persistentMappedObjectBuffer)
            BrowserWindow.vk.MapMemory(BrowserWindow.device, objectBufferMemory, 0, GetSizeOfObjectDatas(), 0, ptr);


        CreateDescriptorSetsForMaterial();
        CreateDescriptorSetForObject();
    }

    void CreateDescriptorSetLayoutForMaterial()
    {
        DescriptorSetLayoutBinding _samplerLayoutBinding = new()
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            PImmutableSamplers = null,
            StageFlags = ShaderStageFlags.FragmentBit,
        };

        DescriptorSetLayoutCreateInfo _layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,

            BindingCount = 1,
            PBindings = &_samplerLayoutBinding,
        };

        if (BrowserWindow.vk.CreateDescriptorSetLayout(BrowserWindow.device, &_layoutInfo, null, out descriptorSetLayoutForMaterial) != Result.Success)
        {
            throw new Exception("Failed to create descriptor set layout!");
        }
    }

    void CreateDescriptorSetLayoutForObject()
    {
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
        };

        if (BrowserWindow.vk.CreateDescriptorSetLayout(BrowserWindow.device, &_layoutInfo, null, out descriptorSetLayoutForObject) != Result.Success)
        {
            throw new Exception("Failed to create descriptor set layout!");
        }
    }

    #endregion

    public virtual DescriptorSetLayout[] GetLayouts()
    {
        return [CameraBuffer.Instance.Layout, descriptorSetLayoutForMaterial, descriptorSetLayoutForObject];
    }

    public virtual unsafe void Render(CommandBuffer commandBuffer, uint currentFrame, KhrPushDescriptor khrPushDescriptor)
    {
        BrowserWindow.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, Pipeline);

        // BrowserWindow.vk.CmdDraw(commandBuffer, 3, 1, 0, 0);

        // set 0 — camera, same for all shaders
        fixed (DescriptorSet* ptr = CameraBuffer.Instance.DescriptorSets)
            BrowserWindow.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, 0, 1, ptr + currentFrame, 0, null);

        // set 2 — object buffer, once per shader
        fixed (DescriptorSet* ptr = &objectDescriptorSet)
            BrowserWindow.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, 2, 1, ptr, 0, null);

        // subclass renders its elements
        RenderElements(commandBuffer, currentFrame, khrPushDescriptor);
    }

    // subclass overrides this to render its own elements
    protected abstract void RenderElements(CommandBuffer commandBuffer, uint currentFrame, KhrPushDescriptor khrPushDescriptor);

    #region Graphic pipeline
    protected virtual PipelineDepthStencilStateCreateInfo GetDepthStencil() => new()
    {
        SType = StructureType.PipelineDepthStencilStateCreateInfo,
        DepthTestEnable = Vk.True,
        DepthWriteEnable = Vk.True,
        DepthCompareOp = CompareOp.Less,
        DepthBoundsTestEnable = Vk.False,
        StencilTestEnable = Vk.False,
    };

    protected virtual PipelineColorBlendAttachmentState GetColorBlend() => new()
    {
        ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit |
                         ColorComponentFlags.BBit | ColorComponentFlags.ABit,
        BlendEnable = Vk.False,
    };

    protected virtual PipelineRasterizationStateCreateInfo GetRasterizer() => new()
    {
        SType = StructureType.PipelineRasterizationStateCreateInfo,
        DepthClampEnable = Vk.False,
        RasterizerDiscardEnable = Vk.False,
        PolygonMode = PolygonMode.Fill,
        LineWidth = 1f,
        CullMode = CullModeFlags.BackBit,
        // CullMode = CullModeFlags.None,
        FrontFace = FrontFace.CounterClockwise,
        DepthBiasEnable = Vk.False,
    };

    // push constant ranges — override if needed
    protected virtual PushConstantRange[] GetPushConstantRanges() => [
        new()
        {
            StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            Offset     = 0,
            Size       = sizeof(int) // object index
        }
    ];

    public virtual void CreatePipeline(RenderPass renderPass)
    {
        var vertCode = File.ReadAllBytes(VertexShaderPath);
        var fragCode = File.ReadAllBytes(FragmentShaderPath);

        var vertModule = CreateShaderModule(vertCode);
        var fragModule = CreateShaderModule(fragCode);

        PipelineShaderStageCreateInfo vertStage = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        PipelineShaderStageCreateInfo fragStage = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        PipelineShaderStageCreateInfo[] stages = [vertStage, fragStage];

        var bindingDesc = Vertex.GetBindingDescription();
        var attributeDescs = Vertex.GetAttributeDescriptions();

        var depthStencil = GetDepthStencil();
        var colorBlend = GetColorBlend();
        var rasterizer = GetRasterizer();
        var pushRanges = GetPushConstantRanges();
        var layouts = GetLayouts();

        DynamicState[] dynamicStates = [DynamicState.Viewport, DynamicState.Scissor];

        fixed (VertexInputAttributeDescription* attrPtr = attributeDescs)
        fixed (DynamicState* dynPtr = dynamicStates)
        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        fixed (PushConstantRange* pushRangesPtr = pushRanges)
        fixed (PipelineShaderStageCreateInfo* stagesPtr = stages)
        {
            PipelineVertexInputStateCreateInfo vertexInput = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,

                VertexBindingDescriptionCount = 1,
                PVertexBindingDescriptions = &bindingDesc,

                VertexAttributeDescriptionCount = (uint)attributeDescs.Length,
                PVertexAttributeDescriptions = attrPtr,
            };

            PipelineInputAssemblyStateCreateInfo inputAssembly = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = Vk.False,
            };

            PipelineViewportStateCreateInfo viewportState = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                ScissorCount = 1,
            };

            PipelineMultisampleStateCreateInfo multisampling = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = Vk.False,
                RasterizationSamples = SampleCountFlags.Count1Bit,
            };

            PipelineColorBlendStateCreateInfo colorBlending = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,

                LogicOpEnable = Vk.False,
                LogicOp = LogicOp.Copy,
                AttachmentCount = 1,
                PAttachments = &colorBlend,
            };

            PipelineDynamicStateCreateInfo dynamicState = new()
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = (uint)dynamicStates.Length,
                PDynamicStates = dynPtr,
            };

            PipelineLayoutCreateInfo layoutInfo = new()
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint)layouts.Length,
                PSetLayouts = layoutsPtr,
                PushConstantRangeCount = (uint)pushRanges.Length,
                PPushConstantRanges = pushRangesPtr,
            };


            if (BrowserWindow.vk.CreatePipelineLayout(BrowserWindow.device, &layoutInfo, null, out PipelineLayout) != Result.Success)
                throw new Exception("Failed to create pipeline layout!");

            GraphicsPipelineCreateInfo pipelineInfo = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = (uint)stages.Length,
                PStages = stagesPtr,
                PVertexInputState = &vertexInput,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlending,
                PDynamicState = &dynamicState,
                Layout = PipelineLayout,
                RenderPass = renderPass,
                Subpass = 0,
                BasePipelineHandle = default,
                BasePipelineIndex = -1,
            };

            if (BrowserWindow.vk.CreateGraphicsPipelines(BrowserWindow.device, default, 1, &pipelineInfo, null, out Pipeline) != Result.Success)
                throw new Exception("Failed to create graphics pipeline!");
        }

        SilkMarshal.FreeString((nint)vertStage.PName);
        SilkMarshal.FreeString((nint)fragStage.PName);
        BrowserWindow.vk.DestroyShaderModule(BrowserWindow.device, vertModule, null);
        BrowserWindow.vk.DestroyShaderModule(BrowserWindow.device, fragModule, null);
    }

    ShaderModule CreateShaderModule(byte[] code)
    {
        fixed (byte* codePtr = code)
        {
            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)code.Length,
                PCode = (uint*)codePtr,
            };

            BrowserWindow.vk.CreateShaderModule(BrowserWindow.device, &createInfo, null, out ShaderModule module);
            return module;
        }
    }
    #endregion

    #region Create descriptor sets
    private protected abstract void CreateDescriptorSetsForMaterial();
    private protected abstract void CreateDescriptorSetForObject();
    #endregion
    public virtual void Dispose()
    {
        foreach (var element in elements)
        {
            element.Dispose();
        }

        BrowserWindow.vk.UnmapMemory(BrowserWindow.device, objectBufferMemory);
        BufferHelper.DestroyBuffer(objectBuffer, objectBufferMemory);

        BrowserWindow.vk.DestroyPipeline(BrowserWindow.device, Pipeline, null);
        BrowserWindow.vk.DestroyPipelineLayout(BrowserWindow.device, PipelineLayout, null);

        BrowserWindow.vk.DestroyDescriptorSetLayout(BrowserWindow.device, descriptorSetLayoutForMaterial, null);
        BrowserWindow.vk.DestroyDescriptorSetLayout(BrowserWindow.device, descriptorSetLayoutForObject, null);
    }
}