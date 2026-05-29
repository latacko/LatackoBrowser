using System;
using Vulkan;

namespace CoreManager;

internal class BufferManager : IDisposable
{

    internal void Init()
    {
        CoreManager.Instance.textManager.RegisterBuffer();
        CoreManager.Instance.objectsManager.RegisterBuffers();
        CoreManager.Instance.objectManager.RegisterBuffer();
    }

    public void Dispose()
    {
        CoreManager.Instance.textManager.Dispose();
        CoreManager.Instance.objectsManager.Dispose();
        CoreManager.Instance.objectManager.Dispose();
    }
}
