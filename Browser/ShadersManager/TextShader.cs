using Browser;
using GraphicCore;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Vulkan;

public unsafe class TextShader : BaseShader
{
    protected override string moduleShaderPath => "shaders/Compiled/textShader.spv";

    protected override int GetMaxObjectForShader() => 1000;

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
    protected override PipelineColorBlendAttachmentState GetColorBlend() => new()
    {
        ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit |
                        ColorComponentFlags.BBit | ColorComponentFlags.ABit,
        BlendEnable = Vk.True,
        SrcColorBlendFactor = BlendFactor.SrcAlpha,
        DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
        ColorBlendOp = BlendOp.Add,
        SrcAlphaBlendFactor = BlendFactor.One,
        DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
        AlphaBlendOp = BlendOp.Add,
    };

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

    public override void Render(CommandBuffer commandBuffer, uint currentFrame)
    {
        CreateVulkan.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, Pipeline);

        CreateVulkan.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, 0, 1, ref VulkanManager.descriptorSetForTextures, 0, null);

        ulong vOffset = 0;
        CreateVulkan.vk.CmdBindVertexBuffers(commandBuffer, 0, 1, ref PrimitiveModelsDb.primitiveBuffer, ref vOffset);
        CreateVulkan.vk.CmdBindIndexBuffer(commandBuffer, PrimitiveModelsDb.primitiveBuffer, PrimitiveModelsDb.indicesOffset, IndexType.Uint16);

        ulong* addresses = stackalloc ulong[2]
        {
            VulkanManager.Instance.cameraBuffers.shaderDataBuffersForCamera[currentFrame].DeviceAddress,
            VulkanManager.Instance.objectsBuffers.shaderDataBuffersForObjects[currentFrame].DeviceAddress,
        };
        CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit, 0, sizeof(ulong) * 2, addresses);

        RenderElements(commandBuffer, currentFrame);
    }
}