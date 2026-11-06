using System;
using GraphicsCore;
using GraphicsCore.Events;
using TextureCore;
using PrimitiveCore.Shader;
using PrimitiveCore;
using InstanceFinderCore;

namespace Vulkan;

public class NodeCreator
{
    uint siteId;
    public readonly HashSet<Node> Elements = [];
    public uint LastCreatedIndex = 0;
    static NodeShader defaultNodeShader;

    public NodeCreator(uint siteId)
    {
        this.siteId = siteId;
    }

    public static void SetDefaultShader(NodeShader shader)
    {
        defaultNodeShader = shader;
    }

    public Node Create(uint modelId, Texture? texture = null, VisualElement? parent = null, NodeShader? shader = null)
    {
        uint objectIndex = LastCreatedIndex++;
        Node runtimeModelData = new(modelId, objectIndex, texture, InstanceRegistry.Get<EventSystem>(), parent);
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
