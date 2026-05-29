using GraphicCore;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Vulkan;

namespace TextCore;

public unsafe class TextShader : BaseShader
{
    public List<RuntimeText> elements = new();
    protected override string moduleShaderPath => "shaders/Compiled/textShader.spv";

    protected override int GetMaxObjectForShader() => 1000;
    protected override ulong GetSizeOfObjectDatas() => (ulong)(sizeof(TextData) * GetMaxObjectForShader());

    public override void Init()
    {
        base.Init();
    }

    public override DescriptorSetLayout[] GetLayouts()
    {
        return [TextManager.Instance.textDescriptorLayout];
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

    protected override PushConstantRange[] GetPushConstantRanges() => [
        new()
        {
            StageFlags = ShaderStageFlags.VertexBit,
            Size       = sizeof(ulong)*2 // camera ubo & objects ubo
        },
        new()
        {
            StageFlags = ShaderStageFlags.FragmentBit,
            Offset = sizeof(ulong)*2,
            Size       = sizeof(ulong) // characters data
        },
    ];

    bool isWireFrameRendering = false;

    protected override void RenderElements(CommandBuffer commandBuffer, uint currentFrame)
    {
        foreach (var element in elements)
        {
            if (element.TryGetObjectData(out var data, currentFrame))
            {
                TextManager.Instance.Update(currentFrame, element.ObjectIndex, data);
                Console.WriteLine($"charactersBiffer.DeviceAddress = {element.fontAtlas.charactersBiffer.DeviceAddress}");
            }

            fixed (ulong* deviceAddressPtr = &element.fontAtlas.charactersBiffer.DeviceAddress)
                CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.FragmentBit, sizeof(ulong)*2, sizeof(ulong), deviceAddressPtr);

            CreateVulkan.vk.CmdDrawIndexed(commandBuffer, (uint)element.ModelData.GetIndicesCount(), 1, element.ModelData.indexOffset, (int)element.ModelData.vertexOffset, element.ObjectIndex);
        }
    }

    public override void Render(CommandBuffer commandBuffer, uint currentFrame, bool wireFrameRendering)
    {
        this.isWireFrameRendering = wireFrameRendering;
        CreateVulkan.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, wireFrameRendering ? PipelineWireframe : Pipeline);

        CreateVulkan.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, 0, 1, ref TextManager.Instance.textDescriptorSet, 0, null);

        ulong vOffset = 0;
        CreateVulkan.vk.CmdBindVertexBuffers(commandBuffer, 0, 1, ref TextManager.Instance.vertexBuffer[currentFrame].Buffer, ref vOffset);
        CreateVulkan.vk.CmdBindIndexBuffer(commandBuffer, TextManager.Instance.indicesBuffer[currentFrame].Buffer, 0, IndexType.Uint16);

        ulong* addresses = stackalloc ulong[2]
        {
            VulkanEngine.Instance.cameraBuffers.shaderDataBuffersForCamera[currentFrame].DeviceAddress,
            TextManager.Instance.dataBuffer[currentFrame].DeviceAddress,
        };
        CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit, 0, sizeof(ulong) * 2, addresses);

        RenderElements(commandBuffer, currentFrame);
    }

    public override void Dispose()
    {
        foreach (var element in elements)
        {
            element.Dispose();
        }
        base.Dispose();
    }

    protected internal override VertexInputBindingDescription GetBindingDescription()
    {
        return new TextVertex().GetBindingDescription();
    }

    protected internal override VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        return new TextVertex().GetAttributeDescriptions();
    }
}