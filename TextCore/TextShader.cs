using System.Drawing;
using GraphicsCore;
using GraphicsCore.Buffers;
using GraphicsCore.Shaders;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using TextCore.Text;
using Vulkan;
using VulkanManager;

namespace TextCore;

public unsafe class TextShader : BaseShader, IShaderFactory<TextShader>
{
    public Dictionary<uint, List<TextContainer>> elements = new();

    public static bool ShowMSDF;
    public static bool ShowLOD;

    InstancesManager textContainerManager;
    InstancesManager textLineManager;

    public TextShader(string shaderPath, InstancesManager textContainerManager, InstancesManager textLineManager) : base(shaderPath)
    {
        this.textContainerManager = textContainerManager;
        this.textLineManager = textLineManager;
    }

    public static TextShader Create(string shaderPath, Func<Type, InstancesManager> objectsManagerFunc)
    {
        return new TextShader(shaderPath, objectsManagerFunc.Invoke(typeof(TextContainerGPUData)), objectsManagerFunc.Invoke(typeof(TextLineGPUData)));
    }

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
            Size       = sizeof(ulong)*4 + sizeof(uint) * 2 // camera ubo & text data & objects ubo
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
        return TextVertex.GetBindingDescription();
    }

    protected internal override VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        return TextVertex.GetAttributeDescriptions();
    }


    public override void Render(TextureRenderer textureRenderer, CommandBuffer commandBuffer, uint frameInFlight, bool wireFrameRendering)
    {
        CreateVulkan.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, wireFrameRendering ? PipelineWireframe : Pipeline);

        CreateVulkan.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, 0, 1, ref TextManager.Instance.textDescriptorSet, 0, null);

        ulong* addresses = stackalloc ulong[3]
        {
            textureRenderer.GetCameraBufferDeviceAddress(),
            textContainerManager.GetBufferDeviceAddress(textureRenderer.GetId(), frameInFlight),
            textLineManager.GetBufferDeviceAddress(textureRenderer.GetId(), frameInFlight),
        };
        CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, 0, sizeof(ulong) * 3, addresses);

        // Parallel.ForEach(elements, item =>
        // {
        //     item.TestToUpdateStyle(frameInFlight);
        // });

        base.Render(textureRenderer, commandBuffer, frameInFlight, wireFrameRendering);
    }

    protected override void RenderElements(uint siteId, CommandBuffer commandBuffer, uint frameInFlight)
    {
        Silk.NET.Vulkan.Buffer _lastVertexBuffer = default;
        Silk.NET.Vulkan.Buffer _lastIndexBuffer = default;
        ulong vOffset = 0;

        uint _dataMask = 0;

        _dataMask |= ShowMSDF ? 1 << 0 : 0u;
        _dataMask |= ShowLOD ? 1 << 1 : 0u;

        foreach (var textContainer in elements[siteId])
        {
            var _siteModelKey = InstancesManager.MakeKey(siteId, textContainer.ModelId);

            textContainerManager.RenderTick(_siteModelKey, frameInFlight);

            textContainer.TryWriteObjectData(textContainerManager.GetDestinationSpan(_siteModelKey, frameInFlight, textContainer.InstanceIndex, textContainer.ObjectDataSize), frameInFlight);

            ulong _characterBufferDeviceAddress = textContainer.fontManager.GetFontAtlas().charactersBuffer.GetBuffer(frameInFlight).DeviceAddress;
            CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, sizeof(ulong) * 3, sizeof(ulong), &_characterBufferDeviceAddress);

            uint _instanceIndex = textContainer.InstanceIndex;
            CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, sizeof(ulong) * 4, sizeof(uint), &_instanceIndex);
            CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, sizeof(ulong) * 4 + sizeof(uint), sizeof(uint), ref _dataMask);

            foreach (var textLine in textContainer.runtimeTexts)
            {
                var _siteModelKeyTextLine = InstancesManager.MakeKey(siteId, textLine.ModelId);
                textLine.TryWriteObjectData(textLineManager.GetDestinationSpan(_siteModelKeyTextLine, frameInFlight, textLine.InstanceIndex, textContainer.ObjectDataSize), frameInFlight);
                
                var _vertexBuffer = textLine.VertexSlotData.GetRingBuffer().buffersInfo[frameInFlight].Buffer;
                var _indexBuffer = textLine.IndicesSlotData.GetRingBuffer().buffersInfo[frameInFlight].Buffer;

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
                CreateVulkan.vk.CmdDrawIndexed(commandBuffer, (uint)textLine.IndicesSlotData.GetDataCount(), 1, textLine.IndicesSlotData.GetSlot().Offset, (int)textLine.VertexSlotData.GetSlot().Offset, textLine.InstanceIndex);
            }
        }
    }

    /// <summary>
    /// Remember to register the site first
    /// </summary>
    /// <param name="siteId"></param>
    /// <param name="runtimeModelData"></param>
    public override void AddElement(TextureRenderer textureRenderer, VisualElement visualElement)
    {
        if (visualElement is null) throw new ArgumentException("visual element must be set");
        if (visualElement is not TextContainer textContainer) throw new ArgumentException("Node shader only allows Node class and not " + visualElement.GetType());

        elements[textureRenderer.GetId()].Add(textContainer);
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