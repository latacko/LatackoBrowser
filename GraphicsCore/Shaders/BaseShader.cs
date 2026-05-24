using Silk.NET.Assimp;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Units;
using Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace GraphicCore;

public unsafe abstract class BaseShader : IDisposable
{
    public Pipeline Pipeline;
    public PipelineLayout PipelineLayout;
    public List<RuntimeModelData> elements = new();

    private protected virtual int GetMaxObjectForShader() => 10000;
    private protected virtual ulong GetSizeOfObjectDatas() => (ulong)(sizeof(ObjectData) * GetMaxObjectForShader());

    protected abstract string moduleShaderPath { get; }


    #region Init
    public virtual void Init()
    {
        CreatePipeline();
    }



    #endregion

    public virtual DescriptorSetLayout[] GetLayouts()
    {
        return [VulkanManager.descriptorSetLayoutForTextures];
    }

    public abstract unsafe void Render(CommandBuffer commandBuffer, uint currentFrame);

    protected abstract void RenderElements(CommandBuffer commandBuffer, uint currentFrame);

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
            StageFlags = ShaderStageFlags.VertexBit,
            Size       = sizeof(ulong)*2 // camera ubo & objects ubo
        },
    ];

    public virtual void CreatePipeline()
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


            if (CreateVulkan.vk.CreatePipelineLayout(LogicalDevice.device, &layoutInfo, null, out PipelineLayout) != Result.Success)
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
        fixed (Format* colorFormatPtr = &Swapchain.swapChainImageFormat)
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
                ColorAttachmentCount = 1,
                PColorAttachmentFormats = colorFormatPtr,
                DepthAttachmentFormat = Depth.depthFormat,
            };

            PipelineColorBlendStateCreateInfo colorBlendState = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                AttachmentCount = 1,
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

            if (CreateVulkan.vk.CreateGraphicsPipelines(LogicalDevice.device, default, 1, &pipelineCI, null, out Pipeline) != Result.Success)
                throw new Exception("Failed to create graphics pipeline!");
        }

        SilkMarshal.FreeString((nint)vertStage.PName);
        SilkMarshal.FreeString((nint)fragStage.PName);
        CreateVulkan.vk.DestroyShaderModule(LogicalDevice.device, shaderModule, null);
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

            CreateVulkan.vk.CreateShaderModule(LogicalDevice.device, &createInfo, null, out ShaderModule module);
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

        CreateVulkan.vk.DestroyPipeline(LogicalDevice.device, Pipeline, null);
        CreateVulkan.vk.DestroyPipelineLayout(LogicalDevice.device, PipelineLayout, null);
    }
}