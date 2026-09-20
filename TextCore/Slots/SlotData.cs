using System;
using VulkanManager.BufferManager;

namespace TextCore.Slots;

public class SlotData<BufferData> : ISlotInformation<BufferData> where BufferData : unmanaged
{
    bool[] isDirty;
    RingBuffer<BufferData> ringBuffer;
    VulkanManager.BufferManager.Slot slot;
    internal int dataCount; 

    internal BufferData[] _data;
    public BufferData[] Data
    {
        get => _data;
        set
        {
            SetDirty();
            _data = value;
        }
    }

    public SlotData()
    {
        isDirty = new bool[Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT];
        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            isDirty[i] = false;
        }
    }


    public uint GetDataCount()=>(uint)dataCount;

    public BufferData[] GetDatas()=>Data;


    public Slot GetSlot() => slot;
    public void UpdateSlot(Slot newSlot)
    {
        slot = newSlot;
    }

    public RingBuffer<BufferData> GetRingBuffer() => ringBuffer;
    public void SetRingBuffer(RingBuffer<BufferData> ringBuffer)
    {
        this.ringBuffer = ringBuffer;
    }



    public bool IsDirty(uint frameInFlight) => isDirty[frameInFlight];

    public void RemoveDirty(uint frameInFlight)
    {
        isDirty[frameInFlight] = false;
    }

    public void SetDirty()
    {
        for (int i = 0; i < Vulkan.VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            isDirty[i] = true;
        }
    }
}
