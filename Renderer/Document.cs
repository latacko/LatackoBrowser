using System;
using GraphicsCore;
using Vulkan;

namespace Renderer;

public class Document
{
    readonly NodeCreator nodeCreator;

    public Document(NodeCreator nodeCreator)
    {
        this.nodeCreator = nodeCreator;
    }

    public VisualElement CreateElement(uint modelId, TextureCore.Texture? texture = null, VisualElement? parent = null, NodeShader? shader = null)
    {
        return nodeCreator.Create(modelId, texture, parent, shader);
    }
}
