using Browser;
using Browser.DataTypes;
using Silk.NET.Assimp;
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

    private protected DescriptorSet materialDescriptorSet;
    private protected DescriptorSet objectDescriptorSet;

    private protected Buffer objectBuffer;
    private protected DeviceMemory objectBufferMemory;
    public void* persistentMappedObjectBuffer;

    private protected virtual int GetMaxObjectForShader() => 10000;
    private protected virtual ulong GetSizeOfObjectDatas() => (ulong)(sizeof(ObjectData) * GetMaxObjectForShader());

    protected abstract string moduleShaderPath { get; }


    #region Init
    public virtual void Init()
    {
        CreateDescriptorSetLayoutForObject();

        BufferHelper.CreateBuffer(GetSizeOfObjectDatas(), BufferUsageFlags.StorageBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref objectBuffer, ref objectBufferMemory);
        fixed (void** ptr = &persistentMappedObjectBuffer)
            BrowserWindow.vk.MapMemory(BrowserWindow.device, objectBufferMemory, 0, GetSizeOfObjectDatas(), 0, ptr);
    }



    #endregion

    public virtual DescriptorSetLayout[] GetLayouts()
    {
        return [VulkanManager.descriptorSetLayoutForTextures];
    }

    public virtual unsafe void Render(CommandBuffer commandBuffer, uint currentFrame, KhrPushDescriptor khrPushDescriptor)
    {
        BrowserWindow.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, Pipeline);

        // BrowserWindow.vk.CmdDraw(commandBuffer, 3, 1, 0, 0);

        // set 0 — camera, same for all shaders
        fixed (DescriptorSet* ptr = CameraBuffers.Instance.DescriptorSets)
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
            Size       = sizeof(int) // object index
        }
    ];

    public virtual void CreatePipeline(RenderPass renderPass)
    {
        #region Pipeline layout
        var pushRanges = GetPushConstantRanges();
        var layouts = GetLayouts();


        fixed (PushConstantRange* pushRangesPtr = pushRanges)
        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
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
        }
        #endregion
        
        var vertCode = System.IO.File.ReadAllBytes(moduleShaderPath);

        var shaderModule = CreateShaderModule(vertCode);

        PipelineShaderStageCreateInfo vertStage = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = shaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("VertexMain")
        };

        PipelineShaderStageCreateInfo fragStage = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = shaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("FragmentMain")
        };

        PipelineShaderStageCreateInfo[] stages = [vertStage, fragStage];

        var bindingDesc = Vertex.GetBindingDescription();
        var attributeDescs = Vertex.GetAttributeDescriptions();

        var depthStencil = GetDepthStencil();
        var colorBlend = GetColorBlend();
        var rasterizer = GetRasterizer();
        
        DynamicState[] dynamicStates = [DynamicState.Viewport, DynamicState.Scissor];

        fixed (VertexInputAttributeDescription* attrPtr = attributeDescs)
        fixed (DynamicState* dynPtr = dynamicStates)
        fixed (PipelineShaderStageCreateInfo* stagesPtr = stages)
        fixed(Format* colorFormatPtr = &Swapchain.swapChainImageFormat)
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

            PipelineDynamicStateCreateInfo dynamicState = new()
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = (uint)dynamicStates.Length,
                PDynamicStates = dynPtr,
            };

            PipelineRenderingCreateInfo renderingCI = new()
            {
                SType = StructureType.PipelineRenderingCreateInfo,
                ColorAttachmentCount=1,
                PColorAttachmentFormats = colorFormatPtr,
                DepthAttachmentFormat = Depth.depthFormat,
            };

            PipelineColorBlendStateCreateInfo colorBlendState = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                AttachmentCount=1,
                PAttachments = &colorBlend,
            };

            PipelineMultisampleStateCreateInfo multisampleState = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                RasterizationSamples = SampleCountFlags.Count1Bit,
            };

            GraphicsPipelineCreateInfo pipelineCI = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                PNext = &renderingCI,

                StageCount = (uint)stages.Length,
                PStages = stagesPtr,

                PVertexInputState = &vertexInput,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampleState,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlendState,
                PDynamicState = &dynamicState,
                Layout = PipelineLayout,
            };

            if (BrowserWindow.vk.CreateGraphicsPipelines(BrowserWindow.device, default, 1, &pipelineCI, null, out Pipeline) != Result.Success)
                throw new Exception("Failed to create graphics pipeline!");
        }

        SilkMarshal.FreeString((nint)vertStage.PName);
        SilkMarshal.FreeString((nint)fragStage.PName);
        BrowserWindow.vk.DestroyShaderModule(BrowserWindow.device, shaderModule, null);
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
    }
}