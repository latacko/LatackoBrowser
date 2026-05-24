using GraphicCore;
using Silk.NET.Vulkan;
using Units;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace TextCore;

public class TextBuffers : BufferManager
{
    BufferInfo<Vertex>[] vertexBuffer = new BufferInfo<Vertex>[Vulkan.VulkanManager.MAX_FRAMES_IN_FLIGHT];
    BufferInfo<uint>[] indicesBuffer = new BufferInfo<uint>[Vulkan.VulkanManager.MAX_FRAMES_IN_FLIGHT];

    Dictionary<BucketSize, Queue<Slot>> freePools = new();
    uint vertexHead = 0;
    uint indexHead = 0;

    List<TextObject> activeTexts = new();

    public override void RegisterBuffer()
    {
        for (int i = 0; i < Vulkan.VulkanManager.MAX_FRAMES_IN_FLIGHT; i++)
        {
            vertexBuffer[i] = new(256);

            indicesBuffer[i] = new(256);
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

    public void Add(TextObject text)
    {
        text.Slot = Allocate(PickBucket(text.characters));
        for (int i = 0; i < Vulkan.VulkanManager.MAX_FRAMES_IN_FLIGHT; i++)
        {
            text.Dirty[i] = true;
        }
        activeTexts.Add(text);
    }

    public void Remove(TextObject text)
    {
        Free(text.Slot);
        activeTexts.Remove(text);
    }

    public void Update(TextObject text)
    {
        BucketSize newBucket = PickBucket(text.characters);

        if (newBucket != text.Slot.Bucket) // outgrew bucket — reallocate
        {
            Free(text.Slot);
            text.Slot = Allocate(newBucket);
        }

        for (int i = 0; i < Vulkan.VulkanManager.MAX_FRAMES_IN_FLIGHT; i++)
        {
            text.Dirty[i] = true;
        }
    }

    public unsafe void CopyToBuffer(uint currentFrame)
    {
        foreach (var text in activeTexts)
        {
            if (!text.Dirty[currentFrame]) continue;

            text.Vertices.CopyTo(
                new Span<Vertex>(((Vertex*)vertexBuffer[currentFrame].Mapped)+text.Slot.VertexOffset, (int)VertexsPerBucket(text.Slot.Bucket))
            );

            text.Indices.CopyTo(
                new Span<uint>((uint*)indicesBuffer[currentFrame].Mapped+text.Slot.IndexOffset, (int)IndicesPerBucket(text.Slot.Bucket))
            );

            text.Dirty[currentFrame] = false;
        }
    }

    public override void Dispose()
    {
        for (int i = 0; i < Vulkan.VulkanManager.MAX_FRAMES_IN_FLIGHT; i++)
        {
            vertexBuffer[i].Dispose();

            indicesBuffer[i].Dispose();
        }
    }
}
