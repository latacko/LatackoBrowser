using System;
using GraphicCore;
using TextCore;

namespace Browser;

public class TextManager
{
    public static readonly List<RuntimeText> Elements = [];
    public static uint LastCreatedIndex = 0;
    public static RuntimeText AddObject(string text, TextShader textShader, RuntimeModelData? parent = null)
    {
        uint objectIndex = LastCreatedIndex++;
        RuntimeText runtimeModelData = new(text, objectIndex, parent);
        if (parent != null)
        {
            parent.AddChild(runtimeModelData);
        }
        textShader.elements.Add(runtimeModelData);
        Elements.Add(runtimeModelData);

        return runtimeModelData;
    }
}
