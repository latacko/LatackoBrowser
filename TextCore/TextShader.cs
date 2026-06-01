using System.Drawing;
using GraphicCore;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Vulkan;

namespace TextCore;

public unsafe class TextShader : BaseShader
{
    public List<RuntimeTextContainer> elements = new();
    protected override string moduleShaderPath => "shaders/Compiled/textShader.spv";

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
            StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            Size       = sizeof(ulong)*4 + sizeof(uint)  // camera ubo & text data & objects ubo
        },
    ];

    bool isWireFrameRendering = false;

    protected override void RenderElements(CommandBuffer commandBuffer, uint currentFrame)
    {
        foreach (var element in elements)
        {
            if (element.TryGetObjectData(out var textData, currentFrame))
            {
                TextManager.Instance.Update(currentFrame, element.ObjectIndex, textData);
            }

            fixed (uint* objectIndexPtr = &element.ObjectIndex)
                CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, sizeof(ulong) * 4, sizeof(uint), objectIndexPtr);

            fixed (ulong* deviceAddressPtr = &element.fontAtlas.charactersBuffer.DeviceAddress)
                CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, sizeof(ulong) * 3, sizeof(ulong), deviceAddressPtr);
            lock (element.runtimeTexts)
            {
                foreach (var runtimeText in element.runtimeTexts)
                {

                    if (runtimeText.TryGetObjectData(out var modelData, currentFrame))
                    {
                        TextManager.Instance.Update(currentFrame, runtimeText.ObjectIndex, modelData);
                        // Console.WriteLine($"charactersBiffer.DeviceAddress = {element.fontAtlas.charactersBuffer.DeviceAddress}");
                    }
                    CreateVulkan.vk.CmdDrawIndexed(commandBuffer, (uint)runtimeText.ModelData.GetIndicesCount(), 1, runtimeText.ModelData.indexOffset, (int)runtimeText.ModelData.vertexOffset, runtimeText.ObjectIndex);
                }
            }
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

        ulong* addresses = stackalloc ulong[3]
        {
            VulkanEngine.Instance.cameraBuffers.shaderDataBuffersForCamera[currentFrame].DeviceAddress,
            TextManager.Instance.textContainerDataBuffer[currentFrame].DeviceAddress,
            TextManager.Instance.textModelDataBuffer[currentFrame].DeviceAddress,
        };
        CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, 0, sizeof(ulong) * 3, addresses);

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