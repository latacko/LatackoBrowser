using System;
using System.Diagnostics;
using GraphicsCore;
using GraphicsCore.Styles;
using Silk.NET.Maths;

namespace TextCore.Text;

public struct TextContainerLayoutManager : ILayoutManager
{

    TextContainer textContainer;
    Vector2D<float> parentSize;
    private readonly List<int> _breakOpportunitiesBuffer = new();

    Stopwatch stopwatch = new();

    bool sthChanged = false;

    public TextContainerLayoutManager() { }

    public VisualElement GetVisualElement() => textContainer;

    public void SetVisualElement(VisualElement visualElement)
    {
        textContainer = (TextContainer)visualElement;
    }

    public Vector2D<float> GetParentSize() => parentSize;

    public void SetParentSize(Vector2D<float> size)
    {
        parentSize = size;
    }

    public void Arrange(ref float cursorX, ref float cursorY, Action newLine, Action<float> updatedSizeOfLine, ref float width)
    {
        Console.WriteLine("============================= Update layout width: " + width + "px =============================");
        stopwatch.Restart();
        ReadOnlySpan<char> _remainingText = textContainer.Text.AsSpan();
        int _leftSlice = 0;


        int _runtimeTextToReuse = 0;
        int _runtimeTextCount = textContainer.runtimeTexts.Count;
        sthChanged = false;

        int _usedTexts = -1;
        while (_leftSlice < textContainer.TextMemory.Length)
        {
            if (_runtimeTextToReuse >= _runtimeTextCount)
            {
                _runtimeTextToReuse = -1;
            }

            float _measuredTextWidth = (float)TextLine.GetTextWidth(textContainer.fontManager, _remainingText[_leftSlice..], textContainer.computedStyle.FontSize);
            Console.WriteLine("For text: " + _remainingText[_leftSlice..].ToString() + " width is " + _measuredTextWidth + " computet font size: " + textContainer.computedStyle.FontSize);
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
                AddText(_runtimeTextToReuse, _leftSlice, textContainer.Text.Length, ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
                _leftSlice = textContainer.Text.Length;
                _runtimeTextToReuse++;
            }
            _usedTexts++;

            // Console.WriteLine("Zostało tekstu: " + (_remainingText.Length-_leftSlice));
        }
    }

    void SliceText(int runtimeTextToReuse, ref int leftSlice, ref float cursorX, ref float cursorY, Action newLine, Action<float> updatedSizeOfLine, ref float width)
    {
        var _span = textContainer.TextMemory.Span;
        List<int> _breakOpportunities = BreakOpportunites(_span[leftSlice..]);
        // Console.WriteLine("Break oppotunities: " + string.Join(", ", _breakOpportunities));
        int _breakIndex = GetTextThatWillFit(_span[leftSlice..], width - cursorX, _breakOpportunities);
        // Console.WriteLine("Final index: " + _breakIndex + " left slice: " + leftSlice);
        if (_breakIndex == -1)
        {
            AddText(runtimeTextToReuse, leftSlice, textContainer.TextMemory.Length, ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
            leftSlice = textContainer.TextMemory.Length;
        }
        else
        {
            AddText(runtimeTextToReuse, leftSlice, leftSlice + _breakIndex, ref cursorX, ref cursorY, newLine, updatedSizeOfLine, ref width);
            leftSlice = leftSlice + _breakIndex + 1;
        }
    }

    //NOTE - Line gap was removed idk what it does. Awaiting for testing with else fonts.
    void AddText(int runtimeTextToReuse, int leftSlice, int rightSlice, ref float cursorX, ref float cursorY, Action newLine, Action<float> updatedSizeOfLine, ref float width)
    {
        lock (textContainer.runtimeTexts)
        {
            TextLine _runtimeText;
            if (runtimeTextToReuse == -1)
            {
                _runtimeText = TextManager.AddModelText(textContainer.Text.AsMemory(), leftSlice, rightSlice, textContainer);
                sthChanged = true;
            }
            else
            {
                _runtimeText = textContainer.runtimeTexts[runtimeTextToReuse];

                if (!_runtimeText.Equals(leftSlice, rightSlice))
                {
                    _runtimeText.UpdateText(leftSlice, rightSlice);
                    sthChanged = true;
                }
            }
            var _size = _runtimeText.LayoutManager.GetLayoutSize();

            _runtimeText.SetPosition(cursorX, cursorY);
            //TODO - LINE GAP TO IMPLEMENT
            Console.WriteLine("Line gap: " + textContainer.fontManager.GetFontGeometry().GetMetrics() + " line gap to imlement px font size: " + textContainer.computedStyle.FontSize);
            // updatedSizeOfLine.Invoke(fontAtlas.lineGap * computedStyle.FontSize + _size.Y);
            updatedSizeOfLine.Invoke(textContainer.computedStyle.FontSize);

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
            float segmentWidth = (float)TextLine.GetTextWidth(textContainer.fontManager, text[prevBreak..breakPos], textContainer.computedStyle.FontSize);
            // Console.WriteLine("For segment: " + text[prevBreak..breakPos].ToString() + " width is: " + segmentWidth + " font size is: " + computedStyle.FontSize);
            prefixWidths[i] = (i == 0 ? 0f : prefixWidths[i - 1]) + segmentWidth;
            prevBreak = breakPos;
        }


        int _left = 0;
        int _right = breakOpportunities.Count - 1;
        int _bestBreakPos = -1;

        // Console.WriteLine("Space for text is: " + space);

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

        if (_bestBreakPos == -1 && breakOpportunities.Count > 0)
        {
            _bestBreakPos = breakOpportunities[0];
        }

        // Console.WriteLine("Finaly text is: " + text[0.._bestBreakPos].ToString());

        return _bestBreakPos;
    }

    public Bounds GetBounds()
    {
        throw new Exception("You shoudn't get bounds of this object. You should iterate throught internal texts bounds.");
    }

    public float GetLayoutLeft()
    {
        throw new Exception("You shoudn't get layout for this object.");
    }

    public Vector2D<float> GetLayoutSize()
    {
        return textContainer.Parent.LayoutManager.GetLayoutSize();
    }

    public float GetLayoutTop()
    {
        throw new Exception("You shoudn't get layout for this object.");
    }

    public void UpdateChildrenLayout()
    {
        throw new Exception("You shoudn't update children layout for this object.");
    }

    public void UpdatePosition()
    {
        textContainer.relativePos = textContainer.Parent!.relativePos;
        textContainer.relativeRot = textContainer.Parent!.relativeRot;
        textContainer.relativeTransformation = textContainer.Parent!.relativeTransformation;

        foreach (var runtimeText in textContainer.runtimeTexts)
        {
            runtimeText.AddFlag(VisualElement.RenderDirtyFlags.Matrix);
        }
    }
}
