using System;
using GraphicsCore.Buffers;
using VulkanManager.BufferManager;

namespace GraphicsCore.Shaders;

public class ShaderCreator<TBaseShader> where TBaseShader: BaseShader, IShaderFactory<TBaseShader>
{
    public TBaseShader CreateShader(string shaderPath)
    {
        return TBaseShader.Create(shaderPath, CreateObjectsManager);
    }
    protected InstancesManager CreateObjectsManager(Type gpuDataType)
    {
        return new InstancesManager(gpuDataType);
    }
}
