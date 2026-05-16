using Browser;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Vulkan;

public unsafe class UIShader : BaseShader
{
    protected override string moduleShaderPath => "shaders/uiShader.spv";

    private protected override int GetMaxObjectForShader() => 1000;

    public override void Init()
    {
        base.Init();
    }

    // // override depth — UI has no depth test
    protected override PipelineDepthStencilStateCreateInfo GetDepthStencil() => new()
    {
        SType = StructureType.PipelineDepthStencilStateCreateInfo,
        DepthTestEnable = Vk.False,
        DepthWriteEnable = Vk.False,
        StencilTestEnable = Vk.False,
    };

    // // override blend — UI needs alpha
    // protected override PipelineColorBlendAttachmentState GetColorBlend() => new()
    // {
    //     ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit |
    //                           ColorComponentFlags.BBit | ColorComponentFlags.ABit,
    //     BlendEnable = Vk.True,
    //     SrcColorBlendFactor = BlendFactor.SrcAlpha,
    //     DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
    //     ColorBlendOp = BlendOp.Add,
    //     SrcAlphaBlendFactor = BlendFactor.One,
    //     DstAlphaBlendFactor = BlendFactor.Zero,
    //     AlphaBlendOp = BlendOp.Add,
    // };

    protected override PipelineRasterizationStateCreateInfo GetRasterizer() => new()
    {
        SType = StructureType.PipelineRasterizationStateCreateInfo,
        DepthClampEnable = Vk.False,
        RasterizerDiscardEnable = Vk.False,
        PolygonMode = PolygonMode.Fill,
        LineWidth = 1f,
        // CullMode = CullModeFlags.BackBit,
        CullMode = CullModeFlags.None,
        FrontFace = FrontFace.CounterClockwise,
        DepthBiasEnable = Vk.False,
    };


    protected override void RenderElements(CommandBuffer commandBuffer, uint currentFrame)
    {
        foreach (var element in elements)
        {
            if (element.TryGetObjectData(out var data, currentFrame))
            {
                VulkanManager.Instance.objectsBuffers.Update(currentFrame, element.ObjectIndex, data);
            }
            CreateVulkan.vk.CmdDrawIndexed(commandBuffer, (uint)element.ModelData.GetIndicesCount(), 1, element.ModelData.indexOffset, (int)element.ModelData.vertexOffset, element.ObjectIndex);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}