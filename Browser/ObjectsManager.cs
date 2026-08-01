using Browser;
using GraphicCore;
using ObjectCore;
using ObjectCore.Textures;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public static class ObjectsManager
{
    public static readonly List<RuntimeModelData> Elements = [];
    public static uint LastCreatedIndex = 0;
    public static RuntimeObject AddObject(ObjectShader objectShader, ObjectModelData<ushort> modelData, Texture texture, RuntimeModelData? parent = null)
    {
        uint objectIndex = LastCreatedIndex++;
        RuntimeObject runtimeModelData = new(modelData, objectIndex, texture, parent);
        parent?.AddChild(runtimeModelData);

        RuntimeModelData.ObjectsToCompile.Add(new()
        {
            runtimeModel = runtimeModelData,
            shader = objectShader
        });

        Elements.Add(runtimeModelData);

        return runtimeModelData;
    }
}