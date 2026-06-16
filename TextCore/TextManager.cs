using GraphicCore;
using GraphicCore.Styles;
using Silk.NET.Vulkan;
using Units;
using Vulkan;
using VulkanManager.BufferManager;
using VulkanManager.Helpers;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace TextCore;

public class TextManager : BufferManager
{
    internal static TextManager Instance;
    public static TextShader TextShader = new();
    internal DynamicBuffer<TextVertex> vertexBuffer;
    internal DynamicBuffer<ushort> indicesBuffer;

    internal BufferInfo<TextContainerData>[] textContainerDataBuffer = new BufferInfo<TextContainerData>[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    internal BufferInfo<ModelData>[] textModelDataBuffer = new BufferInfo<ModelData>[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    Dictionary<BucketSize, Queue<Slot>> freePools = new();
    uint vertexHead = 0;
    uint indexHead = 0;

    public DescriptorAllocatorGrowable TextDescriptorAllocatorGrowable = new();


    List<RuntimeText> activeTexts = new();
    public static uint LastCreatedIndex = 0;

    internal DescriptorSetLayout textDescriptorLayout;
    internal DescriptorSet textDescriptorSet = new();

    internal static Style TextDefaultStyle;

    public TextManager()
    {
        Instance = this;
    }

    internal static RuntimeText AddModelText(ReadOnlyMemory<char> text, int leftRange, int rightRange, RuntimeTextContainer parent)
    {
        uint objectIndex = LastCreatedIndex++;
        RuntimeText runtimeModelData = new(text, leftRange, rightRange, objectIndex, parent);
        if (parent != null)
        {
            parent.AddChild(runtimeModelData);
        }

        return runtimeModelData;
    }

    public static RuntimeTextContainer AddText(string text, RuntimeModelData parent)
    {
        uint objectIndex = LastCreatedIndex++;
        RuntimeTextContainer runtimeTextContainer = new(text, objectIndex, parent);
        if (parent != null)
            parent.AddChild(runtimeTextContainer);

        TextShader.elements.Add(runtimeTextContainer);

        Console.WriteLine("Adding text to the textShader." + TextShader.GetHashCode());

        return runtimeTextContainer;
    }

    public override void RegisterBuffer()
    {
        vertexBuffer = new(10000, 2, BufferUsageFlags.VertexBufferBit);
        indicesBuffer = new(5000, 2, BufferUsageFlags.IndexBufferBit);
        
        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {

            textModelDataBuffer[i] = new(256, 0);
            textContainerDataBuffer[i] = new(256, 0);
        }
        CreateDescriptorsPool();
        RegisterDescriptor();

        TextShader.Init();

        TextDefaultStyle = new Style()
            .SetFontProperties((FontProperties)=>FontProperties
                .SetFont("google-noto/NotoSerif-Regular.ttf")
                .SetFontSize(new(16))
            );
    }

    public unsafe void RegisterDescriptor()
    {
        DescriptorBindingFlags[] _descVariableFlag = [0, DescriptorBindingFlags.VariableDescriptorCountBit | DescriptorBindingFlags.PartiallyBoundBit];
        fixed (DescriptorBindingFlags* _descVariableFlagPtr = _descVariableFlag)
        {
            DescriptorSetLayoutBindingFlagsCreateInfo _descBindingFlags = new()
            {
                SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
                BindingCount = (uint)_descVariableFlag.Length,
                PBindingFlags = _descVariableFlagPtr
            };

            DescriptorLayoutBuilder builder = new();
            builder.AddBinding(new()
            {
                Binding = 0,
                DescriptorType = DescriptorType.Sampler,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.FragmentBit,
            });
            builder.AddBinding(new()
            {
                Binding = 1,
                DescriptorType = DescriptorType.SampledImage,
                DescriptorCount = 256,
                StageFlags = ShaderStageFlags.FragmentBit,
            });

            textDescriptorLayout = builder.Build((nint)(&_descBindingFlags), DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit);

        }

        uint _variableDescCount = 256;
        DescriptorSetVariableDescriptorCountAllocateInfo _variableDescCountAI = new()
        {
            SType = StructureType.DescriptorSetVariableDescriptorCountAllocateInfoExt,
            DescriptorSetCount = 1,
            PDescriptorCounts = &_variableDescCount
        };
        textDescriptorSet = TextDescriptorAllocatorGrowable.Allocate(textDescriptorLayout, (nint)(&_variableDescCountAI));

        DescriptorImageInfo _samplerInfo = new()
        {
            Sampler = VulkanEngine.Instance.sampler,
        };

        WriteDescriptorSet _descriptorWrites = new()
        {
            SType = StructureType.WriteDescriptorSet,

            DstSet = textDescriptorSet,
            DstBinding = 0,
            DstArrayElement = 0,

            DescriptorType = DescriptorType.Sampler,
            DescriptorCount = 1,

            PImageInfo = &_samplerInfo,
        };

        CreateVulkan.vk.UpdateDescriptorSets(LogicalDevice.device, (uint)1, &_descriptorWrites, 0, null);
    }

    void CreateDescriptorsPool()
    {
        DescriptorAllocatorGrowable.PoolSizeRatio[] _sizes = [
            new(){
                Type = DescriptorType.Sampler,
                Ratio = 1,
            },
            new(){
                Type = DescriptorType.SampledImage,
                Ratio = 256,
            }
        ];

        TextDescriptorAllocatorGrowable.Init(1, _sizes);
    }

    public unsafe void RegisterTexture(ImageView imageView, uint slot)
    {
        DescriptorImageInfo _imageInfo = new()
        {
            ImageView = imageView,
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
        };

        WriteDescriptorSet _write = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = textDescriptorSet,
            DstBinding = 1,
            DstArrayElement = slot,
            DescriptorType = DescriptorType.SampledImage,
            DescriptorCount = 1,
            PImageInfo = &_imageInfo,
        };

        // Console.WriteLine("Registering texture at slot: " + slot);
        CreateVulkan.vk.UpdateDescriptorSets(LogicalDevice.device, 1, &_write, 0, null);
    }

    public void Update(RuntimeText text)
    {
        // Console.WriteLine("Vertex: ");
        vertexBuffer.Update(text.VertexSlotData);
        // Console.WriteLine("Indices: ");
        indicesBuffer.Update(text.IndicesSlotData);
    }

    public unsafe void Update(uint currentFrame, uint objectIndex, ModelData objectData)
    {
        ((ModelData*)textModelDataBuffer[currentFrame].Mapped)[objectIndex] = objectData;
    }

    public unsafe void Update(uint currentFrame, uint objectIndex, TextContainerData textContainerData)
    {
        ((TextContainerData*)textContainerDataBuffer[currentFrame].Mapped)[objectIndex] = textContainerData;
    }

    public unsafe void CopyToBuffer(uint currentFrame)
    {
        // Console.WriteLine("Vertex:");
        vertexBuffer.CopyToBuffer(currentFrame);
        // Console.WriteLine("Indices:");
        indicesBuffer.CopyToBuffer(currentFrame);
        // foreach (var text in activeTexts)
        // {
        //     if (!text.dirty[currentFrame].HasFlag(RuntimeModelData.DirtyFlags.Model)) continue;

        //     text.ModelData.Vertices.CopyTo(
        //         new Span<TextVertex>(((TextVertex*)vertexBuffer[currentFrame].Mapped) + text.Slot.VertexOffset, (int)VertexsPerBucket(text.Slot.Bucket))
        //     );

        //     text.ModelData.Indices.CopyTo(
        //         new Span<ushort>((ushort*)indicesBuffer[currentFrame].Mapped + text.Slot.IndexOffset, (int)IndicesPerBucket(text.Slot.Bucket))
        //     );

        //     text.RemoveFlag(RuntimeModelData.DirtyFlags.Model, currentFrame);
        // }
    }

    public override unsafe void Dispose()
    {
        TextShader.Dispose();
        TextDescriptorAllocatorGrowable.DestroyPools();
        CreateVulkan.vk.DestroyDescriptorSetLayout(LogicalDevice.device, textDescriptorLayout, null);

        vertexBuffer.Dispose();
        indicesBuffer.Dispose();

        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {

            textModelDataBuffer[i].Dispose();
            textContainerDataBuffer[i].Dispose();
        }
    }
}
