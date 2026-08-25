using AssetCore;
using GraphicsCore;
using GraphicsCore.Buffers;
using GraphicsCore.Shaders;
using PrimitiveCore.Model;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Vulkan;
using VulkanManager;
using VulkanManager.BufferManager;

namespace PrimitiveCore;

public unsafe class NodeShader : BaseShader, IShaderFactory<NodeShader>
{
    public Dictionary<uint, Dictionary<uint, List<Node>>> elements = [];
    // protected override string moduleShaderPath => "shaders/Compiled/uiShader.spv";
    


    public NodeShader(string shaderPath, InstancesManager objectsManager):base(shaderPath, objectsManager)
    {
    }

    public static NodeShader Create(string shaderPath, Func<Type, InstancesManager> objectsManagerFunc)
    {
        return new NodeShader(shaderPath, objectsManagerFunc.Invoke(typeof(ModelGPUData)));
    }

    public override void Init()
    {
        base.Init();
    }


    #region Shader Data
    public override DescriptorSetLayout[] GetLayouts()
    {
        return [PrimitiveInstancesManager.Instance.objectDescriptorLayout];
    }

    protected internal override VertexInputBindingDescription GetBindingDescription()
    {
        return MeshVertex.GetBindingDescription();
    }

    protected internal override VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        return MeshVertex.GetAttributeDescriptions();
    }
    #endregion

    #region Rendering Options

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
    #endregion

    public override void Render(TextureRenderer textureRenderer, CommandBuffer commandBuffer, uint currentFrame, bool wireFrameRendering)
    {
        CreateVulkan.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, wireFrameRendering ? PipelineWireframe : Pipeline);

        CreateVulkan.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, 0, 1, ref PrimitiveInstancesManager.Instance.objectDescriptorSet, 0, null);

        ulong vOffset = 0;
        CreateVulkan.vk.CmdBindVertexBuffers(commandBuffer, 0, 1, ref PrimitiveModelsDb.primitiveBuffer, ref vOffset);
        CreateVulkan.vk.CmdBindIndexBuffer(commandBuffer, PrimitiveModelsDb.primitiveBuffer, PrimitiveModelsDb.indicesOffset, IndexType.Uint16);

        ulong _cameraBuffer = textureRenderer.GetCameraBufferDeviceAddress();
        CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit, 0, sizeof(ulong), &_cameraBuffer);

        // Parallel.ForEach(elements, item =>
        // {
        //     item.TestToUpdateStyle(currentFrame);
        // });

        base.Render(textureRenderer, commandBuffer, currentFrame, wireFrameRendering);
    }

    protected override void RenderElements(uint siteId, CommandBuffer commandBuffer, uint currentFrame)
    {
        foreach (var (modelId, elements) in elements[siteId])
        {
            var _siteModelKey = InstancesManager.MakeKey(siteId, modelId);
            foreach (var element in elements)
            {
                element.TryWriteObjectData(objectsManager!.GetDestinationSpan(_siteModelKey, currentFrame, element.InstanceIndex, element.ObjectDataSize), currentFrame);
            }
            
            objectsManager!.RenderTick(_siteModelKey, currentFrame);

            ulong _modelBuffer = objectsManager!.GetBufferDeviceAddress(_siteModelKey, modelId);
            CreateVulkan.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit, sizeof(ulong), sizeof(ulong) * 1, &_modelBuffer);

            var _meshData = AssetManager.GetModel(modelId);
            CreateVulkan.vk.CmdDrawIndexed(commandBuffer, (uint)_meshData.GetIndicesCount(), (uint)elements.Count, _meshData.indexOffset, (int)_meshData.vertexOffset, 0);
        }
    }

    /// <summary>
    /// Remember to register the site first
    /// </summary>
    /// <param name="siteId"></param>
    /// <param name="node"></param>
    public override void AddElement(TextureRenderer textureRenderer, VisualElement visualElement)
    {
        if (visualElement is null) throw new ArgumentException("visual element must be set");
        if (visualElement is not Node node) throw new ArgumentException("Node shader only allows Node class and not " + visualElement.GetType());

        if (!elements[textureRenderer.GetId()].TryAdd(node.ModelId, [node]))
            elements[textureRenderer.GetId()][node.ModelId].Add(node);
    }

    public override void AddSite(TextureRenderer textureRenderer)
    {
        elements.Add(textureRenderer.GetId(), []);
    }

    public override void Dispose()
    {
        base.Dispose();
        foreach (var (_, siteElements) in elements)
        {
            foreach (var (_, models) in siteElements)
            {
                foreach (var modelInstance in models)
                {
                    modelInstance.Dispose();
                }
            }
        }
    }
}