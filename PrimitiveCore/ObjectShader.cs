using GraphicsCore;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Vulkan;
using VulkanManager;

namespace PrimitiveCore;

public unsafe class ObjectShader : GraphicsCore.BaseShader
{
    public Dictionary<uint, Dictionary<uint, List<RuntimeObject>>> elements = [];
    protected override string moduleShaderPath => "shaders/Compiled/uiShader.spv";

    public override void Init()
    {
        base.Init();
    }

    public override DescriptorSetLayout[] GetLayouts()
    {
        return [PrimitiveInstancesManager.Instance.objectDescriptorLayout];
    }

    protected internal override VertexInputBindingDescription GetBindingDescription()
    {
        return new ModelVertex().GetBindingDescription();
    }

    protected internal override VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        return new ModelVertex().GetAttributeDescriptions();
    }

    public override unsafe void Render(TextureRenderer textureRenderer, CommandBuffer commandBuffer, uint currentFrame, bool wireFrameRendering)
    {
        CreateVulkan.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, wireFrameRendering ? PipelineWireframe : Pipeline);

        CreateVulkan.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, 0, 1, ref PrimitiveInstancesManager.Instance.objectDescriptorSet, 0, null);

        ulong vOffset = 0;
        CreateVulkan.vk.CmdBindVertexBuffers(commandBuffer, 0, 1, ref PrimitiveModelsDb.primitiveBuffer, ref vOffset);
        CreateVulkan.vk.CmdBindIndexBuffer(commandBuffer, PrimitiveModelsDb.primitiveBuffer, PrimitiveModelsDb.indicesOffset, IndexType.Uint16);

        ulong* addresses = stackalloc ulong[2]
        {
            textureRenderer.GetCameraBufferDeviceAddress(),
            ObjectsManager.Instance.shaderDataBuffersForObjects[currentFrame].DeviceAddress,
        };
        CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit, 0, sizeof(ulong) * 2, addresses);

        // Parallel.ForEach(elements, item =>
        // {
        //     item.TestToUpdateStyle(currentFrame);
        // });

        RenderElements(siteId, commandBuffer, currentFrame);
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

    protected override PipelineRasterizationStateCreateInfo GetRasterizer(bool wireframe) => new()
    {
        SType = StructureType.PipelineRasterizationStateCreateInfo,
        DepthClampEnable = Vk.False,
        RasterizerDiscardEnable = Vk.False,
        PolygonMode = wireframe ? PolygonMode.Line : PolygonMode.Fill,
        LineWidth = 1f,
        // CullMode = CullModeFlags.BackBit,
        CullMode = CullModeFlags.None,
        FrontFace = FrontFace.CounterClockwise,
        DepthBiasEnable = Vk.False,
    };

    protected override void RenderElements(uint siteId, CommandBuffer commandBuffer, uint currentFrame)
    {
        foreach (var element in elements[siteId])
        {
            if (element.TryGetObjectData(out var data, currentFrame))
            {
                ObjectsManager.Instance.Update(currentFrame, element.ObjectIndex, data);
            }
            CreateVulkan.vk.CmdDrawIndexed(commandBuffer, (uint)element.ModelData.GetIndicesCount(), 1, element.ModelData.indexOffset, (int)element.ModelData.vertexOffset, element.ObjectIndex);
        }
    }

    /// <summary>
    /// Remember to register the site first
    /// </summary>
    /// <param name="siteId"></param>
    /// <param name="runtimeModelData"></param>
    public override void AddElement(TextureRenderer textureRenderer, RuntimeModelData runtimeModelData)
    {
        elements[textureRenderer.GetId()].Add(runtimeModelData as RuntimeObject);
    }

    public override void AddSite(TextureRenderer textureRenderer)
    {
        elements.Add(textureRenderer.GetId(), []);
    }

    public override void Dispose()
    {
        foreach (var siteElements in elements)
        {
            foreach (var element in siteElements.Value)
            {
                element.Dispose();
            }
        }
        base.Dispose();
    }
}