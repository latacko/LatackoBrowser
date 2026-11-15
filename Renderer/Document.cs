using System;
using GraphicsCore;
using GraphicsCore.Styles;
using PrimitiveCore.Shader;
using Vulkan;
using VulkanManager;

namespace Renderer;

public class Document
{
    readonly TextureRenderer textureRenderer;
    readonly NodeCreator nodeCreator;
    readonly StylesManager stylesManager;

    public Document(TextureRenderer textureRenderer, NodeCreator nodeCreator, StylesManager stylesManager)
    {
        this.textureRenderer = textureRenderer;
        this.nodeCreator = nodeCreator;
        this.stylesManager = stylesManager;
    }

    public VisualElement CreateElement(uint modelId, TextureCore.Texture? texture = null, VisualElement? parent = null, NodeShader? shader = null)
    {
        return nodeCreator.Create(textureRenderer.GetId(), modelId, stylesManager, texture, parent, shader);
    }
}
