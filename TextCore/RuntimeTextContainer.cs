using System;
using GraphicCore;
using GraphicsCore;
using Silk.NET.Maths;
using TextCore.Styles;
using Vulkan;

namespace TextCore;

public class RuntimeTextContainer : RuntimeModelData<RuntimeTextContainer, TextContainerData, IModelData>
{
    internal FontAtlas fontAtlas;
    public Properties Properties;
    public string Text;
    List<RuntimeText> runtimeTexts = new();

    public RuntimeTextContainer(string text, uint objectIndex, RuntimeModelData? parent = null) : base(null, objectIndex, parent)
    {
        fontAtlas.ScanText(Text);
        Text = text;
        Properties = new(this);
    }

    public RuntimeTextContainer SetProperties(Func<Properties, Properties> setProperties)
    {
        return SetProperties(setProperties.Invoke(Properties));
    }

    public RuntimeTextContainer SetProperties(Properties properties)
    {
        Properties = properties;
        fontAtlas = FontManager.Instance.GetFontAtlas(Properties.font);
        // UpdateBounds();
        AddFlag(DirtyFlags.Data);
        ConvertToPx(new());
        // GenerateMesh();

        return this;
    }

    public override void AddChild(RuntimeModelData runtimeModelData)
    {
        throw new Exception("You can't add children to this container. It's children are managed internaly");
    }

    public override Bounds GetBounds()
    {
        throw new Exception("You shoudn't get bounds of this object. You should iterate throught internal texts bounds.");
    }

    public override CursorType GetCursorType()
    {
        throw new Exception("You shoudn't get cursor for this object. You should iterate throught internal texts for cursor.");
    }

    public override bool TryGetObjectData(out TextContainerData data, uint frame)
    {
        if (Swapchain.Instance.recreatedSwapChain)
        {
            // Console.WriteLine("Recreated");
            UpdateParentSize();
            AddFlag(DirtyFlags.Matrix);
        }

        if (dirty[frame] == DirtyFlags.None || dirty[frame] == DirtyFlags.Model)
        {
            data = default;
            return false;
        }


        data = new TextContainerData
        {
            Color = Properties.TextColor,
            TextureIndex = fontAtlas.id,
        };



        // Console.WriteLine(dirty[frame] + "frame: " + frame);
        // Console.WriteLine(data);

        RemoveFlag(DirtyFlags.Data, frame);

        return true;
    }

    protected internal override void ConvertToPx(Vector2D<float> parentSize)
    {
        foreach (var runtimeText in runtimeTexts)
        {
            runtimeText.ConvertToPx(parentSize);
        }
    }

    protected internal override float GetLayoutLeft()
    {
        throw new Exception("You shoudn't get layout for this object.");
    }

    protected internal override Vector2D<float> GetLayoutSize()
    {
        throw new Exception("You shoudn't get layout for this object.");
    }

    protected internal override float GetLayoutTop()
    {
        throw new Exception("You shoudn't get layout for this object.");
    }

    protected internal override void UpdateLayout(ref float cursorX, ref float cursorY, ref float sizeOfLine, ref float width)
    {
        string _remainingText = Text;

        List<int> _breakOpportunities = BreakOpportunites(_remainingText);
        int _breakIndex = GetTextThatWillFit(width - cursorX, _breakOpportunities);
    }

    List<int> BreakOpportunites(string text)
    {
        var _breakIndexes = new List<int>();

        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
                _breakIndexes.Add(i);
        }
        return _breakIndexes;
    }

    int GetTextThatWillFit(float space, List<int> breakOpportunities)
    {
        if (breakOpportunities.Count == 0)
            return -1;

        int _left = 0;
        int _right = breakOpportunities.Count - 1;

        int _bestIndex = -1;

        while (_left <= _right)
        {
            int _mid = _left + (_right - _left) / 2;


            Vector2D<float> _size = RuntimeText.GetTextSize(fontAtlas, Text.Substring(0, breakOpportunities[_mid]), Properties.fontSize.Value);
            if (_size.X <= space)
            {
                // fits -> try a larger one
                _bestIndex = _mid;
                _left = _mid + 1;
            }
            else
            {
                // too wide -> try smaller
                _right = _mid - 1;
            }
        }

        return _bestIndex;
    }

    protected internal override void UpdatePosition()
    {
        throw new NotImplementedException();
    }
}
