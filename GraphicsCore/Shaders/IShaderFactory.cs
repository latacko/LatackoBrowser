using System;
using GraphicsCore.Buffers;
using VulkanManager.BufferManager;

namespace GraphicsCore.Shaders;

public interface IShaderFactory<TSelf> where TSelf: BaseShader
{
    static abstract TSelf Create(string shaderPath, Func<Type, InstancesManager> objectsManagerFunc);
}
