using Browser;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public static class ObjectsManager
{
    public static uint LastCreatedIndex = 0;
    public static RuntimeModelData AddObject(BaseShader baseShader, ModelData<ushort> modelData)
    {
        uint objectIndex = LastCreatedIndex++;
        RuntimeModelData runtimeModelData = new(modelData, objectIndex);

        baseShader.elements.Add(runtimeModelData);

        return runtimeModelData;
    }
}