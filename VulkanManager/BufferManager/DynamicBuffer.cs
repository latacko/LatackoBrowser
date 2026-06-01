using System;
using Silk.NET.Vulkan;

namespace VulkanManager.BufferManager;

public class DynamicBuffer<BufferData> : IDisposable where BufferData : unmanaged
{
    List<RingBuffer<BufferData>> freeBuffers = [];
    List<RingBuffer<BufferData>> fullBuffers = [];
    uint bucketSizeMultiplier;
    uint bufferSize;
    BufferUsageFlags usage;

    public DynamicBuffer(uint bufferSize, uint bucketSizeMultiplier, BufferUsageFlags usage)
    {
        this.bufferSize = bufferSize;
        this.bucketSizeMultiplier = bucketSizeMultiplier;
        this.usage = usage;
    }

    public (bool, RingBuffer<BufferData>) GetBuffer()
    {
        if (freeBuffers.Count > 0)
            return (false, freeBuffers[0]);
        return (true, new RingBuffer<BufferData>(bufferSize, bucketSizeMultiplier, usage));
    }

    public void Update(ISlotInformation<BufferData> slotInformation)
    {
        RingBuffer<BufferData> _ringBuffer = slotInformation.GetRingBuffer();
        if (_ringBuffer == null)
        {
            (bool _isNewBuffer, _ringBuffer) = GetBuffer();
            if (_isNewBuffer)
                freeBuffers.Add(_ringBuffer);
            slotInformation.SetRingBuffer(_ringBuffer);
        }

        var _hasSize = _ringBuffer.Update(slotInformation);
        // Console.WriteLine("Updating buffer: " + bucketSizeMultiplier + " " + _hasSize.ToString());
        if (_hasSize) return;
        _ringBuffer.Remove(slotInformation);

        //? Aktualny buffer nie miał miejsca na większą ilość info.
        //? Przeszukujemy aktualną liste wolnych bufferów aby sprawdzić czy tam gdzieś pasuje

        for (int i = 0; i < freeBuffers.Count; i++)
        {
            if (freeBuffers[i].Update(slotInformation))
            {
                slotInformation.SetRingBuffer(freeBuffers[i]);

                //? Buffer jest już pełny wywalamy do listy pełnych bufferów
                if (!freeBuffers[i].HasFreeSpace())
                {
                    fullBuffers.Add(freeBuffers[i]);
                    freeBuffers.RemoveAt(i);
                }
                return;
            }
        }

        //? Żaden buffer nie pasował robimy nowy
        _ringBuffer = new RingBuffer<BufferData>(bufferSize, bucketSizeMultiplier, usage);
        _ringBuffer.Update(slotInformation);
        slotInformation.SetRingBuffer(_ringBuffer);

        return;
    }

    public void Remove(ISlotInformation<BufferData> slotInformation)
    {
        RingBuffer<BufferData> ringBuffer = slotInformation.GetRingBuffer();

        if (fullBuffers.Contains(ringBuffer))
        {
            ringBuffer.Remove(slotInformation);

            if (ringBuffer.HasFreeSpace())
            {
                fullBuffers.Remove(ringBuffer);
                freeBuffers.Add(ringBuffer);
            }
        }
        else
        {
            ringBuffer.Remove(slotInformation);
        }
    }

    public void CopyToBuffer(uint currentFrame)
    {
        foreach (var item in freeBuffers)
        {
            item.CopyToBuffer(currentFrame);
        }

        foreach (var item in fullBuffers)
        {
            item.CopyToBuffer(currentFrame);
        }
    }

    public void Dispose()
    {
        foreach (var item in freeBuffers)
        {
            item.Dispose();
        }

        foreach (var item in fullBuffers)
        {
            item.Dispose();
        }
    }
}
