using System;
using System.Runtime.CompilerServices;
using InstanceFinderCore;
using Silk.NET.Vulkan;
using Vulkan;

namespace VulkanManager.BufferManager;

public class RingBuffer<BufferData> : IDisposable, IRenderTick where BufferData : unmanaged
{
    internal BufferInfo<BufferData>[] buffersInfo = new BufferInfo<BufferData>[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    Dictionary<BucketSize, Queue<Slot>> freePools = new();
    readonly List<ISlotInformation<BufferData>> slots = [];
    byte bucketSizeMultiplier;
    uint elementOffset = 0;
    uint maxElementsCount = 0;
    byte threadId = 0;

    public RingBuffer(byte threadId, uint maxElementsCount, byte bucketSizeMultiplier, BufferUsageFlags usage)
    {
        this.maxElementsCount = maxElementsCount;
        this.bucketSizeMultiplier = bucketSizeMultiplier;
        this.threadId = threadId;
        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            buffersInfo[i] = new(maxElementsCount, usage);
        }
    }

    BucketSize PickBucket(uint elementsCount)
    {
        if (elementsCount <= (int)BucketSize.Tiny * bucketSizeMultiplier)
            return BucketSize.Tiny;
        else if (elementsCount <= (int)BucketSize.Small * bucketSizeMultiplier)
            return BucketSize.Small;
        else if (elementsCount <= (int)BucketSize.Medium * bucketSizeMultiplier)
            return BucketSize.Medium;
        else if (elementsCount <= (int)BucketSize.Large * bucketSizeMultiplier)
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

    uint GetBucketSize(BucketSize bucket)
    {
        return (uint)bucket * bucketSizeMultiplier;
    }

    uint GetElementBucketSize(BucketSize bucket)
    {
        return (uint)Unsafe.SizeOf<BufferData>() * GetBucketSize(bucket);
    }

    public bool HasFreeSpace(BucketSize bucket)
    {
        if (freePools.TryGetValue(bucket, out var pool) && pool.Count > 0)
            return true;
        return elementOffset + GetElementBucketSize(bucket) <= maxElementsCount;
    }

    Slot GetNewSlot(BucketSize bucket)
    {
        if (freePools.TryGetValue(bucket, out var pool) && pool.TryDequeue(out var slot))
            return slot;

        var newSlot = new Slot(elementOffset, bucket, new bool[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT]);
        elementOffset += GetElementBucketSize(bucket);

        return newSlot;
    }

    void Free(Slot slot)
    {
        // Console.WriteLine("Usuwam slot: " + slot.Bucket);
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
        slots.Remove(slotInformation);
        Free(slotInformation.GetSlot());
    }

    uint elementsCountToUpload = 0;
    public bool Update(ISlotInformation<BufferData> slotInformation)
    {
        BucketSize newBucket = PickBucket(slotInformation.GetDataCount());
        elementsCountToUpload += slotInformation.GetDataCount();
        // Console.WriteLine("Element count: " + slotInformation.GetDataCount() + " a wybieram bucket: " + ((int)newBucket*bucketSizeMultiplier));
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
            _slot = GetNewSlot(newBucket);
            slotInformation.UpdateSlot(_slot);

            if (!slots.Contains(slotInformation))
                slots.Add(slotInformation);
        }
        // Console.WriteLine("Set is dirty");
        slotInformation.SetDirty();
        return true;
    }

    public unsafe void RenderTick(uint frameInFlight)
    {
        var (_bufferData, size) = BufferHelper.CreateStagingBuffer<BufferData>(elementsCountToUpload);
        uint _elementOffset = 0;
        uint i = 0;
        BufferCopy2[] _bc = new BufferCopy2[elementsCountToUpload];
        foreach (var slot in slots)
        {
            if (!slot.IsDirty(frameInFlight)) continue;
            // Console.WriteLine("Copy to buffer with multiplier of: " + bucketSizeMultiplier + " bucket size: " + ((int)slot.GetSlot().Bucket*bucketSizeMultiplier) + " vertexes: " + slot.GetDataCount() + " offset: " + slot.GetSlot().Offset + " handle: " + buffersInfo[0].Buffer.Handle);
            ReadOnlySpan<BufferData> sourceSpan = slot.GetDatas().AsSpan(0, (int)slot.GetDataCount());

            sourceSpan.CopyTo(
                new Span<BufferData>(((BufferData*)_bufferData.Mapped) + _elementOffset, (int)GetElementBucketSize(slot.GetSlot().Bucket))
            );


            _bc[i] = new()
            {
                SrcOffset = _elementOffset,
                DstOffset = slot.GetSlot().Offset,
                Size = GetElementBucketSize(slot.GetSlot().Bucket),
            };

            _elementOffset += (uint)Unsafe.SizeOf<BufferData>() * slot.GetDataCount();
            slot.RemoveDirty(frameInFlight);
            i++;
        }

        InstanceFinder.GetInstance<RingBufferManager>(threadId).AddTransfer(frameInFlight, _bufferData.Buffer, buffersInfo[frameInFlight].Buffer, _bc);
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

/// <summary>
/// Show how many elements can fit insize a bucket
/// </summary>
public enum BucketSize : uint
{
    Tiny = 16,
    Small = 32,
    Medium = 64,
    Large = 128,
    Huge = 256,
}
