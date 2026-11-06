using System;
using AssetCore;
using GraphicsCore.Buffers;
using VulkanManager.BufferManager;

namespace GraphicsCore.Shaders;

public class ShaderCreator<TBaseShader> where TBaseShader : BaseShader, IShaderFactory<TBaseShader>
{
    readonly Dictionary<string, TBaseShader> createdShaders = [];
    public TBaseShader CreateShader<TMeshData, TInstanceData>(string shaderPath)
    where TMeshData : IMeshData
    where TInstanceData : struct
    {
        Type meshData = typeof(TMeshData);
        Type instanceData = typeof(TInstanceData);
        string _fullShaderSaveName = shaderPath + "_" + meshData.Name + "_" + instanceData.Name;
        Console.WriteLine("Full shader name" + _fullShaderSaveName);
        if (createdShaders.TryGetValue(_fullShaderSaveName, out TBaseShader? shader))
            return shader;
        else
        {
            TBaseShader _newShader = TBaseShader.Create(shaderPath, meshData, instanceData, CreateObjectsManager);
            createdShaders.Add(_fullShaderSaveName, _newShader);
            return _newShader;
        }
    }
    protected InstancesManager CreateObjectsManager(Type meshData, Type gpuDataType)
    {
        if (!BaseShader.InstancesManagerByType.ContainsKey(meshData))
            BaseShader.InstancesManagerByType.Add(meshData, new InstancesManager(meshData, gpuDataType));
        return BaseShader.InstancesManagerByType[meshData];
    }
}
