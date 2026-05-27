using GraphicCore;
using Silk.NET.Vulkan;
using Units;
using Vulkan;
using VulkanManager.Helpers;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace TextCore;

public class TextManager : BufferManager
{
    internal static TextManager Instance;
    internal BufferInfo<Vertex>[] vertexBuffer = new BufferInfo<Vertex>[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    internal BufferInfo<ushort>[] indicesBuffer = new BufferInfo<ushort>[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    internal BufferInfo<TextData>[] dataBuffer = new BufferInfo<TextData>[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    Dictionary<BucketSize, Queue<Slot>> freePools = new();
    uint vertexHead = 0;
    uint indexHead = 0;

    public DescriptorAllocatorGrowable TextDescriptorAllocatorGrowable = new();


    List<RuntimeText> activeTexts = new();

    internal DescriptorSetLayout textDescriptorLayout;
    internal DescriptorSet textDescriptorSet = new();

    public TextManager()
    {
        Instance = this;
    }

    public override void RegisterBuffer()
    {
        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            vertexBuffer[i] = new(256, BufferUsageFlags.VertexBufferBit);

            indicesBuffer[i] = new(256, BufferUsageFlags.IndexBufferBit);

            dataBuffer[i] = new(256, 0);
        }
        CreateDescriptorsPool();
        RegisterDescriptor();
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

        Console.WriteLine("Registering texture at slot: " + slot);
        CreateVulkan.vk.UpdateDescriptorSets(LogicalDevice.device, 1, &_write, 0, null);
    }

    static uint VertexsPerBucket(BucketSize b) => (uint)b * 4;
    static uint IndicesPerBucket(BucketSize b) => (uint)b * 6;

    static BucketSize PickBucket(uint glyphCount) => glyphCount switch
    {
        <= 16 => BucketSize.Tiny,
        <= 32 => BucketSize.Small,
        <= 64 => BucketSize.Medium,
        <= 128 => BucketSize.Large,
        _ => BucketSize.Huge
    };

    Slot Allocate(BucketSize bucket)
    {
        if (freePools.TryGetValue(bucket, out var pool) && pool.TryDequeue(out var slot))
            return slot;

        var newSlot = new Slot(vertexHead, indexHead, bucket);
        vertexHead += VertexsPerBucket(bucket);
        indexHead += IndicesPerBucket(bucket);

        return newSlot;
    }

    void Free(Slot slot)
    {
        if (freePools.TryGetValue(slot.Bucket, out var bucket))
            bucket.Enqueue(slot);
        else
        {
            var _queue = new Queue<Slot>();
            _queue.Enqueue(slot);
            freePools.Add(slot.Bucket, _queue);
        }
    }

    public void Add(RuntimeText text)
    {
        text.Slot = Allocate(PickBucket((uint)text.Text.Length));
        text.ModelData.vertexOffset = text.Slot.VertexOffset;
        text.ModelData.indexOffset = text.Slot.IndexOffset;
        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            text.AddFlag(RuntimeModelData.DirtyFlags.Matrix);
        }
        activeTexts.Add(text);
    }

    public void Remove(RuntimeText text)
    {
        Free(text.Slot);
        activeTexts.Remove(text);
    }

    public void Update(RuntimeText text)
    {
        BucketSize newBucket = PickBucket((uint)text.Text.Length*2);

        if (newBucket != text.Slot.Bucket) // outgrew bucket — reallocate
        {

            if (text.Slot != default)
                Free(text.Slot);
            Console.WriteLine("Update data");
            text.Slot = Allocate(newBucket);

            text.ModelData.vertexOffset = text.Slot.VertexOffset;
            text.ModelData.indexOffset = text.Slot.IndexOffset;

            if (!activeTexts.Contains(text))
                activeTexts.Add(text);
        }

        text.AddFlag(RuntimeModelData.DirtyFlags.Model);
    }

    public unsafe void Update(uint currentFrame, uint objectIndex, TextData objectData)
    {
        ((TextData*)dataBuffer[currentFrame].Mapped)[objectIndex] = objectData;
    }

    public unsafe void CopyToBuffer(uint currentFrame)
    {
        foreach (var text in activeTexts)
        {
            if (!text.dirty[currentFrame].HasFlag(RuntimeModelData.DirtyFlags.Model)) continue;

            Console.WriteLine("Found text that has changed: " + text.Text + " offset: " + text.Slot.VertexOffset + " bucket: " + VertexsPerBucket(text.Slot.Bucket) + " has vertexs: " + text.ModelData.Vertices.Length);

            text.ModelData.Vertices.CopyTo(
                new Span<Vertex>(((Vertex*)vertexBuffer[currentFrame].Mapped) + text.Slot.VertexOffset, (int)VertexsPerBucket(text.Slot.Bucket))
            );

            text.ModelData.Indices.CopyTo(
                new Span<ushort>((ushort*)indicesBuffer[currentFrame].Mapped + text.Slot.IndexOffset, (int)IndicesPerBucket(text.Slot.Bucket))
            );

            text.RemoveFlag(RuntimeModelData.DirtyFlags.Model, currentFrame);
        }
    }

    public override void Dispose()
    {
        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            vertexBuffer[i].Dispose();

            indicesBuffer[i].Dispose();
        }
    }
}
