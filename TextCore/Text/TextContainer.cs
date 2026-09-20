using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AtlasGeneratorCore;
using GraphicsCore;
using Silk.NET.Maths;

namespace TextCore.Text;

public class TextContainer : VisualElement
{
    internal FontManager fontManager;
    public string Text;
    internal ReadOnlyMemory<char> TextMemory;
    internal List<TextLine> runtimeTexts = new();
    protected internal override int ObjectDataSize => Unsafe.SizeOf<TextContainerGPUData>();

    public TextContainer(string text, uint objectIndex, VisualElement? parent = null) : base(0, objectIndex, null, parent)
    {
        SetStyle(TextManager.TextDefaultStyle);

        Text = text;
        TextMemory = Text.AsMemory();
        fontManager = FontsManager.Instance.GetFontManager(Style.FontProperties.font);
        fontManager.LoadCharset(Text);
    }

    public override void AddChild(VisualElement runtimeModelData)
    {
        throw new Exception("You can't add children to this container. It's children are managed internaly");
    }

    public void AddChild(TextLine runtimeText)
    {
        runtimeTexts.Add(runtimeText);
        Console.WriteLine("Adding text: " + runtimeText.TextStr);
    }

    public override CursorType GetCursorType()
    {
        throw new Exception("You shoudn't get cursor for this object. You should iterate throught internal texts for cursor.");
    }

    public bool TryGetObjectData(out TextContainerGPUData data, uint frame)
    {
        if (renderDirty[frame] == RenderDirtyFlags.None || renderDirty[frame] == RenderDirtyFlags.Model)
        {
            data = default;
            return false;
        }


        data = new TextContainerGPUData
        {
            Color = Style.FontProperties.TextColor,
            TextureIndex = fontManager.GetFontAtlas().Id,
        };

        Console.WriteLine("Text color: " + data.Color + " " + renderDirty[frame]);


        // Console.WriteLine(dirty[frame] + "frame: " + frame);
        // Console.WriteLine(data);

        RemoveFlag(RenderDirtyFlags.Data, frame);

        return true;
    }

    protected internal override bool TryWriteObjectData(Span<byte> destination, uint frame)
    {
        return TryGetObjectData(out var data, frame) && WriteStruct(data, destination);
    }

    public override void Compile()
    {
        base.Compile();
        AddFlag(RenderDirtyFlags.Data);
    }
}
