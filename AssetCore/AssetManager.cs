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
}
