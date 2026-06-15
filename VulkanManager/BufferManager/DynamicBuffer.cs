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
        {
            // Console.WriteLine($"Returning free buffer {bucketSizeMultiplier} from list. Buffer: frame 0: {freeBuffers[0].buffersInfo[0].Buffer.Handle} frame 1: {freeBuffers[0].buffersInfo[1].Buffer.Handle}");
            return (false, freeBuffers[0]);
        }
        return (true, new RingBuffer<BufferData>(bufferSize, bucketSizeMultiplier, usage));
    }

    public void Update(ISlotInformation<BufferData> slotInformation)
    {
        RingBuffer<BufferData> _ringBuffer = slotInformation.GetRingBuffer();
        if (_ringBuffer == null)
        {
            (bool _isNewBuffer, _ringBuffer) = GetBuffer();
            if (_isNewBuffer)
            {
                freeBuffers.Add(_ringBuffer);
                // Console.WriteLine($"Adding to free buffers {bucketSizeMultiplier}. Buffer: frame 0: {_ringBuffer.buffersInfo[0].Buffer.Handle} frame 1: {_ringBuffer.buffersInfo[1].Buffer.Handle}");
            }
            slotInformation.SetRingBuffer(_ringBuffer);
        }

        var _hasSize = _ringBuffer.Update(slotInformation);
        // Console.WriteLine("Updating buffer: " + bucketSizeMultiplier + " " + _hasSize.ToString());
        if (_hasSize) return;
        // Console.WriteLine("Nie ma miejsca szukam nowego");
        _ringBuffer.Remove(slotInformation);

        slotInformation.UpdateSlot(default);

        //? Aktualny buffer nie miał miejsca na większą ilość info.
        //? Przeszukujemy aktualną liste wolnych bufferów aby sprawdzić czy tam gdzieś pasuje

        for (int i = freeBuffers.Count - 1; i >= 0; i--)
        {
            // Console.WriteLine("Sprawdzam czy się mieszczę w " + freeBuffers[i].buffersInfo[0].Buffer.Handle);
            if (freeBuffers[i].Update(slotInformation))
            {
                // Console.WriteLine("Zmieściłem się");
                slotInformation.SetRingBuffer(freeBuffers[i]);

                //? Buffer jest już pełny wywalamy do listy pełnych bufferów
                if (!freeBuffers[i].HasFreeSpace())
                {
                    // Console.WriteLine("Nie ma już wgl miejsca w tym bufferze. Wywalam do pełnych");
                    fullBuffers.Add(freeBuffers[i]);
                    freeBuffers.RemoveAt(i);
                }
                return;
            }
        }

        // Console.WriteLine("Żaden buffer nie pasował więc robimy nowy");

        //? Żaden buffer nie pasował robimy nowy
        _ringBuffer = new RingBuffer<BufferData>(bufferSize, bucketSizeMultiplier, usage);
        _ringBuffer.Update(slotInformation);
        freeBuffers.Add(_ringBuffer);
        // Console.WriteLine("Adding to free buffers: ring buffer with handle of: " + _ringBuffer.buffersInfo[0].Buffer.Handle);
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

    int lastFreeCount = 0;
    int lastFullCount = 0;
    public void CopyToBuffer(uint currentFrame)
    {
        if (lastFreeCount != freeBuffers.Count)
        {
            // Console.WriteLine("Free buffers count: " + freeBuffers.Count);
            lastFreeCount = freeBuffers.Count;
        }
        foreach (var item in freeBuffers)
        {
            item.CopyToBuffer(currentFrame);
        }

        if (lastFullCount != fullBuffers.Count)
        {
            // Console.WriteLine("Full buffers count: " + fullBuffers.Count);
            lastFullCount = fullBuffers.Count;
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
