using System;
using System.Diagnostics;
using GraphicCore;
using GraphicsCore;
using Silk.NET.Maths;
using Vulkan;

namespace TextCore;

public class RuntimeTextContainer : RuntimeModelData<RuntimeTextContainer, TextContainerData, IModelData>
{
    internal FontAtlas fontAtlas;
    public string Text;
    ReadOnlyMemory<char> TextMemory;
    internal List<RuntimeText> runtimeTexts = new();

    public RuntimeTextContainer(string text, uint objectIndex, RuntimeModelData? parent = null) : base(null, objectIndex, parent)
    {
        SetStyle(TextManager.TextDefaultStyle);

        Text = text;
        TextMemory = Text.AsMemory();
        fontAtlas = FontManager.Instance.GetFontAtlas(Style.FontProperties.font);
        fontAtlas.ScanText(Text);
    }

    public override void AddChild(RuntimeModelData runtimeModelData)
    {
        throw new Exception("You can't add children to this container. It's children are managed internaly");
    }

    public void AddChild(RuntimeText runtimeText)
    {
        runtimeTexts.Add(runtimeText);
        Console.WriteLine("Adding text: " + runtimeText.TextStr);
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
            ParentSizeUpdated();
            AddFlag(RenderDirtyFlags.Matrix);
        }

        if (renderDirty[frame] == RenderDirtyFlags.None || renderDirty[frame] == RenderDirtyFlags.Model)
        {
            data = default;
            return false;
        }


        data = new TextContainerData
        {
            Color = Style.FontProperties.TextColor,
            TextureIndex = fontAtlas.id,
        };



        // Console.WriteLine(dirty[frame] + "frame: " + frame);
        // Console.WriteLine(data);

        RemoveFlag(RenderDirtyFlags.Data, frame);

        return true;
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

    bool sthChanged = false;

    protected internal override void UpdateChildrenLayout()
    {
        throw new Exception("You shoudn't update children layout for this object.");
    }

    protected internal override void Arrange(ref float cursorX, ref float cursorY, Action newLine, Action<float> updatedSizeOfLine, ref float width)
    {
        Console.WriteLine("Update layout width: " + width + "px");
        stopwatch.Restart();
        ReadOnlySpan<char> _remainingText = Text.AsSpan();
        int _leftSlice = 0;


        int _runtimeTextToReuse = 0;
        int _runtimeTextCount = runtimeTexts.Count;
        sthChanged = false;

        int _usedTexts = -1;
        while (_leftSlice < TextMemory.Length)
        {
            if (_runtimeTextToReuse >= _runtimeTextCount)
            {
                _runtimeTextToReuse = -1;
            }

            float _measuredTextWidth = RuntimeText.GetTextWidth(fontAtlas, _remainingText[_leftSlice..], computedStyle.FontSize);
            Console.WriteLine("For text: " + _remainingText[_leftSlice..].ToString() + " width is " + _measuredTextWidth);
            if (_measuredTextWidth > width - cursorX)
            {
                Console.WriteLine("Too much slicing");
                SliceText(_runtimeTextToReuse, ref _leftSlice, ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
                _runtimeTextToReuse++;
                newLine();
            }
            else
            {
                Console.WriteLine("Good adding all");
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
        if (sthChanged)
        {
            UpdateLayoutCount++;
            msUpdatetime += stopwatch.ElapsedMilliseconds;
        }
        // Console.WriteLine("Layout update avarage: " + (msUpdatetime / UpdateLayoutCount) + "ms");
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
            {
                _runtimeText = TextManager.AddModelText(Text.AsMemory(), leftSlice, rightSlice, this);
                sthChanged = true;
            }
            else
            {
                _runtimeText = runtimeTexts[runtimeTextToReuse];

                if (!_runtimeText.Equals(leftSlice, rightSlice))
                {
                    _runtimeText.UpdateText(leftSlice, rightSlice);
                    sthChanged = true;
                }
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
            float segmentWidth = RuntimeText.GetTextWidth(fontAtlas, text[prevBreak..breakPos], computedStyle.FontSize);
            Console.WriteLine("For segment: " + text[prevBreak..breakPos].ToString() + " width is: " + segmentWidth + " font size is: " + computedStyle.FontSize);
            prefixWidths[i] = (i == 0 ? 0f : prefixWidths[i - 1]) + segmentWidth;
            prevBreak = breakPos;
        }


        int _left = 0;
        int _right = breakOpportunities.Count - 1;
        int _bestBreakPos = -1;

        Console.WriteLine("Space for text is: " + space);

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
        Console.WriteLine("Finaly text is: " + text[0.._bestBreakPos].ToString());

        return _bestBreakPos;
    }

    protected internal override void UpdatePosition()
    {
        relativePos = Parent!.relativePos;
        relativeRot = Parent!.relativeRot;
        relativeTransformation = Parent!.relativeTransformation;

        foreach (var runtimeText in runtimeTexts)
        {
            runtimeText.AddFlag(RenderDirtyFlags.Matrix);
        }
    }
}
