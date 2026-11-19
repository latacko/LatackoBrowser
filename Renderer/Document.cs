using System;
using GraphicsCore;
using GraphicsCore.Styles;
using PrimitiveCore;
using PrimitiveCore.Shader;
using Vulkan;
using VulkanManager;

namespace Renderer;

public class Document
{
    readonly TextureRenderer textureRenderer;
    readonly NodeCreator nodeCreator;
    readonly StylesManager stylesManager;
    public Node Body { get; private set; }
    public Document(TextureRenderer textureRenderer, NodeCreator nodeCreator, StylesManager stylesManager)
    {
        this.textureRenderer = textureRenderer;
        this.nodeCreator = nodeCreator;
        this.stylesManager = stylesManager;
    }

    public void SetBody(Node body)
    {
        if (this.Body != null)
        {
            Console.WriteLine("[ERROR] The body was already set for this site");
            return;
        }
        if (body == null)
            throw new System.ArgumentNullException("The body can not be null");
            
        this.Body = body;
    }

    public VisualElement CreateElement(uint modelId, TextureCore.Texture? texture = null, VisualElement? parent = null, NodeShader? shader = null)
    {
        return nodeCreator.Create(textureRenderer.GetId(), modelId, stylesManager, texture, parent, shader);
    }
}
