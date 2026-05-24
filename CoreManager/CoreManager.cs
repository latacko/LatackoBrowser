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
        fontManager.LoadFont("/usr/share/fonts/open-sans/OpenSans-Regular.ttf");
    }

    public void Update(double deltaTime)
    {
        GraphicCore.ColorTransitionsHelper.Update((float)deltaTime);
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
