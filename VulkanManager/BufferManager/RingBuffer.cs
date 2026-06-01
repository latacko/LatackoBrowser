using System;
using Silk.NET.Vulkan;

namespace VulkanManager.BufferManager;

public class RingBuffer<BufferData> : IDisposable where BufferData : unmanaged
{
    internal BufferInfo<BufferData>[] buffersInfo = new BufferInfo<BufferData>[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    Dictionary<BucketSize, Queue<Slot>> freePools = new();
    readonly List<ISlotInformation<BufferData>> slots = [];
    uint bucketSizeMultiplier;
    uint offsetHead = 0;
    uint bufferSize = 0;

    public RingBuffer(uint bufferSize, uint bucketSizeMultiplier, BufferUsageFlags usage)
    {
        this.bufferSize = bufferSize;
        this.bucketSizeMultiplier = bucketSizeMultiplier;
        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            buffersInfo[i] = new(bufferSize, usage);
        }
    }

    BucketSize PickBucket(uint quadCount)
    {
        if (quadCount <= 16 * bucketSizeMultiplier)
            return BucketSize.Tiny;
        else if (quadCount <= 32 * bucketSizeMultiplier)
            return BucketSize.Small;
        else if (quadCount <= 64 * bucketSizeMultiplier)
            return BucketSize.Medium;
        else if (quadCount <= 128 * bucketSizeMultiplier)
            return BucketSize.Large;
        else
            return BucketSize.Huge;
    }

    public bool HasFreeSpace()
    {
        foreach (var item in Enum.GetValues<BucketSize>())
        {
            if (HasFreeSpace(item))
            {
                return true;
            }
        }
        return false;
    }

    public bool HasFreeSpace(BucketSize bucket)
    {
        if (freePools.TryGetValue(bucket, out var pool) && pool.Count > 0)
            return true;
        return offsetHead + (uint)bucket <= bufferSize;
    }

    Slot Allocate(BucketSize bucket)
    {
        if (freePools.TryGetValue(bucket, out var pool) && pool.TryDequeue(out var slot))
            return slot;

        var newSlot = new Slot(offsetHead, bucket, new bool[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT]);
        offsetHead += (uint)bucket*bucketSizeMultiplier;

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

    public void Remove(ISlotInformation<BufferData> slotInformation)
    {
        Free(slotInformation.GetSlot());
    }

    public bool Update(ISlotInformation<BufferData> slotInformation)
    {
        BucketSize newBucket = PickBucket(slotInformation.GetDataCount());
        Slot _slot = slotInformation.GetSlot();
        if (newBucket != _slot.Bucket) // outgrew bucket — reallocate
        {

            if (_slot != default)
                Free(_slot);

            if (!HasFreeSpace(newBucket))
            {
                // Console.WriteLine("No space for buclet: " + newBucket + " " + bucketSizeMultiplier);
                return false;
            }
            // Console.WriteLine("Finaly found space" + " " + bucketSizeMultiplier);
            _slot = Allocate(newBucket);
            slotInformation.UpdateSlot(_slot);

            if (!slots.Contains(slotInformation))
                slots.Add(slotInformation);
        }
        slotInformation.SetDirty();
        return true;
    }

    public unsafe void CopyToBuffer(uint currentFrame)
    {
        foreach (var slot in slots)
        {
            if (!slot.IsDirty(currentFrame)) continue;
            Console.WriteLine("Copy to buffer with multiplier of: " + bucketSizeMultiplier + " bucket size: " + ((int)slot.GetSlot().Bucket) + " vertexes: " + slot.GetDataCount() + " offset: " + slot.GetSlot().Offset + " vertexs: ");
            slot.GetDatas().CopyTo(
                new Span<BufferData>(((BufferData*)buffersInfo[currentFrame].Mapped) + slot.GetSlot().Offset, (int)slot.GetSlot().Bucket * (int)bucketSizeMultiplier)
            );

            slot.RemoveDirty(currentFrame);
        }
    }


    public void Dispose()
    {
        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            buffersInfo[i].Dispose();
        }
    }
}

public record struct Slot(
    uint Offset,
    BucketSize Bucket,
    bool[] dirty
);

public enum BucketSize : uint
{
    Tiny = 16,
    Small = 32,
    Medium = 64,
    Large = 128,
    Huge = 256,
}
