namespace GraphicCore;

public abstract class BufferManager : IDisposable {
    public abstract void RegisterBuffer();
    public abstract void Dispose();
}