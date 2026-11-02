using System;
using AssetCore;
using GraphicsCore.Buffers;
using VulkanManager.BufferManager;

namespace GraphicsCore.Shaders;

public class ShaderCreator<TBaseShader> where TBaseShader : BaseShader, IShaderFactory<TBaseShader>
{
    public TBaseShader CreateShader<TMeshData, TInstanceData>(string shaderPath)
    where TMeshData : IMeshData
    where TInstanceData : struct
    {
        Type meshData = typeof(TMeshData);
        Type instanceData = typeof(TInstanceData);
        Console.WriteLine("Mesh data: " + meshData);
        return TBaseShader.Create(shaderPath, meshData, instanceData, CreateObjectsManager);
    }
    protected InstancesManager CreateObjectsManager(Type meshData, Type gpuDataType)
    {
        if (!BaseShader.InstancesManagerByType.ContainsKey(meshData))
            BaseShader.InstancesManagerByType.Add(meshData, new InstancesManager(meshData, gpuDataType));
        return BaseShader.InstancesManagerByType[meshData];
    }
}
