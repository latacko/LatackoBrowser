using System;
using GraphicsCore;
using GraphicsCore;
using Silk.NET.Maths;
using TextCore.Slots;
using Units;
using Vulkan;
using VulkanManager.BufferManager;

namespace TextCore;

public class RuntimeText : VisualElement
{
    public VulkanManager.BufferManager.Slot Slot;
    public SlotData<TextVertex> VertexSlotData = new();
    public SlotData<ushort> IndicesSlotData = new();
    ReadOnlyMemory<char> Text;
    internal int leftRange = 0;
    internal int rightRange = 0;

    /// <summary>
    /// Only for debug pupropses.
    /// </summary>
    /// <returns></returns>
    public string TextStr => Text.Span[leftRange..rightRange].ToString();

    public int TextLength => rightRange - leftRange;

    protected internal override int ObjectDataSize  => 0;

    public float Left;
    public float Top;


    float widthWithoutScale;

    Bounds bounds = new();

    RuntimeTextContainer textContainer;


    public RuntimeText(ReadOnlyMemory<char> text, int leftRange, int rightRange, uint objectIndex, VisualElement parent) : base(new([], []), objectIndex, null, parent)
    {
        Text = text;
        this.leftRange = leftRange;
        this.rightRange = rightRange;

        if (parent is not RuntimeTextContainer)
            throw new Exception("Runtime text can be only a child of runtime text container!");

        textContainer = (RuntimeTextContainer)parent;
        GenerateMesh();
    }


    public bool Equals(int leftRange, int rightRange)
    {
        return this.leftRange == leftRange && this.rightRange == rightRange;
    }

    public void UpdateText(int leftRange, int rightRange)
    {
        this.leftRange = leftRange;
        this.rightRange = rightRange;
        GenerateMesh();
    }

    //FIXME - troszkę zaniża długość tekstu
    public static float GetTextWidth(FontAtlas fontAtlas, ReadOnlySpan<char> text, float textSize)
    {
        if (text.IsEmpty) return 0f;

        float cursorX = 0f;
        float invHeight = 1f / fontAtlas.height;
        int textLength = text.Length;
        var glyphs = fontAtlas.Glyphs;

        for (int i = 0; i < textLength; i++)
        {
            GlyphData g = glyphs[text[i]];

            // 1. Odwzorowanie dodawania szerokości właściwej znaku (jeśli istnieje)
            float glyphWidth = g.Width;
            if (glyphWidth != 0)
            {
                cursorX += glyphWidth * invHeight;
            }

            // Jeśli to ostatni znak, nie przetwarzamy odstępu do następnego
            if (i + 1 == textLength)
                break;

            // 2. Odwzorowanie dodawania odstępu między znakami
            float nextBearingX = glyphs[text[i + 1]].BearingX;
            float spacingWidth = g.Advance - g.Width - g.BearingX + nextBearingX;

            if (spacingWidth != 0)
            {
                cursorX += spacingWidth * invHeight;
            }
        }

        return cursorX * textSize;
    }


    public void GenerateMesh()
    {
        // Console.WriteLine("trying to generate mesh with size of: " + ((rightRange - leftRange) * 4 + 2) + " " + TextStr);
        // if (VertexSlotData != null && VertexSlotData.GetRingBuffer() != null && VertexSlotData.GetRingBuffer().buffersInfo[0] != null && IndicesSlotData != null)
        //     Console.WriteLine("Vertex buffer: " + VertexSlotData.GetRingBuffer().buffersInfo[0].Buffer.Handle + " index buffer " + +IndicesSlotData.GetRingBuffer().buffersInfo[0].Buffer.Handle);

        TextVertex[] _vertices = new TextVertex[(rightRange - leftRange) * 4 + 2];
        ushort[] _indices = new ushort[(rightRange - leftRange) * 4 + 2];

        float _cursorX = 0;
        int _j = 0;
        float invHeight = 1f / textContainer.fontAtlas.height;

        ReadOnlySpan<char> _text = Text.Span;

        GlyphData _fcharGlyphData = textContainer.fontAtlas.Glyphs[_text[leftRange]];
        _vertices[0] = new TextVertex(new(0, 1, 0), new(_fcharGlyphData.UVMin.X, _fcharGlyphData.UVMax.Y), _text[leftRange]);
        _vertices[1] = new TextVertex(new(0, 0, 0), new(_fcharGlyphData.UVMin.X, _fcharGlyphData.UVMin.Y), _text[leftRange]);

        _indices[0] = 1;
        _indices[1] = 0;

        for (int i = leftRange; i < rightRange; i++)
        {
            GlyphData _glyphData = textContainer.fontAtlas.Glyphs[_text[i]];

            float _width = _glyphData.Width;
            if (_width != 0)
            {
                _width *= invHeight;
                GenerateQuad(_vertices, _indices, _width, _glyphData.UVMin, _glyphData.UVMax, _cursorX, _text[i], ref _j, false);
                _cursorX += _width;
            }

            if (i + 1 == rightRange)
                break;

            GlyphData _nextCharGlyphData = textContainer.fontAtlas.Glyphs[_text[i + 1]];

            _width = _glyphData.Advance - _glyphData.Width - _glyphData.BearingX + textContainer.fontAtlas.Glyphs[_text[i + 1]].BearingX;

            if (_width != 0)
            {
                _width *= invHeight;
                GenerateQuad(_vertices, _indices, _width, _nextCharGlyphData.UVMin, _nextCharGlyphData.UVMax, _cursorX, _text[i + 1], ref _j, true);
                _cursorX += _width;
            }

            // _width = (glyphData.Advance - glyphData.Width) * invHeight;

            // _cursorX += _width;
        }


        VertexSlotData.Data = _vertices;
        VertexSlotData.dataCount = _j * 2 + 2;
        IndicesSlotData.Data = _indices;
        IndicesSlotData.dataCount = _j * 2 + 2;

        widthWithoutScale = _cursorX;
        UpdateBounds();
        AddFlag(RenderDirtyFlags.Model | RenderDirtyFlags.Matrix);

        TextManager.Instance.Update(this);
        // if (VertexSlotData != null && VertexSlotData.GetRingBuffer() != null && VertexSlotData.GetRingBuffer().buffersInfo[0] != null && IndicesSlotData != null)
        //     Console.WriteLine("New Vertex buffer: " + VertexSlotData.GetRingBuffer().buffersInfo[0].Buffer.Handle + " index buffer " + +IndicesSlotData.GetRingBuffer().buffersInfo[0].Buffer.Handle);
    }

    void UpdateBounds()
    {
        bounds.Width = widthWithoutScale * textContainer.computedStyle.FontSize;
        bounds.Height = textContainer.computedStyle.FontSize;
    }

    const uint FLAG_FLIP_IF_END_CHAR = 1u << 31;

    public void GenerateQuad(TextVertex[] vertices, ushort[] indices, float width, Vector2D<float> UVMin, Vector2D<float> UVMax, float x, uint charAscii, ref int i, bool beginning)
    {
        int _vericesIndex = i * 2 + 2;
        int _indicesIndex = i * 2 + 2;

        if (!beginning)
            charAscii ^= FLAG_FLIP_IF_END_CHAR;

        vertices[_vericesIndex] = new TextVertex(new(x + width, 0, 0), beginning ? new(UVMin.X, UVMin.Y) : new(UVMax.X, UVMin.Y), (uint)charAscii);
        vertices[_vericesIndex + 1] = new TextVertex(new(x + width, 1, 0), beginning ? new(UVMin.X, UVMax.Y) : new(UVMax.X, UVMax.Y), (uint)charAscii);

        // vertices[_vericesIndex + 0] = new Vertex(new(x, 0, 0), new(0, 0));
        // vertices[_vericesIndex + 1] = new Vertex(new(x, 1, 0), new(0, 1));
        // vertices[_vericesIndex + 2] = new Vertex(new(x + width, 1, 0), new(1, 1));
        // vertices[_vericesIndex + 3] = new Vertex(new(x + width, 0, 0), new(1, 0));

        indices[_indicesIndex] = (ushort)(_vericesIndex);
        indices[_indicesIndex + 1] = (ushort)(_vericesIndex + 1);

        i++;
    }

    protected internal override Vector2D<float> GetLayoutSize()
    {
        return new Vector2D<float>(widthWithoutScale * textContainer.computedStyle.FontSize / 2, textContainer.fontAtlas.height * textContainer.computedStyle.FontSize);
    }

    public RuntimeText SetPosition(float left, float top)
    {
        Left = left;
        Top = top;
        return this;
    }

    public override void AddChild(VisualElement runtimeModelData)
    {
        throw new System.Exception("You can't add children to a text");
    }

    public override Bounds GetBounds() => bounds;

    public override CursorType GetCursorType() => Style.FontProperties.Cursor;

    //FIXME - the text is diffrent between buffers when resizing
    public override bool TryGetObjectData(out ModelData data, uint frame)
    {
        if (Swapchain.Instance.recreatedSwapChain)
        {
            // Console.WriteLine("Recreated");
            AddFlag(RenderDirtyFlags.Matrix);
        }

        if (renderDirty[frame] == RenderDirtyFlags.None || renderDirty[frame] == RenderDirtyFlags.Model)
        {
            data = default;
            return false;
        }

        if (renderDirty[frame].HasFlag(RenderDirtyFlags.Matrix))
        {
            Console.WriteLine("Text matrix " + textContainer.computedStyle);
            cachedModel =
                Matrix4X4.CreateScale(textContainer.computedStyle.FontSize, textContainer.computedStyle.FontSize, 1f) *
                        // Matrix4X4.CreateScale(GetLayoutSize().X, GetLayoutSize().Y, 1f) *
                        // Matrix4X4.CreateFromYawPitchRoll(Transform.Rotation.X, Transform.Rotation.Y, Transform.Rotation.Z) *
                        // (
                        //     Parent == null ?
                        Matrix4X4.CreateTranslation(Left, Top, 0f);
            // Matrix4X4.CreateTranslation(10, 10, 0f);
            //         Matrix4X4.CreateTranslation(Parent.GetLayoutLeft() + Layout.LayoutPos.X + Layout.Left.Value, Parent.GetLayoutTop() + Layout.LayoutPos.Y + Layout.Top.Value, 0f)
            // );
            if (Parent != null)
            {
                // Console.WriteLine("relative pos: " + Parent.relativePos.Y);
                cachedModel *=
                    Matrix4X4.CreateTranslation(-Parent.relativeTransformation.X, -Parent.relativeTransformation.Y, 0) *
                    Matrix4X4.CreateFromYawPitchRoll(Parent.relativeRot.X, Parent.relativeRot.Y, Parent.relativeRot.Z) *
                    Matrix4X4.CreateTranslation(Parent.relativePos.X, Parent.relativePos.Y, Parent.relativePos.Z);
            }
            RemoveFlag(RenderDirtyFlags.Matrix, frame);
        }

        data = new ModelData
        {
            Model = cachedModel,
        };



        // Console.WriteLine(dirty[frame] + "frame: " + frame);
        // Console.WriteLine(data);

        RemoveFlag(RenderDirtyFlags.Data, frame);

        return true;
    }

    protected internal override float GetLayoutLeft() => 0;
    protected internal override float GetLayoutTop() => 0;


    protected internal override void UpdatePosition()
    {
        AddFlag(RenderDirtyFlags.Matrix);
        return;
    }


    protected internal override void UpdateChildrenLayout()
    {
        throw new Exception("You shoudn't update children layout for this object.");
    }

    protected internal override void Arrange(ref float cursorX, ref float cursorY, Action newLine, Action<float> sizeOfLine, ref float width)
    {
        throw new System.Exception("This funtion shoudn't be executed on runtime text!");
    }

    protected internal override bool TryWriteObjectData(Span<byte> destination, uint frame)
    {
        throw new NotImplementedException();
    }
}


public record struct Slot(
    uint VertexOffset,
    uint IndexOffset,
    BucketSize Bucket
);

public enum BucketSize : uint
{
    Tiny = 16,
    Small = 32,
    Medium = 64,
    Large = 128,
    Huge = 256,
}
