using Browser;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public static class ObjectsManager
{
    public static unsafe RuntimeModelData AddObject(BaseShader baseShader, ModelData<ushort> modelData)
    {
        int objectIndex = baseShader.LastCreatedIndex++;
        RuntimeModelData runtimeModelData = new(modelData, objectIndex);

        baseShader.elements.Add(runtimeModelData);

        return runtimeModelData;
    }
}