using Browser;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

public unsafe class UIShader : BaseShader
{
    public Image TextureImage;
    public DeviceMemory TextureImageMemory;
    public ImageView TextureImageView;

    protected override string moduleShaderPath => "shaders/uiShader.spv";

    private protected override int GetMaxObjectForShader() => 1000;

    public override void Init()
    {
        BrowserWindow.Instance.CreateTextureImage("textures/viking_room.png", ref TextureImage, ref TextureImageMemory);
        TextureImageView = BrowserWindow.Instance.CreateTextureImageView(TextureImage);
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


    protected override unsafe void RenderElements(CommandBuffer commandBuffer, uint currentFrame, KhrPushDescriptor khrPushDescriptor)
    {
        foreach (var element in elements)
        {
            if (element.TryGetObjectData(out var data))
            {
                // write into storage buffer at element's index
                var bufferData = (ObjectData*)persistentMappedObjectBuffer + element.ObjectIndex;
                *bufferData = data;
            }
        }

        // draw
        foreach (var element in elements)
        {
            element.ModelData.BindVertexBuffers(BrowserWindow.vk, commandBuffer);
            element.ModelData.BindIndexBuffer(BrowserWindow.vk, commandBuffer);

            // DescriptorImageInfo imageInfo = new()
            // {
            //     ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            //     ImageView = element.TextureImageView,
            //     Sampler = BrowserWindow.textureSampler,
            // };

            // WriteDescriptorSet write = new()
            // {
            //     SType = StructureType.WriteDescriptorSet,
            //     DstBinding = 0,
            //     DescriptorType = DescriptorType.CombinedImageSampler,
            //     DescriptorCount = 1,
            //     PImageInfo = &imageInfo,
            // };

            fixed (DescriptorSet* ptr = &materialDescriptorSet)
                BrowserWindow.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, 1, 1, ptr, 0, null);

            // khrPushDescriptor.CmdPushDescriptorSet(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, 1, 1, &write);

            int index = element.ObjectIndex;
            BrowserWindow.vk.CmdPushConstants(commandBuffer, PipelineLayout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, 0, sizeof(int), &index);

            BrowserWindow.vk.CmdDrawIndexed(commandBuffer, (uint)element.ModelData.GetIndicesCount(), 1, 0, 0, 0);
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        BrowserWindow.Instance.DestroyTexture(TextureImage, TextureImageMemory, TextureImageView);
    }
}