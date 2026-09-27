using PrimitiveCore;
using Silk.NET.Vulkan;
using TextCore;
using VulkanManager;

namespace CoreManager;

public class CoreManager : IDisposable, IRenderTick
{
    // BufferManager bufferManager = new();

    internal TextManager textManager = new();
    // internal NodesManager objectsManager = new();
    // public ObjectManager objectManager = new();
    public void InitBuffers()
    {
        // bufferManager.Init();
    }

    public void Start()
    {
        // fontManager.LoadFont("google-noto/NotoSerif-Regular.ttf");
        // fontManager.LoadFont("stix-fonts/STIXTwoText-Regular.otf");
        // fontManager.LoadFont("sil-padauk-fonts/Padauk-Regular.ttf");
    }
    uint _ticksToReset = 0;
    public void Update(double deltaTime)
    {
        GraphicsCore.ColorTransitionsHelper.Update((float)deltaTime);
        _ticksToReset++;

        if (_ticksToReset == 1000)
        {
            _ticksToReset = 0;
        }
    }

    public void RenderTick(uint frameInFlight)
    {
        textManager.RenderTick(frameInFlight);
    }

    // public void RenderShader(CommandBuffer commandBuffer, uint frameInFlight, bool wireFrameRendering)
    // {
    //     TextManager.TextShader.Render(commandBuffer, frameInFlight, wireFrameRendering);
    // }

    public void Dispose()
    {
        // fontManager.Dispose();
        // bufferManager.Dispose();
    }
}
