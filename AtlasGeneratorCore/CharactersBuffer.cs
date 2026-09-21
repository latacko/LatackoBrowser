using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MsdfAtlasGen;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Vulkan;
using VulkanManager;

namespace AtlasGeneratorCore;

public class CharactersBuffer : IDisposable, IRenderTick
{
    public const int NODES_COUNT_PER_INCREASE = 100;
    uint incresedTimes = 1;
    uint lastInstanceId = 0;
    readonly BufferData[] buffers = new BufferData[VulkanEngine.MAX_FRAMES_IN_FLIGHT];
    readonly int gpuDataSize = Unsafe.SizeOf<CharacterDataGPU>();
    readonly Queue<GlyphGeometry>[] charactersToAdd = new Queue<GlyphGeometry>[VulkanEngine.MAX_FRAMES_IN_FLIGHT];


    public CharactersBuffer()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            buffers[i] = CreateBuffer();
            charactersToAdd[i] = new();
        }
    }

    #region Buffer functions
    unsafe BufferData CreateBuffer()
    {
        BufferData _bufferData = new();
        ulong _bufferSize = (ulong)gpuDataSize * NODES_COUNT_PER_INCREASE * incresedTimes;
        BufferHelper.CreateBuffer(_bufferSize, BufferUsageFlags.ShaderDeviceAddressBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref _bufferData.Buffer, ref _bufferData.Memory);
        void* data;
        CreateVulkan.vk.MapMemory(LogicalDevice.device, _bufferData.Memory, 0, _bufferSize, 0, &data);
        _bufferData.Mapped = data;

        BufferDeviceAddressInfo addrInfo = new()
        {
            SType = StructureType.BufferDeviceAddressInfo,
            Buffer = _bufferData.Buffer
        };

        _bufferData.DeviceAddress = CreateVulkan.vk.GetBufferDeviceAddress(LogicalDevice.device, ref addrInfo);
        return _bufferData;
    }

    public BufferData GetBuffer(uint frameInFlight)
    {
        return buffers[frameInFlight];
    }

    unsafe void IncreseBuffer(int frameInFlight, uint newIncresedTimes)
    {
        ulong _bufferSize = (ulong)gpuDataSize * NODES_COUNT_PER_INCREASE * incresedTimes;
        incresedTimes = newIncresedTimes;
        BufferData _newBuffer = CreateBuffer();
        ulong _newBufferSize = (ulong)gpuDataSize * NODES_COUNT_PER_INCREASE * incresedTimes;

        new Span<byte>(buffers[frameInFlight].Mapped, (int)_bufferSize).CopyTo(new Span<byte>(_newBuffer.Mapped, (int)_newBufferSize));
        buffers[frameInFlight].Dispose();
        buffers[frameInFlight] = _newBuffer;
    }

    uint GetNewSize(uint requiredSize)
    {
        uint i = 0;
        while (requiredSize > NODES_COUNT_PER_INCREASE * (incresedTimes + i))
        {
            i++;
        }
        return incresedTimes + i;
    }
    #endregion

    public void EnqueueCharacter(GlyphGeometry character)
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            charactersToAdd[i].Enqueue(character);
        }
    }

    public void RenderTick(uint frameInFlight)
    {
        if (charactersToAdd[frameInFlight].Count == 0) return;

        #region Test if it is required to create new buffer with larger size
        uint _newSize = GetNewSize(lastInstanceId + (uint)charactersToAdd[frameInFlight].Count);
        if (_newSize != incresedTimes)
        {
            IncreseBuffer((int)frameInFlight, _newSize);
        }
        #endregion

        while (charactersToAdd[frameInFlight].TryDequeue(out var element))
        {
            AddInstance(element, frameInFlight);
        }
    }

    uint GetNewInstanceId()
    {
        return lastInstanceId++;
    }

    unsafe void AddInstance(GlyphGeometry character, uint frameInFlight)
    {
        if (!character.CharacterBufferId.HasValue)
        {
            var _newInstanceId = GetNewInstanceId();
            character.SetCharacterBufferId(_newInstanceId);
        }

        ((CharacterDataGPU*)GetBuffer(frameInFlight).Mapped)[character.CharacterBufferId!.Value] = new()
        {
            UV = new Vector2D<float>(character.GetBoxRect().Y, character.GetBoxRect().Y+character.GetBoxRect().H),
            Scale = (float)character.GetBoxScale(),
            // BearingY = (baseline - glyph.BearingY) / height
        };
    }

    public void Dispose()
    {
        for (int i = 0; i < VulkanEngine.MAX_FRAMES_IN_FLIGHT; i++)
        {
            buffers[i].Dispose();
        }
    }
}
