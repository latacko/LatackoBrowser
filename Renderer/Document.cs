using System;
using GraphicsCore;
using PrimitiveCore.Shader;
using Vulkan;
using VulkanManager;

namespace Renderer;

public class Document
{
    readonly TextureRenderer textureRenderer;
    readonly NodeCreator nodeCreator;

    public Document(TextureRenderer textureRenderer, NodeCreator nodeCreator)
    {
        this.textureRenderer = textureRenderer;
        this.nodeCreator = nodeCreator;
    }

    public VisualElement CreateElement(uint modelId, TextureCore.Texture? texture = null, VisualElement? parent = null, NodeShader? shader = null)
    {
        return nodeCreator.Create(textureRenderer.GetId(), modelId, texture, parent, shader);
    }
}
