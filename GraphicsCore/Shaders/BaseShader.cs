using GraphicsCore.Buffers;
using Silk.NET.Assimp;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Units;
using Vulkan;
using VulkanManager;
using VulkanManager.BufferManager;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace GraphicsCore;

public unsafe abstract class BaseShader : IDisposable
{
    public Pipeline Pipeline;
    public PipelineLayout PipelineLayout;
    public Pipeline PipelineWireframe;

    public static readonly Dictionary<Type, InstancesManager> InstancesManagerByType = [];

    protected readonly string moduleShaderPath;

    public BaseShader(string shaderPath)
    {
        moduleShaderPath = "Shaders/" + shaderPath + ".spv";
    }

    #region Init
    public virtual void Init()
    {
        CreatePipeline();

#if DEBUG
        CreatePipeline(true);
#endif
    }



    #endregion

    public abstract DescriptorSetLayout[] GetLayouts();

    public virtual void Render(TextureRenderer textureRenderer, CommandBuffer commandBuffer, uint frameInFlight, bool wireFrameRendering)
    {
        RenderElements(textureRenderer.GetId(), commandBuffer, frameInFlight);
    }

    protected abstract void RenderElements(uint siteId, CommandBuffer commandBuffer, uint frameInFlight);

    #region Graphic pipeline
    protected virtual PipelineDepthStencilStateCreateInfo GetDepthStencil() => new(
        depthTestEnable: Vk.True,
        depthWriteEnable: Vk.True,
        depthCompareOp: CompareOp.Less,
        depthBoundsTestEnable: Vk.False,
        stencilTestEnable: Vk.False
    );

    protected virtual PipelineColorBlendAttachmentState GetColorBlend() => new(
        colorWriteMask: ColorComponentFlags.RBit | ColorComponentFlags.GBit |
                        ColorComponentFlags.BBit | ColorComponentFlags.ABit,
        blendEnable: Vk.False
    );

    protected virtual PipelineRasterizationStateCreateInfo GetRasterizer(bool wireFrame) => new(
        depthClampEnable: Vk.False,
        rasterizerDiscardEnable: Vk.False,
        polygonMode: wireFrame ? PolygonMode.Line : PolygonMode.Fill,
        lineWidth: 1f,
        cullMode: CullModeFlags.BackBit,
        // CullMode = CullModeFlags.None,
        frontFace: FrontFace.CounterClockwise,
        depthBiasEnable: Vk.False
    );

    // push constant ranges — override if needed
    protected virtual PushConstantRange[] GetPushConstantRanges() => [
        new(
            stageFlags : ShaderStageFlags.VertexBit,
            size       : sizeof(ulong)*2 // camera ubo & objects ubo
        )
    ];

    protected internal abstract VertexInputBindingDescription GetBindingDescription();
    protected internal abstract VertexInputAttributeDescription[] GetAttributeDescriptions();

    protected internal virtual PipelineInputAssemblyStateCreateInfo GetPipelineInputAssemblyStateCreateInfo() => new(
        topology: PrimitiveTopology.TriangleList,
        primitiveRestartEnable: Vk.False
    );

    public virtual void CreatePipeline(bool wireFrameRendering = false)
    {
        #region Pipeline layout
        var pushRanges = GetPushConstantRanges();
        var layouts = GetLayouts();


        if (!wireFrameRendering)
        {
            fixed (PushConstantRange* pushRangesPtr = pushRanges)
            fixed (DescriptorSetLayout* layoutsPtr = layouts)
            {
                PipelineLayoutCreateInfo layoutInfo = new(
                    setLayoutCount: (uint)layouts.Length,
                    pSetLayouts: layoutsPtr,
                    pushConstantRangeCount: (uint)pushRanges.Length,
                    pPushConstantRanges: pushRangesPtr
                );

                if (CreateVulkan.vk.CreatePipelineLayout(LogicalDevice.device, &layoutInfo, null, out PipelineLayout) != Result.Success)
                    throw new Exception("Failed to create pipeline layout!");

            }
        }

        #endregion

        var vertCode = System.IO.File.ReadAllBytes(moduleShaderPath);



        var shaderModule = CreateShaderModule(vertCode);

        PipelineShaderStageCreateInfo vertStage = new(
            stage: ShaderStageFlags.VertexBit,
            module: shaderModule,
            pName: (byte*)SilkMarshal.StringToPtr("VertexMain")
        );

        PipelineShaderStageCreateInfo fragStage = new(
            stage: ShaderStageFlags.FragmentBit
        );

        var wireframeCode = System.IO.File.ReadAllBytes(Path.GetDirectoryName(moduleShaderPath) + "/wireFrameShader.spv");
        var wireframeShaderModule = CreateShaderModule(wireframeCode);
        if (wireFrameRendering)
        {
            fragStage.PName = (byte*)SilkMarshal.StringToPtr("main");
            fragStage.Module = wireframeShaderModule;
        }
        else
        {
            fragStage.PName = (byte*)SilkMarshal.StringToPtr("FragmentMain");
            fragStage.Module = shaderModule;
        }

        PipelineShaderStageCreateInfo[] stages = [vertStage, fragStage];

        var bindingDesc = GetBindingDescription();
        var attributeDescs = GetAttributeDescriptions();

        var depthStencil = GetDepthStencil();
        var colorBlend = GetColorBlend();
        var rasterizer = GetRasterizer(wireFrameRendering);

        DynamicState[] dynamicStates = [DynamicState.Viewport, DynamicState.Scissor];

        fixed (VertexInputAttributeDescription* attrPtr = attributeDescs)
        fixed (DynamicState* dynPtr = dynamicStates)
        fixed (PipelineShaderStageCreateInfo* stagesPtr = stages)
        fixed (Format* colorFormatPtr = &Swapchain.swapChainImageFormat)
        {

            PipelineVertexInputStateCreateInfo vertexInput = new(
                vertexBindingDescriptionCount: 1,
                pVertexBindingDescriptions: &bindingDesc,

                vertexAttributeDescriptionCount: (uint)attributeDescs.Length,
                pVertexAttributeDescriptions: attrPtr
            );

            PipelineInputAssemblyStateCreateInfo inputAssembly = GetPipelineInputAssemblyStateCreateInfo();

            PipelineViewportStateCreateInfo viewportState = new(
                viewportCount: 1,
                scissorCount: 1
            );

            PipelineDynamicStateCreateInfo dynamicState = new(
                dynamicStateCount: (uint)dynamicStates.Length,
                pDynamicStates: dynPtr
            );

            PipelineRenderingCreateInfo renderingCI = new(
                colorAttachmentCount: 1,
                pColorAttachmentFormats: colorFormatPtr,
                depthAttachmentFormat: Depth.depthFormat
            );

            PipelineColorBlendStateCreateInfo colorBlendState = new(
                attachmentCount: 1,
                pAttachments: &colorBlend
            );

            PipelineMultisampleStateCreateInfo multisampleState = new(
                rasterizationSamples: SampleCountFlags.Count1Bit
            );

            GraphicsPipelineCreateInfo pipelineCI = new(
                pNext: &renderingCI,

                stageCount: (uint)stages.Length,
                pStages: stagesPtr,

                pVertexInputState: &vertexInput,
                pInputAssemblyState: &inputAssembly,
                pViewportState: &viewportState,
                pRasterizationState: &rasterizer,
                pMultisampleState: &multisampleState,
                pDepthStencilState: &depthStencil,
                pColorBlendState: &colorBlendState,
                pDynamicState: &dynamicState,
                layout: PipelineLayout
            );

            Pipeline _pipeline;
            if (CreateVulkan.vk.CreateGraphicsPipelines(LogicalDevice.device, default, 1, &pipelineCI, null, out _pipeline) != Result.Success)
                throw new Exception("Failed to create graphics pipeline!");

            if (wireFrameRendering)
                PipelineWireframe = _pipeline;
            else
                Pipeline = _pipeline;
        }

        SilkMarshal.FreeString((nint)vertStage.PName);
        SilkMarshal.FreeString((nint)fragStage.PName);
        CreateVulkan.vk.DestroyShaderModule(LogicalDevice.device, shaderModule, null);
        CreateVulkan.vk.DestroyShaderModule(LogicalDevice.device, wireframeShaderModule, null);
    }

    ShaderModule CreateShaderModule(byte[] code)
    {
        fixed (byte* codePtr = code)
        {
            ShaderModuleCreateInfo createInfo = new(
                codeSize: (nuint)code.Length,
                pCode: (uint*)codePtr
            );

            CreateVulkan.vk.CreateShaderModule(LogicalDevice.device, &createInfo, null, out ShaderModule module);
            return module;
        }
    }
    #endregion

    public abstract void CompileElement(TextureRenderer textureRenderer, VisualElement visualElement);

    public abstract void AddElement(TextureRenderer textureRenderer, VisualElement runtimeModelData);
    public abstract void AddSite(TextureRenderer textureRenderer);

    public virtual void Dispose()
    {
        CreateVulkan.vk.DestroyPipeline(LogicalDevice.device, Pipeline, null);
        CreateVulkan.vk.DestroyPipeline(LogicalDevice.device, PipelineWireframe, null);
        CreateVulkan.vk.DestroyPipelineLayout(LogicalDevice.device, PipelineLayout, null);
    }
}