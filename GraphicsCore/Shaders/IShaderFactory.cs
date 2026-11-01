using System;
using AssetCore;
using GraphicsCore.Buffers;
using VulkanManager.BufferManager;

namespace GraphicsCore.Shaders;

public interface IShaderFactory<TSelf> where TSelf: BaseShader
{
    static abstract TSelf Create(string shaderPath, Type meshData, Type instanceData, Func<Type, Type, InstancesManager> objectsManagerFunc);
}
