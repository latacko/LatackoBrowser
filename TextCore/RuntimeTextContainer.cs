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
    internal List<RuntimeText> runtimeTexts = new();

    public RuntimeTextContainer(string text, uint objectIndex, RuntimeModelData? parent = null) : base(null, objectIndex, parent)
    {
        Text = text;
        Properties = new(this);
        fontAtlas = FontManager.Instance.GetFontAtlas(Properties.font);
        fontAtlas.ScanText(Text);
        UpdateParentSize();
        ConvertToPx(ParentSize);
    }

    public RuntimeTextContainer SetProperties(Func<Properties, Properties> setProperties)
    {
        return SetProperties(setProperties.Invoke(Properties));
    }

    public RuntimeTextContainer SetProperties(Properties properties)
    {
        Properties = properties;
        fontAtlas = FontManager.Instance.GetFontAtlas(Properties.font);
        fontAtlas.ScanText(Text);
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

    public void AddChild(RuntimeText runtimeText)
    {
        runtimeTexts.Add(runtimeText);
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
        Properties.ConvertToPx(parentSize);
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
        return Parent.GetLayoutSize();
    }

    protected internal override float GetLayoutTop()
    {
        throw new Exception("You shoudn't get layout for this object.");
    }

    protected internal override void UpdateLayout(ref float cursorX, ref float cursorY, Action newLine, Action<float> updatedSizeOfLine, ref float width)
    {
        ReadOnlySpan<char> _remainingText = (Text + "").AsSpan();

        Console.WriteLine("Updating layout for text: " + Text);

        while (_remainingText.Length > 0)
        {
            Vector2D<float> _size = RuntimeText.GetTextSize(fontAtlas, _remainingText, Properties.fontSize.Value);
            Console.WriteLine("Size of whole text: " + _size + " > " + (width-cursorX));
            if (_size.X > width - cursorX)
                SliceText(ref _remainingText, ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
            else
            {
                AddText(_remainingText, ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
                _remainingText = [];
            }
        }
    }

    void SliceText(ref ReadOnlySpan<char> _remainingText, ref float cursorX, ref float cursorY, Action newLine, Action<float> updatedSizeOfLine, ref float width)
    {
        List<int> _breakOpportunities = BreakOpportunites(_remainingText);
        int _breakIndex = GetTextThatWillFit(_remainingText, width - cursorX, _breakOpportunities);
        Console.WriteLine("The break index for thix text is: " + _breakIndex + " break opportunities: " + string.Join(", ", _breakOpportunities));
        if (_breakIndex == -1)
        {
            Console.WriteLine("Creating whole remaining text: " + _remainingText.ToString());
            AddText(_remainingText, ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
            _remainingText = [];
        }
        else
        {
            Console.WriteLine("Creating remaining text to break index: " + _remainingText[0..+_breakIndex].ToString());
            AddText(_remainingText[0..+_breakIndex], ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
            _remainingText = _remainingText[(_breakIndex + 1)..];
        }
    }

    void AddText(ReadOnlySpan<char> text, ref float cursorX, ref float cursorY, Action newLine, Action<float> updatedSizeOfLine, ref float width)
    {
        var _runtimeText = TextManager.AddModelText(text.ToString(), this);
        var _size = _runtimeText.GetLayoutSize();
        _runtimeText.SetPosition(cursorX, cursorY);

        updatedSizeOfLine(fontAtlas.lineGap + _size.Y);

        if (cursorX + _size.X > width)
            newLine();

        cursorX += _size.X;
    }

    List<int> BreakOpportunites(ReadOnlySpan<char> text)
    {
        var _breakIndexes = new List<int>();

        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
                _breakIndexes.Add(i);
        }
        return _breakIndexes;
    }

    int GetTextThatWillFit(ReadOnlySpan<char> text, float space, List<int> breakOpportunities)
    {
        if (breakOpportunities.Count == 0)
            return -1;

        int _left = 0;
        int _right = breakOpportunities.Count - 1;
        int _bestBreakPos = -1;

        while (_left <= _right)
        {
            int _mid = _left + (_right - _left) / 2;
            int _breakPos = breakOpportunities[_mid];

            Vector2D<float> _size = RuntimeText.GetTextSize(fontAtlas, text[0.._breakPos], Properties.fontSize.Value);

            if (_size.X <= space)
            {
                _bestBreakPos = _breakPos; // actual char index, not the list index
                _left = _mid + 1;
            }
            else
            {
                _right = _mid - 1;
            }
        }

        return _bestBreakPos;
    }

    protected internal override void UpdatePosition()
    {
        relativePos = Parent!.relativePos;
        relativeRot = Parent!.relativeRot;

        foreach (var runtimeText in runtimeTexts)
        {
            runtimeText.AddFlag(DirtyFlags.Matrix);
        }
    }
}
