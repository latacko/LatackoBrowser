using System;
using System.Runtime.CompilerServices;
using AtlasGeneratorCore;
using GraphicsCore;
using GraphicsCore;
using Silk.NET.Maths;
using TextCore.Slots;
using Units;
using Vulkan;
using VulkanManager.BufferManager;

namespace TextCore.Text;

public class TextLine : VisualElement
{
    public Slot Slot;
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

    protected internal override int ObjectDataSize => Unsafe.SizeOf<TextLineGPUData>();

    public float Left;
    public float Top;


    internal double widthWithoutScale;

    Bounds bounds = new();

    internal TextContainer textContainer;


    public TextLine(ReadOnlyMemory<char> text, int leftRange, int rightRange, VisualElement parent) : base(null, parent)
    {
        Text = text;
        this.LayoutManager = new TextLineLayoutManager();

        this.leftRange = leftRange;
        this.rightRange = rightRange;

        if (parent is not TextContainer)
            throw new Exception("Runtime text can be only a child of runtime text container!");

        textContainer = (TextContainer)parent;
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
    public static double GetTextWidth(FontManager fontManager, ReadOnlySpan<char> text, float textSize)
    {
        if (text.IsEmpty) return 0f;

        double cursorX = 0f;
        double invHeight = 1f / fontManager.GetFontGeometry().GetMetrics().UniversalHeight;
        int textLength = text.Length;

        for (int i = 0; i < textLength; i++)
        {
            MsdfAtlasGen.GlyphGeometry g = fontManager.GetFontAtlas().GetGlyph(text[i]);

            // 1. Odwzorowanie dodawania szerokości właściwej znaku (jeśli istnieje)
            double glyphWidth = g.GetWidth();
            if (glyphWidth != 0)
            {
                cursorX += glyphWidth * invHeight;
            }

            // Jeśli to ostatni znak, nie przetwarzamy odstępu do następnego
            if (i + 1 == textLength)
                break;

            // 2. Odwzorowanie dodawania odstępu między znakami
            double nextBearingX = fontManager.GetFontAtlas().GetGlyph(text[i + 1]).GetBearingX();
            double spacingWidth = g.GetAdvance() - g.GetWidth() - g.GetBearingX() + nextBearingX;

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

        double _cursorX = 0;
        int _j = 0;
        double invHeight = 1f / textContainer.fontManager.GetFontGeometry().GetMetrics().UniversalHeight;

        ReadOnlySpan<char> _text = Text.Span;

        var _fontAtlas = textContainer.fontManager.GetFontAtlas();

        var _fcharGlyphData = _fontAtlas.GetGlyph(_text[leftRange]);

        //TODO - Włączenie koordynatów uv
        // _vertices[0] = new TextVertex(new(0, 1, 0), new(_fcharGlyphData.UVMin.X, _fcharGlyphData.UVMax.Y), _text[leftRange]);
        // _vertices[1] = new TextVertex(new(0, 0, 0), new(_fcharGlyphData.UVMin.X, _fcharGlyphData.UVMin.Y), _text[leftRange]);

        _vertices[0] = new TextVertex(new(0, 1, 0), new(0, 1), _text[leftRange]);
        _vertices[1] = new TextVertex(new(0, 0, 0), new(0, 1), _text[leftRange]);


        _indices[0] = 1;
        _indices[1] = 0;

        for (int i = leftRange; i < rightRange; i++)
        {
            var _glyphData = _fontAtlas.GetGlyph(_text[i]);

            double _width = _glyphData.GetWidth();
            if (_width != 0)
            {
                _width *= invHeight;
                GenerateQuad(_vertices, _indices, (float)_width, new(0,0), new(1,1), (float)_cursorX, _text[i], ref _j, false);
                //TODO - Włączenie koordynatów uv
                // GenerateQuad(_vertices, _indices, _width, _glyphData.UVMin, _glyphData.UVMax, _cursorX, _text[i], ref _j, false);
                _cursorX += _width;
            }

            if (i + 1 == rightRange)
                break;

            var _nextCharGlyphData = _fontAtlas.GetGlyph(_text[i + 1]);

            _width = _glyphData.GetAdvance() - _glyphData.GetWidth() - _glyphData.GetBearingX() + _nextCharGlyphData.GetBearingX();

            if (_width != 0)
            {
                _width *= invHeight;
                GenerateQuad(_vertices, _indices, (float)_width, new(0,0), new(1,1), (float)_cursorX, _text[i + 1], ref _j, true);
                //TODO - Włączenie koordynatów uv
                // GenerateQuad(_vertices, _indices, _width, _nextCharGlyphData.UVMin, _nextCharGlyphData.UVMax, _cursorX, _text[i + 1], ref _j, true);
                _cursorX += _width;
            }

            // _width = (glyphData.Advance - glyphData.Width) * invHeight;

            // _cursorX += _width;
        }


        VertexSlotData.Data = _vertices;
        VertexSlotData.dataCount = _j * 2 + 2;
        IndicesSlotData.Data = _indices;
        IndicesSlotData.dataCount = _j * 2 + 2;

        AssetCore.AssetManager.RegisterModel()

        widthWithoutScale = _cursorX;
        UpdateBounds();
        AddFlag(RenderDirtyFlags.Model | RenderDirtyFlags.Matrix);

        TextManager.Instance.Update(this);
        // if (VertexSlotData != null && VertexSlotData.GetRingBuffer() != null && VertexSlotData.GetRingBuffer().buffersInfo[0] != null && IndicesSlotData != null)
        //     Console.WriteLine("New Vertex buffer: " + VertexSlotData.GetRingBuffer().buffersInfo[0].Buffer.Handle + " index buffer " + +IndicesSlotData.GetRingBuffer().buffersInfo[0].Buffer.Handle);
    }

    void UpdateBounds()
    {
        bounds.Width = (float)widthWithoutScale * textContainer.computedStyle.FontSize;
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

    public TextLine SetPosition(float left, float top)
    {
        Left = left;
        Top = top;
        return this;
    }

    public override void AddChild(VisualElement runtimeModelData)
    {
        throw new System.Exception("You can't add children to a text");
    }

    public override CursorType GetCursorType() => Style.FontProperties.Cursor;

    protected internal override bool TryWriteObjectData(Span<byte> destination, uint frame)
    {
        return TryGetObjectData(out var data, frame) && WriteStruct(data, destination);
    }

    //FIXME - the text is diffrent between buffers when resizing
    public bool TryGetObjectData(out TextLineGPUData data, uint frame)
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

        data = new TextLineGPUData
        {
            Model = cachedModel,
        };



        // Console.WriteLine(dirty[frame] + "frame: " + frame);
        // Console.WriteLine(data);

        RemoveFlag(RenderDirtyFlags.Data, frame);

        return true;
    }
}
