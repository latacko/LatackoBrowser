using System;
using GraphicCore;
using GraphicsCore;
using Silk.NET.Maths;
using TextCore.Slots;
using TextCore.Styles;
using Units;
using Vulkan;
using VulkanManager.BufferManager;

namespace TextCore;

public class RuntimeText : RuntimeModelData<RuntimeText, ModelData, TextModelData<ushort>>
{
    public VulkanManager.BufferManager.Slot Slot;
    public SlotData<TextVertex> VertexSlotData = new();
    public SlotData<ushort> IndicesSlotData = new();
    ReadOnlyMemory<char> Text;
    int leftRange = 0;
    int rightRange = 0;

    /// <summary>
    /// Only for debug pupropses.
    /// </summary>
    /// <returns></returns>
    public string TextStr => Text.Span[leftRange..rightRange].ToString();

    public int TextLength => rightRange - leftRange;

    public float Left;
    public float Top;


    float widthWithoutScale;

    Bounds bounds = new();

    RuntimeTextContainer textContainer;


    public RuntimeText(ReadOnlyMemory<char> text, int leftRange, int rightRange, uint objectIndex, RuntimeModelData parent) : base(new([], []), objectIndex, parent)
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

    public static float GetTextWidth(FontAtlas fontAtlas, ReadOnlySpan<char> text, float textSize)
    {
        float cursorX = 0f;
        float invHeight = 1f / fontAtlas.height;
        int textLength = text.Length;
        var glyphs = fontAtlas.Glyphs;

        for (int i = 0; i < textLength; i++)
        {
            GlyphData g = glyphs[text[i]];
            float nextBearingX = (i + 1 < textLength) ? glyphs[text[i + 1]].BearingX : 0f;
            cursorX += (2f * g.Advance - g.Width - g.BearingX + nextBearingX) * invHeight;
        }

        return cursorX * textSize;
    }

    public void GenerateMesh()
    {
        Console.WriteLine("trying to generate mesh with size of: " + ((rightRange - leftRange) * 2 * 4) + " " + TextStr);
        TextVertex[] _vertices = new TextVertex[(rightRange - leftRange) * 2 * 4];
        ushort[] _indices = new ushort[(rightRange - leftRange) * 2 * 6];
        float _cursorX = 0;
        int _j = 0;
        float invHeight = 1f / textContainer.fontAtlas.height;

        ReadOnlySpan<char> _text = Text.Span;
        int _textLength = Text.Length;

        for (int i = leftRange; i < rightRange; i++)
        {
            GlyphData glyphData = textContainer.fontAtlas.Glyphs[_text[i]];

            float _width = glyphData.Width;
            if (_width != 0)
            {
                _width *= invHeight;
                GenerateQuad(_vertices, _indices, _width, glyphData.UVMin, glyphData.UVMax, _cursorX, (uint)_text[i], ref _j);
                _cursorX += _width;
            }

            _width = glyphData.Advance - glyphData.Width - glyphData.BearingX + (i + 1 < _textLength ? textContainer.fontAtlas.Glyphs[_text[i + 1]].BearingX : 0);

            if (_width != 0)
            {
                _width *= invHeight;
                GenerateQuad(_vertices, _indices, _width, new(), new(), _cursorX, 0, ref _j);
                _cursorX += _width;
            }

            _width = (glyphData.Advance - glyphData.Width) * invHeight;

            _cursorX += _width;
        }

        VertexSlotData.Data = _vertices;
        IndicesSlotData.Data = _indices;

        widthWithoutScale = _cursorX;
        UpdateBounds();
        AddFlag(DirtyFlags.Model | DirtyFlags.Matrix);

        TextManager.Instance.Update(this);
    }

    void UpdateBounds()
    {
        bounds.Width = widthWithoutScale * textContainer.Properties.fontSize.Value;
        bounds.Height = textContainer.Properties.fontSize.Value;
    }

    public void GenerateQuad(TextVertex[] vertices, ushort[] indices, float width, Vector2D<float> UVMin, Vector2D<float> UVMax, float x, uint charAscii, ref int i)
    {
        int _vericesIndex = i * 4;
        int _indicesIndex = i * 6;

        vertices[_vericesIndex + 0] = new TextVertex(new(x, 0, 0), new(UVMin.X, UVMin.Y), (uint)charAscii);
        vertices[_vericesIndex + 1] = new TextVertex(new(x, 1, 0), new(UVMin.X, UVMax.Y), (uint)charAscii);
        vertices[_vericesIndex + 2] = new TextVertex(new(x + width, 1, 0), new(UVMax.X, UVMax.Y), (uint)charAscii);
        vertices[_vericesIndex + 3] = new TextVertex(new(x + width, 0, 0), new(UVMax.X, UVMin.Y), (uint)charAscii);

        // vertices[_vericesIndex + 0] = new Vertex(new(x, 0, 0), new(0, 0));
        // vertices[_vericesIndex + 1] = new Vertex(new(x, 1, 0), new(0, 1));
        // vertices[_vericesIndex + 2] = new Vertex(new(x + width, 1, 0), new(1, 1));
        // vertices[_vericesIndex + 3] = new Vertex(new(x + width, 0, 0), new(1, 0));

        indices[_indicesIndex + 0] = (ushort)(_vericesIndex + 0);
        indices[_indicesIndex + 1] = (ushort)(_vericesIndex + 1);
        indices[_indicesIndex + 2] = (ushort)(_vericesIndex + 2);
        indices[_indicesIndex + 3] = (ushort)(_vericesIndex + 2);
        indices[_indicesIndex + 4] = (ushort)(_vericesIndex + 3);
        indices[_indicesIndex + 5] = (ushort)(_vericesIndex + 0);

        i++;
    }

    protected internal override Vector2D<float> GetLayoutSize()
    {
        return new Vector2D<float>(widthWithoutScale * textContainer.Properties.fontSize.Value / 2, textContainer.fontAtlas.height * textContainer.Properties.fontSize.Value);
    }

    public RuntimeText SetPosition(float left, float top)
    {
        Left = left;
        Top = top;
        return this;
    }

    public override void AddChild(RuntimeModelData runtimeModelData)
    {
        throw new System.Exception("You can't add children to a text");
    }

    public override Bounds GetBounds() => bounds;

    public override CursorType GetCursorType() => textContainer.Properties.Cursor;

    public override bool TryGetObjectData(out ModelData data, uint frame)
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

        if (dirty[frame].HasFlag(DirtyFlags.Matrix))
        {
            cachedModel =
                Matrix4X4.CreateScale(textContainer.Properties.fontSize.Value, textContainer.Properties.fontSize.Value, 1f) *
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
            RemoveFlag(DirtyFlags.Matrix, frame);
        }

        data = new ModelData
        {
            Model = cachedModel,
        };



        // Console.WriteLine(dirty[frame] + "frame: " + frame);
        // Console.WriteLine(data);

        RemoveFlag(DirtyFlags.Data, frame);

        return true;
    }

    protected internal override void ConvertToPx()
    {
        textContainer.Properties.ConvertToPx(ParentSize);
    }

    protected internal override float GetLayoutLeft() => 0;
    protected internal override float GetLayoutTop() => 0;


    protected internal override void UpdatePosition()
    {
        AddFlag(DirtyFlags.Matrix);
        return;
    }

    protected internal override void UpdateLayout(ref float cursorX, ref float cursorY, Action newLine, Action<float> sizeOfLine, ref float width)
    {
        throw new System.Exception("This funtion shoudn't be executed on runtime text!");
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
