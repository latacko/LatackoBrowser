using System;
using Vulkan;

namespace CoreManager;

internal class BufferManager : IDisposable
{

    internal void Init()
    {
        CoreManager.Instance.textManager.RegisterBuffer();
        CoreManager.Instance.objectsManager.RegisterBuffers();
    }

    public void Dispose()
    {
        CoreManager.Instance.textManager.Dispose();
        CoreManager.Instance.objectsManager.Dispose();
    }
}
