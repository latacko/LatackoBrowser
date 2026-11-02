using System;

namespace AssetCore;

public static class AssetManager
{
    static uint lastModelId = 0;
    static readonly Dictionary<Type, Action<IMeshData>> addMeshDataToBuffer = [];
    static readonly Dictionary<uint, IMeshData> models = [];

    public static uint RegisterModel(IMeshData meshData, bool instantLoad = false)
    {
        if (!addMeshDataToBuffer.TryGetValue(meshData.GetType(), out var add))
            throw new System.NotSupportedException("This type of data doesn't have registered buffer action.");

        uint _assetId = lastModelId++;
        meshData.AssetId = _assetId;

        if (instantLoad)
            add.Invoke(meshData);
        models.Add(lastModelId, meshData);
        return meshData.AssetId;
    }

    public static void LoadModel(uint assetId)
    {
        IMeshData meshData = GetModel(assetId);
        if (!addMeshDataToBuffer.TryGetValue(meshData.GetType(), out var add))
            throw new System.NotSupportedException("This type of data doesn't have registered buffer action.");
        add.Invoke(meshData);
    }

    public static void RegisterMeshDataBufferManager<TMeshData>(Action<IMeshData> registerMeshData) where TMeshData : IMeshData
    {
        addMeshDataToBuffer.Add(typeof(TMeshData), registerMeshData);
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
