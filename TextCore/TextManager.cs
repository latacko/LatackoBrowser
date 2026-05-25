using GraphicCore;
using Silk.NET.Vulkan;
using Units;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace TextCore;

public class TextManager : BufferManager
{
    internal static TextManager Instance;
    internal BufferInfo<Vertex>[] vertexBuffer = new BufferInfo<Vertex>[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    internal BufferInfo<uint>[] indicesBuffer = new BufferInfo<uint>[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    internal BufferInfo<TextData>[] dataBuffer = new BufferInfo<TextData>[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];

    Dictionary<BucketSize, Queue<Slot>> freePools = new();
    uint vertexHead = 0;
    uint indexHead = 0;

    List<RuntimeText> activeTexts = new();

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
        text.Slot = Allocate(PickBucket(text.characters));
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
        BucketSize newBucket = PickBucket(text.characters);

        if (newBucket != text.Slot.Bucket) // outgrew bucket — reallocate
        {
            Free(text.Slot);
            text.Slot = Allocate(newBucket);

            text.ModelData.vertexOffset = text.Slot.VertexOffset;
            text.ModelData.indexOffset = text.Slot.IndexOffset;
        }

        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            text.AddFlag(RuntimeModelData.DirtyFlags.Model);
        }
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
