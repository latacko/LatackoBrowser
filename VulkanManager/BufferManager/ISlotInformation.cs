using System;
using System.Xml.Serialization;

namespace VulkanManager.BufferManager;

public interface ISlotInformation<BufferData> where BufferData : unmanaged
{
    public void UpdateSlot(Slot newSlot);
    public Slot GetSlot();

    public uint GetDataCount();

    public BufferData[] GetDatas();

    public bool IsDirty(uint frameInFlight);
    public void SetDirty();
    public void RemoveDirty(uint frameInFlight);

    public RingBuffer<BufferData> GetRingBuffer();
    public void SetRingBuffer(RingBuffer<BufferData> ringBuffer);
}
