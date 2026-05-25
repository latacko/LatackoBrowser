using ObjectCore;
using TextCore;

namespace CoreManager;

public class CoreManager : IDisposable
{
    internal static CoreManager Instance;
    BufferManager bufferManager = new();

    internal FontManager fontManager = new();
    internal TextManager textManager = new();
    internal ObjectsManager objectsManager = new();

    public CoreManager()
    {
        Instance = this;
    }

    public void InitBuffers()
    {
        bufferManager.Init();
    }

    public void Start()
    {
        fontManager.LoadFont("open-sans/OpenSans-Regular");
    }

    public void Update(double deltaTime)
    {
        GraphicCore.ColorTransitionsHelper.Update((float)deltaTime);
        fontManager.Tick();
    }

    public void Render(uint currentFrame)
    {
        textManager.CopyToBuffer(currentFrame);
    }

    public void Dispose()
    {
        fontManager.Dispose();
        bufferManager.Dispose();
    }
}
