using Browser;
using GraphicCore;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public static class ObjectsManager
{
    public static readonly List<RuntimeModelData> Elements = [];
    public static uint LastCreatedIndex = 0;
    public static RuntimeModelData AddObject(BaseShader baseShader, ModelData<ushort> modelData, RuntimeModelData? parent = null)
    {
        uint objectIndex = LastCreatedIndex++;
        RuntimeModelData runtimeModelData = new(modelData, objectIndex, parent);
        if (parent != null)
        {
            parent.Children ??= new();
            parent.Children.Add(runtimeModelData);
        }
        baseShader.elements.Add(runtimeModelData);
        Elements.Add(runtimeModelData);

        return runtimeModelData;
    }
}