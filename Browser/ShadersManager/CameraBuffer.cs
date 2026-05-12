using System.Runtime.CompilerServices;
using Browser;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe class CameraBuffer
{
    public static CameraBuffer Instance;

    internal Buffer[] _uniformBuffers;
    internal DeviceMemory[] _uniformMemory;

    public DescriptorSetLayout Layout;
    public DescriptorSet[] DescriptorSets;

    public void Init()
    {
        Instance = this;
        CreateLayout();
        CreateBuffers();
        CreateDescriptorSets();
    }

    void CreateLayout()
    {
        DescriptorSetLayoutBinding binding = new()
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit,
        };

        DescriptorSetLayoutCreateInfo info = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &binding,
        };

        BrowserWindow.vk.CreateDescriptorSetLayout(BrowserWindow.device, &info, null, out Layout);
    }

    void CreateBuffers()
    {
        _uniformBuffers = new Buffer[BrowserWindow.MAX_FRAMES_IN_FLIGHT];
        _uniformMemory = new DeviceMemory[BrowserWindow.MAX_FRAMES_IN_FLIGHT];

        for (int i = 0; i < BrowserWindow.MAX_FRAMES_IN_FLIGHT; i++)
        {
            BufferHelper.CreateBuffer((ulong)Unsafe.SizeOf<UICameraUBO>(), BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref _uniformBuffers[i], ref _uniformMemory[i]);
        }
    }

    void CreateDescriptorSets()
    {
        var layouts = stackalloc DescriptorSetLayout[BrowserWindow.MAX_FRAMES_IN_FLIGHT];
        for (int i = 0; i < BrowserWindow.MAX_FRAMES_IN_FLIGHT; i++)
            layouts[i] = Layout;

        DescriptorSetAllocateInfo allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = GlobalDescriptorPool.Pool,
            DescriptorSetCount = BrowserWindow.MAX_FRAMES_IN_FLIGHT,
            PSetLayouts = layouts,
        };

        DescriptorSets = new DescriptorSet[BrowserWindow.MAX_FRAMES_IN_FLIGHT];
        fixed (DescriptorSet* ptr = DescriptorSets)
            BrowserWindow.vk.AllocateDescriptorSets(
                BrowserWindow.device, &allocInfo, ptr);

        for (int i = 0; i < BrowserWindow.MAX_FRAMES_IN_FLIGHT; i++)
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = _uniformBuffers[i],
                Offset = 0,
                Range = (ulong)Unsafe.SizeOf<UICameraUBO>()
            };

            WriteDescriptorSet write = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = DescriptorSets[i],
                DstBinding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                PBufferInfo = &bufferInfo,
            };

            BrowserWindow.vk.UpdateDescriptorSets(
                BrowserWindow.device, 1, &write, 0, null);
        }
    }

    // called once per frame in BrowserWindow
    public void Update(uint currentFrame, Matrix4X4<float> proj)
    {
        var ubo = new UICameraUBO { Proj = proj };

        void* data;
        BrowserWindow.vk.MapMemory(BrowserWindow.device, _uniformMemory[currentFrame], 0, (ulong)Unsafe.SizeOf<UICameraUBO>(), 0, &data);
        new Span<UICameraUBO>(data, 1)[0] = ubo;
        BrowserWindow.vk.UnmapMemory(BrowserWindow.device, _uniformMemory[currentFrame]);
    }

    public void Dispose()
    {
        for (int i = 0; i < BrowserWindow.MAX_FRAMES_IN_FLIGHT; i++)
            BufferHelper.DestroyBuffer(_uniformBuffers[i], _uniformMemory[i]);

        BrowserWindow.vk.DestroyDescriptorSetLayout(BrowserWindow.device, Layout, null);
    }
}