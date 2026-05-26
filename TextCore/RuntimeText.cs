using System;
using GraphicCore;
using GraphicsCore;
using Silk.NET.Maths;
using TextCore.Styles;
using Units;
using Vulkan;

namespace TextCore;

public class RuntimeText : RuntimeModelData<RuntimeText, TextData>
{
    public Slot Slot;
    public Properties Properties;
    FontAtlas fontAtlas;

    public string Text;

    float widthWithoutScale;

    Bounds bounds = new();


    public RuntimeText(string text, uint objectIndex, RuntimeModelData? parent = null) : base(new([], []), objectIndex, parent)
    {
        Text = text;
        Properties = new(this);
    }

    public void GenerateMesh()
    {
        Vertex[] _vertices = new Vertex[(Text.Length * 2) * 4];
        ushort[] _indices = new ushort[(Text.Length * 2) * 6];
        float _lastX = 0;
        int _j = 0;

        int _textLength = Text.Length;


        for (int i = 0; i < _textLength; i++)
        {
            GlyphData glyphData = fontAtlas.Glyphs[Text[i]];
            float _nextX = glyphData.Width / glyphData.Height;
            GenerateQuad(_vertices, _indices, _nextX, glyphData.UVMin, glyphData.UVMax, _lastX, ref _j);
            _lastX += _nextX;

            _nextX = glyphData.Advance / glyphData.Height;
            // _nextX = glyphData.Advance + (i>0 ? fontAtlas.Glyphs[Text[i-1]].BearingX : 0) / glyphData.Height;

            GenerateQuad(_vertices, _indices, _nextX, new(0, 0), new(0, 0), _lastX, ref _j);

            _lastX += _nextX;
        }

        ModelData.Vertices = _vertices;
        ModelData.Indices = _indices;

        widthWithoutScale = _lastX;
        UpdateBounds();
        AddFlag(DirtyFlags.Model | DirtyFlags.Matrix);

        TextManager.Instance.Update(this);
    }

    void UpdateBounds()
    {
        bounds.Width = widthWithoutScale * Properties.fontSize.Value;
        bounds.Height = Properties.fontSize.Value;
    }

    public void GenerateQuad(Vertex[] vertices, ushort[] indices, float width, Vector2D<float> UVMin, Vector2D<float> UVMax, float x, ref int i)
    {
        int _vericesIndex = i * 4;
        int _indicesIndex = i * 6;

        vertices[_vericesIndex + 0] = new Vertex(new(x, 0, 0), new(UVMin.X, UVMin.Y));
        vertices[_vericesIndex + 1] = new Vertex(new(x, 1, 0), new(UVMin.X, UVMax.Y));
        vertices[_vericesIndex + 2] = new Vertex(new(x + width, 1, 0), new(UVMax.X, UVMax.Y));
        vertices[_vericesIndex + 3] = new Vertex(new(x + width, 0, 0), new(UVMax.X, UVMin.Y));

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

    public RuntimeText SetProperties(Func<Properties, Properties> setProperties)
    {
        return SetProperties(setProperties.Invoke(Properties));
    }

    public RuntimeText SetProperties(Properties properties)
    {
        Properties = properties;
        fontAtlas = FontManager.Instance.GetFontAtlas(Properties.font);
        UpdateBounds();
        AddFlag(DirtyFlags.Data);
        ConvertToPx(new());
        GenerateMesh();

        return this;
    }

    public override void AddChild(RuntimeModelData runtimeModelData)
    {
        throw new System.Exception("You can't add children to a text");
    }

    public override Bounds GetBounds() => bounds;

    public override CursorType GetCursorType() => Properties.Cursor;

    public override bool TryGetObjectData(out TextData data, uint frame)
    {
        if (Swapchain.Instance.recreatedSwapChain)
        {
            Console.WriteLine("Recreated");
            UpdateParentSize();
            AddFlag(DirtyFlags.Matrix);
        }

        if (dirty[frame] == DirtyFlags.None || dirty[frame] == DirtyFlags.Model)
        {
            data = default;
            return false;
        }

        if (dirty[frame].HasFlag(DirtyFlags.Matrix))
        {
            _cachedModel =
                Matrix4X4.CreateScale(GetLayoutSize().X, GetLayoutSize().Y, 1f) *
                        // Matrix4X4.CreateFromYawPitchRoll(Transform.Rotation.X, Transform.Rotation.Y, Transform.Rotation.Z) *
                        // (
                        //     Parent == null ?
                        Matrix4X4.CreateTranslation(10, 10, 0f);
            //         Matrix4X4.CreateTranslation(Parent.GetLayoutLeft() + Layout.LayoutPos.X + Layout.Left.Value, Parent.GetLayoutTop() + Layout.LayoutPos.Y + Layout.Top.Value, 0f)
            // );
            RemoveFlag(DirtyFlags.Matrix, frame);
        }

        data = new TextData
        {
            Model = _cachedModel,
            Color = Properties.TextColor,

            pos = new Vector2D<float>(_cachedModel.M41, _cachedModel.M42),
            size = GetLayoutSize(),

            TextureIndex = 0,
        };



        // Console.WriteLine(dirty[frame] + "frame: " + frame);
        Console.WriteLine(data);

        RemoveFlag(DirtyFlags.Data, frame);

        return true;
    }

    protected internal override void ConvertToPx(Vector2D<float> parentSize)
    {
        Properties.ConvertToPx(parentSize);
    }

    protected internal override float GetLayoutLeft() => 0;
    protected internal override float GetLayoutTop() => 0;

    protected internal override Vector2D<float> GetLayoutSize()
    {
        return new Vector2D<float>(widthWithoutScale * Properties.fontSize.Value, fontAtlas.height);
    }


    protected internal override void UpdateLayout(ref float cursorX, ref float cursorY, ref float sizeOfLine, ref float width)
    {
        return;
    }

    protected internal override void UpdatePosition()
    {
        return;
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
