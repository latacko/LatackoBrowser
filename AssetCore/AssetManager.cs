using System;

namespace AssetCore;

public static class AssetManager
{
    static uint lastModelId = 0;
    static readonly Dictionary<uint, IMeshData> models = [];

    public static uint RegisterModel(IMeshData meshData)
    {
        models.Add(lastModelId, meshData);
        return lastModelId++;
    }

    public static void RemoveModel(uint modelId)
    {
        models.Remove(modelId);
    }

    public static IMeshData GetModel(uint modelId)
    {
        return models[modelId];
    }

    public static uint GetModelCountOfType<TMeshData>() where TMeshData : IMeshData
    {
        uint _count = 0;
        foreach (var model in models)
        {
            if (model.Value is TMeshData)
                _count++;
        }
        return _count;
    }

    public static uint[] GetIdsOfModelType<TMeshData>() where TMeshData : IMeshData
    {
        uint[] _ids = new uint[GetModelCountOfType<TMeshData>()];
        uint _i = 0;
        foreach (var model in models)
        {
            if (model.Value is TMeshData)
            {
                _ids[_i] = model.Key;
                _i++;
            }
        }
        return _ids;
    }
}
