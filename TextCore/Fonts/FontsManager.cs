using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AtlasGeneratorCore;
using Msdfgen;
using SharpFont;
using Silk.NET.Vulkan;
using Vulkan;
using VulkanManager;
using VulkanManager.Helpers;

namespace TextCore;

public class FontsManager : IDisposable
{
    public static FontsManager? Instance;
    readonly Dictionary<string, FontManager> loadedFonts = new();
    static Library library = new();

    CommandPool transferCommandPool;
    CommandBuffer commandBuffer;
    Fence transferDoneFance;
    private int isUploadRunning = 0; // 0 = idle, 1 = running

    const string preload = "ABCDEFGHIJKLMNOPRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?:;-()[]{}'\"/\\@#";

    public DescriptorAllocatorGrowable TextDescriptorAllocatorGrowable = new();

    internal static DescriptorSetLayout textDescriptorLayout;
    internal static DescriptorSet textDescriptorSet = new();
    Sampler fontSampler;

    readonly ConcurrentDictionary<string, ConcurrentQueue<string>> fontCharactersToLoad = new();

    public FontsManager()
    {
        Instance = this;

        CreateFontSampler();
        CreateDescriptorsPool();
        RegisterDescriptor();

        CreateCommandPool();
        CreateCommandBuffer();
        CreateFence();
    }

    unsafe void CreateCommandPool()
    {
        CommandPoolCreateInfo commandPoolCI = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.TransientBit,
            QueueFamilyIndex = LogicalDevice.Indices.TransferFamily!.Value,
        };

        CreateVulkan.vk.CreateCommandPool(LogicalDevice.device, ref commandPoolCI, null, out transferCommandPool);
    }

    void CreateCommandBuffer()
    {
        CommandBufferAllocateInfo commandBufferCI = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = transferCommandPool,
            CommandBufferCount = 1
        };

        CreateVulkan.vk.AllocateCommandBuffers(LogicalDevice.device, ref commandBufferCI, out commandBuffer);
    }

    unsafe void CreateFence()
    {
        FenceCreateInfo fenceCI = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };
        CreateVulkan.vk.CreateFence(LogicalDevice.device, in fenceCI, null, out transferDoneFance);
    }


    public static void RequestFontCharacters(string name, string charset = preload)
    {
        var _newFont = Instance.fontCharactersToLoad.TryAdd(name, new());
        Instance.fontCharactersToLoad[name].Enqueue(charset);

        if (Interlocked.CompareExchange(ref Instance.isUploadRunning, 1, 0) != 0)
        {
            // Someone else already has it running — do nothing.
            return;
        }

        Task.Run(() =>
        {
            try
            {
                Instance.LoadRequestedFontCharactersThread();
            }
            finally
            {
                // Reset so a future request can start it again.
                Interlocked.Exchange(ref Instance.isUploadRunning, 0);
            }
        });
    }

    unsafe void LoadRequestedFontCharactersThread()
    {
        CreateVulkan.vk.ResetCommandPool(LogicalDevice.device, transferCommandPool, CommandPoolResetFlags.None);

        var _stagingBuffer = RecordCommandBuffer();
        if (!_stagingBuffer.HasValue)
            return;


        SubmitInfo2 submitInfo = new()
        {
            SType = StructureType.SubmitInfo2,
            WaitSemaphoreInfoCount = 0,
        };

        submitInfo.CommandBufferInfoCount = 1;
        CommandBufferSubmitInfo commandBufferSubmitInfo = new()
        {
            CommandBuffer = commandBuffer,
        };
        submitInfo.PCommandBufferInfos = &commandBufferSubmitInfo;

        if (CreateVulkan.vk.QueueSubmit2(LogicalDevice.GraphicsQueue, 1, &submitInfo, transferDoneFance) != Result.Success)
        {
            throw new Exception("Failed to submit command buffer!");
        }

        CreateVulkan.vk.WaitForFences(LogicalDevice.device, 1, in transferDoneFance, Vk.True, ulong.MaxValue);
        CreateVulkan.vk.ResetFences(LogicalDevice.device, 1, in transferDoneFance);

        BufferHelper.DestroyBuffer(_stagingBuffer.Value);

        bool _isFontToLoad = false;

        foreach (var item in loadedFonts)
        {
            item.Value.GetFontAtlas().ReportProgress();

            if (!_isFontToLoad)
            {
                _isFontToLoad = fontCharactersToLoad[item.Key].IsEmpty;
            }
        }

        if (_isFontToLoad)
            LoadRequestedFontCharactersThread();
    }

    unsafe BufferData? RecordCommandBuffer()
    {
        StringBuilder stringBuilder = new();
        uint _totalCharactersToLoad = 0;
        List<FontManager> _fonts = new();

        foreach (var (path, queue) in fontCharactersToLoad)
        {
            stringBuilder.Clear();
            while (queue.TryDequeue(out var characters))
            {
                stringBuilder.Append(characters);
            }

            string _charsToLoad = stringBuilder.ToString();

            if (_charsToLoad.Length == 0)
                continue;

            var _charactersToLoadCount = LoadFont(path, _charsToLoad);
            if (_charactersToLoadCount == 0)
                continue;

            _fonts.Add(loadedFonts[path]);

            _totalCharactersToLoad += _charactersToLoadCount;
        }

        if (_fonts.Count == 0) return null;

        #region Transfer Barriers
        ImageMemoryBarrier2* _transferBarriers = stackalloc ImageMemoryBarrier2[_fonts.Count];
        for (int i = 0; i < _fonts.Count; i++)
        {
            _transferBarriers[i] = _fonts[i].GetFontAtlas().GetTransferImageBarrier();
        }

        DependencyInfo _barrierTexInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = (uint)_fonts.Count,
            PImageMemoryBarriers = _transferBarriers
        };

        CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, in _barrierTexInfo);
        #endregion

        uint _glyphSize = FontAtlas.MSDFLevelData.GlyphSize * FontAtlas.MSDFLevelData.GlyphSize * 4;
        var (_stagingBuffer, size) = BufferHelper.CreateStagingBuffer<byte>(_glyphSize * _totalCharactersToLoad);
        uint _elementHead = 0;

        foreach (var (path, bucket) in fontCharactersToLoad)
        {
            loadedFonts[path].GetFontAtlas().RecordTransfer(commandBuffer, _stagingBuffer, ref _elementHead);
        }
        #region  ShaderOptimal Barriers
        ImageMemoryBarrier2* _shaderOptimalBarriers = stackalloc ImageMemoryBarrier2[_fonts.Count];
        for (int i = 0; i < _fonts.Count; i++)
        {
            _transferBarriers[i] = _fonts[i].GetFontAtlas().GetShaderOptimalImageBarrier();
        }

        _barrierTexInfo = new()
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = (uint)_fonts.Count,
            PImageMemoryBarriers = _shaderOptimalBarriers
        };

        CreateVulkan.vk.CmdPipelineBarrier2(commandBuffer, &_barrierTexInfo);
        #endregion
        
        CreateVulkan.vk.EndCommandBuffer(commandBuffer);

        return _stagingBuffer;
    }

    public uint LoadFont(string name, string charset = preload)
    {
        FontManager _fontManager;
        if (!loadedFonts.TryGetValue(name, out _fontManager!))
        {
            _fontManager = new(library, name, RegisterTexture, new Progress<uint>(charactersRemaining =>
            {
                Console.WriteLine("Atlas <" + name + "> has remaining " + charactersRemaining + " characters");
            }));
            loadedFonts.Add(name, _fontManager);
        }

        Stopwatch stopwatch = new();
        stopwatch.Start();
        uint _toLoadChars = _fontManager.LoadCharset(charset);
        stopwatch.Stop();
        Console.WriteLine("czciąke " + name + " załadowałem w " + stopwatch.ElapsedMilliseconds + "ms");
        return _toLoadChars;
    }

    public FontManager GetFontManager(string path) => loadedFonts[path];

    unsafe void CreateFontSampler()
    {
        SamplerCreateInfo _samplerCI = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AnisotropyEnable = false,
            MinLod = 0,
            MaxLod = 0, // = 1000.0f, allows all mip levels
            AddressModeU = SamplerAddressMode.ClampToEdge, // good for atlas
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
        };

        // Console.WriteLine("Creating sampler");
        fixed (Sampler* samplerPtr = &fontSampler)
            CreateVulkan.vk.CreateSampler(LogicalDevice.device, &_samplerCI, null, samplerPtr);
    }

    #region Descriptor

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
            Sampler = fontSampler,
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

        // Console.WriteLine("Registering texture at slot: " + slot);
        CreateVulkan.vk.UpdateDescriptorSets(LogicalDevice.device, 1, &_write, 0, null);
    }

    #endregion

    public unsafe void Dispose()
    {
        TextDescriptorAllocatorGrowable.DestroyPools();
        CreateVulkan.vk.DestroyDescriptorSetLayout(LogicalDevice.device, textDescriptorLayout, null);

        library.Dispose();
        foreach (var item in loadedFonts)
        {
            item.Value.Dispose();
        }
    }
}