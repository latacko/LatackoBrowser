using System;
using GraphicsCore;
using GraphicsCore.Events;
using PrimitiveCore.Model;
using TextureCore;

namespace PrimitiveCore;

public static class NodeCreator
{
    public static readonly HashSet<Node> Elements = [];
    public static uint LastCreatedIndex = 0;
    static NodeShader defaultNodeShader;

    public static void SetDefaultShader(NodeShader shader)
    {
        defaultNodeShader = shader;
    }

    public static Node Create(uint modelId, Texture? texture = null, VisualElement? parent = null, NodeShader? shader = null)
    {
        uint objectIndex = LastCreatedIndex++;
        Node runtimeModelData = new(modelId, objectIndex, texture, EventSystem.Instance, parent);
        parent?.AddChild(runtimeModelData);

        VisualElement.ObjectsToCompile.Add(new()
        {
            runtimeModel = runtimeModelData,
            shader = shader ?? defaultNodeShader
        });

        Elements.Add(runtimeModelData);

        return runtimeModelData;
    }
}
