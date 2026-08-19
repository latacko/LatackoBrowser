using Browser;
using GraphicsCore;
using PrimitiveCore;
using PrimitiveCore.Textures;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public static class ObjectsManager
{
    public static readonly List<VisualElement> Elements = [];
    public static uint LastCreatedIndex = 0;
    public static Node AddObject(NodeShader objectShader, ModelData<ushort> modelData, Texture texture, VisualElement? parent = null)
    {
        uint objectIndex = LastCreatedIndex++;
        Node runtimeModelData = new(modelData, objectIndex, texture, parent);
        parent?.AddChild(runtimeModelData);

        VisualElement.ObjectsToCompile.Add(new()
        {
            runtimeModel = runtimeModelData,
            shader = objectShader
        });

        Elements.Add(runtimeModelData);

        return runtimeModelData;
    }
}