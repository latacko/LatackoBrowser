using System.Drawing;
using GraphicsCore;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Vulkan;

namespace TextCore;

public unsafe class TextShader : BaseShader
{
    public Dictionary<uint, List<RuntimeTextContainer>> elements = new();
    protected override string moduleShaderPath => "shaders/Compiled/textShader.spv";

    public static bool ShowMSDF;
    public static bool ShowLOD;
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
            Size       = sizeof(ulong)*4 + sizeof(uint) * 3  // camera ubo & text data & objects ubo
        },
    ];

    protected internal override PipelineInputAssemblyStateCreateInfo GetPipelineInputAssemblyStateCreateInfo() => new()
    {
        SType = StructureType.PipelineInputAssemblyStateCreateInfo,
        Topology = PrimitiveTopology.TriangleStrip,
        PrimitiveRestartEnable = Vk.False,
    };

    protected internal override VertexInputBindingDescription GetBindingDescription()
    {
        return new TextVertex().GetBindingDescription();
    }

    protected internal override VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        return new TextVertex().GetAttributeDescriptions();
    }


    public override void Render(uint siteId, CommandBuffer commandBuffer, uint currentFrame, bool wireFrameRendering)
    {
        CreateVulkan.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, wireFrameRendering ? PipelineWireframe : Pipeline);

        CreateVulkan.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, 0, 1, ref TextManager.Instance.textDescriptorSet, 0, null);

        ulong vOffset = 0;
        // CreateVulkan.vk.CmdBindVertexBuffers(commandBuffer, 0, 1, ref TextManager.Instance.vertexBuffer[currentFrame].Buffer, ref vOffset);
        // CreateVulkan.vk.CmdBindIndexBuffer(commandBuffer, TextManager.Instance.indicesBuffer[currentFrame].Buffer, 0, IndexType.Uint16);

        ulong* addresses = stackalloc ulong[3]
        {
            VulkanEngine.Instance.cameraBuffers.shaderDataBuffersForCamera[currentFrame].DeviceAddress,
            TextManager.Instance.textContainerDataBuffer[currentFrame].DeviceAddress,
            TextManager.Instance.textModelDataBuffer[currentFrame].DeviceAddress,
        };
        CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, 0, sizeof(ulong) * 3, addresses);

        // Parallel.ForEach(elements, item =>
        // {
        //     item.TestToUpdateStyle(currentFrame);
        // });

        RenderElements(siteId, commandBuffer, currentFrame);
    }

    protected override void RenderElements(uint siteId, CommandBuffer commandBuffer, uint currentFrame)
    {
        Silk.NET.Vulkan.Buffer _lastVertexBuffer = default;
        Silk.NET.Vulkan.Buffer _lastIndexBuffer = default;
        ulong _lastFontAtlasAddress = 0;
        ulong vOffset = 0;

        uint _shouldShowMSDF = ShowMSDF ? 1u : 0;
        uint _shouldShowLOD = ShowLOD ? 1u : 0;

        foreach (var element in elements[siteId])
        {
            if (element.TryGetObjectData(out var textData, currentFrame))
            {
                TextManager.Instance.Update(currentFrame, element.InstanceIndex, textData);
            }

            fixed (uint* objectIndexPtr = &element.InstanceIndex)
                CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, sizeof(ulong) * 4, sizeof(uint), objectIndexPtr);

            CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, sizeof(ulong) * 4 + sizeof(uint), sizeof(uint), ref _shouldShowMSDF);
            CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, sizeof(ulong) * 4 + sizeof(uint) * 2, sizeof(uint), ref ShowLOD);

            fixed (ulong* deviceAddressPtr = &element.fontAtlas.charactersBuffer.DeviceAddress)
                CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, sizeof(ulong) * 3, sizeof(ulong), deviceAddressPtr);

            lock (element.runtimeTexts)
            {
                foreach (var runtimeText in element.runtimeTexts)
                {

                    if (runtimeText.TryGetObjectData(out var modelData, currentFrame))
                    {
                        TextManager.Instance.Update(currentFrame, runtimeText.InstanceIndex, modelData);
                        // Console.WriteLine($"charactersBiffer.DeviceAddress = {element.fontAtlas.charactersBuffer.DeviceAddress}");
                    }

                    var _vertexBuffer = runtimeText.VertexSlotData.GetRingBuffer().buffersInfo[currentFrame].Buffer;
                    var _indexBuffer = runtimeText.IndicesSlotData.GetRingBuffer().buffersInfo[currentFrame].Buffer;

                    if (_lastVertexBuffer.Handle != _vertexBuffer.Handle)
                    {
                        _lastVertexBuffer = _vertexBuffer;
                        CreateVulkan.vk.CmdBindVertexBuffers(commandBuffer, 0, 1, ref _vertexBuffer, ref vOffset);
                    }

                    if (_lastIndexBuffer.Handle != _indexBuffer.Handle)
                    {
                        _lastIndexBuffer = _indexBuffer;
                        CreateVulkan.vk.CmdBindIndexBuffer(commandBuffer, _indexBuffer, 0, IndexType.Uint16);
                    }
                    CreateVulkan.vk.CmdDrawIndexed(commandBuffer, (uint)runtimeText.IndicesSlotData.GetDataCount(), 1, runtimeText.IndicesSlotData.GetSlot().Offset, (int)runtimeText.VertexSlotData.GetSlot().Offset, runtimeText.InstanceIndex);
                }
            }
        }
    }

    /// <summary>
    /// Remember to register the site first
    /// </summary>
    /// <param name="siteId"></param>
    /// <param name="runtimeModelData"></param>
    public override void AddElement(uint siteId, RuntimeModelData runtimeModelData)
    {
        elements[siteId].Add(runtimeModelData as RuntimeTextContainer);
    }

    public override void AddSite(uint siteId)
    {
        elements.Add(siteId, []);
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