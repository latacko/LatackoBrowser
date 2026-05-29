using ObjectCore;
using ObjectCore.Textures;
using TextCore;

namespace CoreManager;

public class CoreManager : IDisposable
{
    public static CoreManager Instance;
    BufferManager bufferManager = new();

    internal FontManager fontManager = new();
    internal TextManager textManager = new();
    internal ObjectsManager objectsManager = new();
    public ObjectManager objectManager = new();
    public TexturesManager texturesManager = new();

    public CoreManager()
    {
        Instance = this;
    }

    public void InitBuffers()
    {
        texturesManager.Init();
        bufferManager.Init();
    }

    public void Start()
    {
        fontManager.LoadFont("open-sans/OpenSans-Regular.ttf");
        fontManager.LoadFont("stix-fonts/STIXTwoText-Regular.otf");
    }
    uint _ticksToReset = 0;
    public void Update(double deltaTime)
    {
        GraphicCore.ColorTransitionsHelper.Update((float)deltaTime);
        _ticksToReset++;

        fontManager.Tick();
        if (_ticksToReset == 1000)
        {
            TexturesManager.Tick();
            _ticksToReset = 0;
        }
    }

    public void Render(uint currentFrame)
    {
        textManager.CopyToBuffer(currentFrame);
    }

    public void Dispose()
    {
        fontManager.Dispose();
        bufferManager.Dispose();
        texturesManager.Dispose();
    }
}
