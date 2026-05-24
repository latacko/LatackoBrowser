using Browser;
using GraphicCore;
using ObjectCore;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public static class ObjectsManager
{
    public static readonly List<RuntimeModelData> Elements = [];
    public static uint LastCreatedIndex = 0;
    public static RuntimeObject AddObject(ObjectShader objectShader, ModelData<ushort> modelData, RuntimeModelData? parent = null)
    {
        uint objectIndex = LastCreatedIndex++;
        RuntimeObject runtimeModelData = new(modelData, objectIndex, parent);
        if (parent != null)
        {
            parent.AddChild(runtimeModelData);
        }
        objectShader.elements.Add(runtimeModelData);
        Elements.Add(runtimeModelData);

        return runtimeModelData;
    }
}