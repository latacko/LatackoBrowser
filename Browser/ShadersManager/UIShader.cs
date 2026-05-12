using Browser;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

public unsafe class UIShader : BaseShader
{
    public Image TextureImage;
    public DeviceMemory TextureImageMemory;
    public ImageView TextureImageView;

    protected override string VertexShaderPath => "shaders/vert.spv";
    protected override string FragmentShaderPath => "shaders/frag.spv";

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

    private protected override void CreateDescriptorSetsForMaterial()
    {
        fixed (DescriptorSetLayout* layoutPtr = &descriptorSetLayoutForMaterial)
        {

            DescriptorSetAllocateInfo _allocInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,

                DescriptorPool = GlobalDescriptorPool.Pool,
                DescriptorSetCount = 1,
                PSetLayouts = layoutPtr,
            };

            fixed (DescriptorSet* setsPtr = &materialDescriptorSet)
            {
                if (BrowserWindow.vk.AllocateDescriptorSets(BrowserWindow.device, &_allocInfo, setsPtr) != Result.Success)
                {
                    throw new Exception("Failed to allocate descriptor sets!");
                }
            }

            DescriptorImageInfo _imageInfo = new()
            {
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = TextureImageView,
                Sampler = BrowserWindow.textureSampler,
            };

            WriteDescriptorSet[] _descriptorWrites = [
                new (){
                SType = StructureType.WriteDescriptorSet,

                DstSet = materialDescriptorSet,
                DstBinding = 0,
                DstArrayElement = 0,

                DescriptorType = DescriptorType.CombinedImageSampler,
                DescriptorCount = 1,

                PImageInfo = &_imageInfo,
            },
        ];

            fixed (WriteDescriptorSet* descriptorWritesPtr = _descriptorWrites)
                BrowserWindow.vk.UpdateDescriptorSets(BrowserWindow.device, (uint)_descriptorWrites.Length, descriptorWritesPtr, 0, null);
        }
    }

    private protected override void CreateDescriptorSetForObject()
    {
        fixed (DescriptorSetLayout* layoutPtr = &descriptorSetLayoutForObject)
        {
            DescriptorSetAllocateInfo allocInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,

                DescriptorPool = GlobalDescriptorPool.Pool,
                DescriptorSetCount = 1,
                PSetLayouts = layoutPtr,
            };

            if (BrowserWindow.vk.AllocateDescriptorSets(BrowserWindow.device, &allocInfo, out objectDescriptorSet) != Result.Success)
            {
                throw new Exception("Failed to allocate object descriptor set!");
            }

            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = objectBuffer,
                Offset = 0,
                Range = GetSizeOfObjectDatas()
            };

            WriteDescriptorSet write = new()
            {
                SType = StructureType.WriteDescriptorSet,

                DstSet = objectDescriptorSet,
                DstBinding = 0,

                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,

                PBufferInfo = &bufferInfo,
            };

            BrowserWindow.vk.UpdateDescriptorSets(BrowserWindow.device, 1, &write, 0, null);
        }
    }

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