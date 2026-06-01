using System;
using System.Diagnostics;
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
    ReadOnlyMemory<char> TextMemory;
    internal List<RuntimeText> runtimeTexts = new();

    public RuntimeTextContainer(string text, uint objectIndex, RuntimeModelData? parent = null) : base(null, objectIndex, parent)
    {
        Text = text;
        TextMemory = Text.AsMemory();
        Properties = new(this);
        fontAtlas = FontManager.Instance.GetFontAtlas(Properties.font);
        fontAtlas.ScanText(Text);
        ConvertToPx();
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
        ConvertToPx();
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
            UpdateMySize();
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

    protected internal override void ConvertToPx()
    {
        Properties.ConvertToPx(ParentSize);
        foreach (var runtimeText in runtimeTexts)
        {
            runtimeText.ConvertToPx();
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

    private readonly List<int> _breakOpportunitiesBuffer = new();

    Stopwatch stopwatch = new();
    double msUpdatetime;
    uint UpdateLayoutCount = 0;

    protected internal override void UpdateLayout(ref float cursorX, ref float cursorY, Action newLine, Action<float> updatedSizeOfLine, ref float width)
    {
        UpdateLayoutCount++;
        stopwatch.Restart();
        ReadOnlySpan<char> _remainingText = Text.AsSpan();
        int _leftSlice = 0;


        int _runtimeTextToReuse = 0;
        int _runtimeTextCount = runtimeTexts.Count;

        int _usedTexts = -1;
        while (_leftSlice < TextMemory.Length)
        {
            if (_runtimeTextToReuse >= _runtimeTextCount)
            {
                _runtimeTextToReuse = -1;
            }

            float _measuredTextWidth = RuntimeText.GetTextWidth(fontAtlas, _remainingText[_leftSlice..], Properties.fontSize.Value);
            if (_measuredTextWidth > width - cursorX)
            {
                SliceText(_runtimeTextToReuse, ref _leftSlice, ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
                _runtimeTextToReuse++;
                newLine();
            }
            else
            {
                AddText(_runtimeTextToReuse, _leftSlice, Text.Length, ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
                _leftSlice = Text.Length;
                _runtimeTextToReuse++;
            }
            _usedTexts++;

            // Console.WriteLine("Zostało tekstu: " + (_remainingText.Length-_leftSlice));
        }

        for (int i = runtimeTexts.Count - 1; i > _usedTexts; i--)
        {
            runtimeTexts[i].VertexSlotData.GetRingBuffer().Remove(runtimeTexts[i].VertexSlotData);
            runtimeTexts[i].IndicesSlotData.GetRingBuffer().Remove(runtimeTexts[i].IndicesSlotData);
            runtimeTexts.RemoveAt(i);
        }
        stopwatch.Stop();
        msUpdatetime += stopwatch.ElapsedMilliseconds;
        // Console.WriteLine("Layout update avarage: " + (msUpdatetime/UpdateLayoutCount) + "ms");
    }

    void SliceText(int runtimeTextToReuse, ref int leftSlice, ref float cursorX, ref float cursorY, Action newLine, Action<float> updatedSizeOfLine, ref float width)
    {
        var _span = TextMemory.Span;
        List<int> _breakOpportunities = BreakOpportunites(_span[leftSlice..]);
        // Console.WriteLine("Break oppotunities: " + string.Join(", ", _breakOpportunities));
        int _breakIndex = GetTextThatWillFit(_span[leftSlice..], width - cursorX, _breakOpportunities);
        // Console.WriteLine("Final index: " + _breakIndex + " left slice: " + leftSlice);
        if (_breakIndex == -1)
        {
            AddText(runtimeTextToReuse, leftSlice, TextMemory.Length, ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
            leftSlice = TextMemory.Length;
        }
        else
        {
            AddText(runtimeTextToReuse, leftSlice, leftSlice + _breakIndex, ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
            leftSlice = leftSlice + _breakIndex + 1;
        }
    }

    void AddText(int runtimeTextToReuse, int leftSlice, int rightSlice, ref float cursorX, ref float cursorY, Action newLine, Action<float> updatedSizeOfLine, ref float width)
    {
        lock (runtimeTexts)
        {
            RuntimeText _runtimeText;
            if (runtimeTextToReuse == -1)
                _runtimeText = TextManager.AddModelText(Text.AsMemory(), leftSlice, rightSlice, this);
            else
            {
                _runtimeText = runtimeTexts[runtimeTextToReuse];

                if (!_runtimeText.Equals(leftSlice, rightSlice))
                    _runtimeText.UpdateText(leftSlice, rightSlice);
            }
            var _size = _runtimeText.GetLayoutSize();

            _runtimeText.SetPosition(cursorX, cursorY);
            updatedSizeOfLine(fontAtlas.lineGap + _size.Y);

            cursorX += _size.X;
        }

    }

    List<int> BreakOpportunites(ReadOnlySpan<char> text)
    {
        _breakOpportunitiesBuffer.Clear();

        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
                _breakOpportunitiesBuffer.Add(i);
        }
        return _breakOpportunitiesBuffer;
    }

    int GetTextThatWillFit(ReadOnlySpan<char> text, float space, List<int> breakOpportunities)
    {
        if (breakOpportunities.Count == 0)
            return -1;

        Span<float> prefixWidths = breakOpportunities.Count <= 128
        ? stackalloc float[breakOpportunities.Count]
        : new float[breakOpportunities.Count];

        int prevBreak = 0;
        for (int i = 0; i < breakOpportunities.Count; i++)
        {
            int breakPos = breakOpportunities[i];
            float segmentWidth = RuntimeText.GetTextWidth(fontAtlas, text[prevBreak..breakPos], Properties.fontSize.Value);
            prefixWidths[i] = (i == 0 ? 0f : prefixWidths[i - 1]) + segmentWidth;
            prevBreak = breakPos;
        }

        int _left = 0;
        int _right = breakOpportunities.Count - 1;
        int _bestBreakPos = -1;

        while (_left <= _right)
        {
            int _mid = _left + (_right - _left) / 2;
            if (prefixWidths[_mid] <= space)
            {
                _bestBreakPos = breakOpportunities[_mid];
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
        relativeTransformation = Parent!.relativeTransformation;

        foreach (var runtimeText in runtimeTexts)
        {
            runtimeText.AddFlag(DirtyFlags.Matrix);
        }
    }
}
