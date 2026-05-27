using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Remora.MSDFGen;
using Remora.MSDFGen.Graphics;
using SharpFont;
using Silk.NET.Vulkan;

namespace TextCore;

public class FontManager : IDisposable
{
    public static FontManager Instance;
    uint lastId = 0;
    Dictionary<string, FontAtlas> loadedFonts = new();
    static Library library = new();
    // const string preload = "A";
    const string preload = "AMIE-WT";
    // const string preload = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?:;-–()[]{}'\"/\\@#";


    public FontManager()
    {
        Instance = this;
    }

    public void Tick()
    {
        foreach (var item in loadedFonts)
        {
            item.Value.Tick();
        }
    }

    void Test()
    {
        var testShape = new Shape();
        var testContour = new Contour();
        testContour.Edges.Add(new LinearSegment(new Vector2(10, 10), new Vector2(90, 10), EdgeColor.White));
        testContour.Edges.Add(new LinearSegment(new Vector2(90, 10), new Vector2(50, 90), EdgeColor.White));
        testContour.Edges.Add(new LinearSegment(new Vector2(50, 90), new Vector2(10, 10), EdgeColor.White));
        testShape.Contours.Add(testContour);
        testShape.InverseYAxis = false;

        var testPixmap = new Pixmap<Color3>(100, 100);
        Console.WriteLine("Test1");
        MSDF.GenerateMSDF(testPixmap, testShape, 4.0, new Vector2(1, 1), new Vector2(0, 0));
        Console.WriteLine("Test2");

        int testNonBlack = 0;
        for (int k = 0; k < 100 * 100; k++)
        {
            var c = testPixmap[k % 100, k / 100];
            if (c.R != 0 || c.G != 0 || c.B != 0) testNonBlack++;
        }
        Console.WriteLine($"Test shape non-black: {testNonBlack}");
    }

    public void LoadFont(string name)
    {
        string path = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            path = "/usr/share/fonts/";
        }

        path += name + ".ttf";
        if (loadedFonts.ContainsKey(name)) return;
        loadedFonts.Add(name, new());
        Console.WriteLine(path);
        var face = new Face(library, path);

        uint renderSize = 64 * 4;
        face.SetCharSize(0, 64 * 64, 72, 72);

        int glyphSize = FontAtlas.GLYPH_SIZE;
        int padding = FontAtlas.PADDING;

        loadedFonts[name].Create(lastId++);

        // float ascender = face.Size.Metrics.Ascender.ToSingle();
        // float descender = face.Size.Metrics.Descender.ToSingle();
        // float lineHeight = face.Size.Metrics.Height.ToSingle();

        float unitsPerEm = face.UnitsPerEM;

        loadedFonts[name].height = (face.Ascender - face.Descender) / (float)unitsPerEm;
        loadedFonts[name].lineGap = face.Height / (float)unitsPerEm - loadedFonts[name].height;

        Console.WriteLine($"face.Ascender={face.Ascender} face.Descender={face.Descender} face.Height={face.Height} UnitsPerEM={face.UnitsPerEM}");

        for (int i = 0; i < preload.Length; i += 10)
        {
            int _length = i + 10 > preload.Length ? preload.Length - i : 10;

            loadedFonts[name].StartRecording(_length);


            for (int j = i; j < _length + i; j++)
            {
                var _character = preload[j];
                face.LoadChar(_character, LoadFlags.NoScale | LoadFlags.NoBitmap, LoadTarget.Normal);
                // Console.WriteLine("Info for char |" + _character + "|");
                // Console.WriteLine(" >Width: " + face.Glyph.Metrics.Width);
                // Console.WriteLine(" >Bearing X: " + face.Glyph.Metrics.HorizontalBearingX);
                // Console.WriteLine(" >Bearing Y: " + face.Glyph.Metrics.HorizontalBearingY);
                // Console.WriteLine(" >Advance: " + face.Glyph.Advance.X);

                if (face.Glyph.Metrics.Width != 0)
                {
                    var _characterShape = BuildShape(face, _character);



                    Console.WriteLine(_characterShape.Contours.Count);

                    var _isValid = _characterShape.Validate();
                    if (!_isValid)
                        throw new Exception("Not a shape for " + _character);
                    _characterShape.Normalize();
                    MSDF.EdgeColoringSimple(_characterShape, Math.PI / 3.0);
                    var _pixmap = new Pixmap<Color3>(glyphSize, glyphSize);

                    float glyphWidth = face.Glyph.Metrics.Width.Value;
                    float glyphHeight = face.Glyph.Metrics.Height.Value;
                    float bearingX = face.Glyph.Metrics.HorizontalBearingX.Value;
                    float bearingY = face.Glyph.Metrics.HorizontalBearingY.Value;
                    double _range = (FontAtlas.RANGE / (double)(glyphSize - padding * 2)) * glyphWidth;

                    var _scale = new Vector2(
                        (glyphSize - padding * 2) / glyphWidth,
                        (glyphSize - padding * 2) / glyphHeight
                    );

                    // Shift so the glyph bottom-left maps to (padding, padding)
                    var _translate = new Vector2(
                        padding,           // X: bearingX=0 so no shift needed
                        padding            // Y: with InverseYAxis, row is flipped internally
                    );

                    Console.WriteLine($"glyphWidth={glyphWidth} glyphHeight={glyphHeight}");
                    Console.WriteLine($"bearingX={bearingX} bearingY={bearingY}");
                    Console.WriteLine($"scale={_scale} translate={_translate}");
                    Console.WriteLine($"range={_range}");

                    foreach (var contour in _characterShape.Contours)
                    {
                        foreach (var edge in contour.Edges)
                        {
                            Console.WriteLine($"  Edge: {edge.GetType().Name} " +
                                $"p0={edge.GetPoint(0)} p1={edge.GetPoint(1)}");
                        }
                    }
                    MSDF.GenerateMSDF(_pixmap, _characterShape, _range, _scale, _translate);

                    byte[] _pixels = new byte[glyphSize * glyphSize * 4];
                    for (int k = 0; k < glyphSize * glyphSize; k++)
                    {
                        var c = _pixmap[k % glyphSize, k / glyphSize];
                        _pixels[k * 4 + 0] = (byte)Math.Clamp(c.R * 255f, 0, 255);
                        _pixels[k * 4 + 1] = (byte)Math.Clamp(c.G * 255f, 0, 255);
                        _pixels[k * 4 + 2] = (byte)Math.Clamp(c.B * 255f, 0, 255);
                        _pixels[k * 4 + 3] = 255;
                    }

                    GlyphData glyphData = new()
                    {
                        Advance = face.Glyph.Metrics.HorizontalAdvance.Value / unitsPerEm,
                        BearingX = face.Glyph.Metrics.HorizontalBearingX.Value / unitsPerEm,
                        BearingY = face.Glyph.Metrics.HorizontalBearingY.Value / unitsPerEm,
                        Width = face.Glyph.Metrics.Width.Value / unitsPerEm,
                        Height = face.Glyph.Metrics.Height.Value / unitsPerEm,
                    };


                    loadedFonts[name].AddGlyph(_character, _pixels, ref glyphData);
                    Console.WriteLine($"Glyph '{_character}': W={glyphData.Width} H={glyphData.Height} Advance={glyphData.Advance} BearingX={glyphData.BearingX} BearingY={glyphData.BearingY}");
                    Console.WriteLine($"  UVMin={glyphData.UVMin} UVMax={glyphData.UVMax}");
                    loadedFonts[name].Glyphs[_character] = glyphData;

                }
                else
                {
                    GlyphData _glyphData = new()
                    {
                        Advance = face.Glyph.Metrics.HorizontalAdvance.Value / unitsPerEm,
                        BearingX = face.Glyph.Metrics.HorizontalBearingX.Value / unitsPerEm,
                        BearingY = face.Glyph.Metrics.HorizontalBearingY.Value / unitsPerEm,
                    };
                    loadedFonts[name].Glyphs[_character] = _glyphData;
                }
            }

            loadedFonts[name].EndRecording();
        }


    }

    public FontAtlas GetFontAtlas(string path) => loadedFonts[path];

    Shape BuildShape(Face face, char c)
    {
        var outline = face.Glyph.Outline;
        var shape = new Shape();

        int contourStart = 0;

        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);

        for (int i = 0; i < outline.Points.Length; i++)
        {
            var p = outline.Points[i];

            min.X = Math.Min(min.X, (float)p.X.Value);
            min.Y = Math.Min(min.Y, (float)p.Y.Value);

            max.X = Math.Max(max.X, (float)p.X.Value);
            max.Y = Math.Max(max.Y, (float)p.Y.Value);
        }

        Vector2 size = max - min;
        Console.WriteLine(size);

        for (int i = 0; i < outline.Contours.Length; i++)
        {
            int contourEnd = outline.Contours[i];

            Contour contour = new();

            //? Iterating throught points

            int shapeStarts = contourStart;
            Console.WriteLine("Contour starts:");
            for (int j = contourStart; j <= contourEnd; j++)
            {
                Console.WriteLine("J: " + j + $" tag={Convert.ToString(outline.Tags[j], 2).PadLeft(8, '0')}");
                if ((outline.Tags[j] & 0b00000001) == 0)
                {
                    Console.WriteLine($"Skipping {j} of the curve");
                    continue;
                }

                if (j == shapeStarts)
                {
                    Console.WriteLine($"Skipping {j} it is start and the end.");
                    continue;
                }

                int shapeEnds = j;

                Vector2 _startPoint = new Vector2(outline.Points[shapeStarts].X.Value, outline.Points[shapeStarts].Y.Value);
                Vector2 _endPoint = new Vector2(outline.Points[shapeEnds].X.Value, outline.Points[shapeEnds].Y.Value);

                switch (shapeEnds - shapeStarts)
                {
                    case 1:
                        Console.WriteLine("From " + shapeStarts + " to " + shapeEnds + " is linear");
                        contour.Edges.Add(new LinearSegment(_startPoint, _endPoint, EdgeColor.White));
                        break;
                    case 2:
                        Console.WriteLine("From " + shapeStarts + " to " + shapeEnds + " is quadratic");
                        Vector2 _controlPoint = new Vector2(outline.Points[shapeStarts + 1].X.Value, outline.Points[shapeStarts + 1].Y.Value);
                        contour.Edges.Add(new QuadraticSegment(_startPoint, _controlPoint, _endPoint, EdgeColor.White));
                        break;
                    case 3:
                        Console.WriteLine("From " + shapeStarts + " to " + shapeEnds + " is cubic");
                        _controlPoint = new Vector2(outline.Points[shapeStarts + 1].X.Value, outline.Points[shapeStarts + 1].Y.Value);
                        Vector2 _controlPoint2 = new Vector2(outline.Points[shapeStarts + 2].X.Value, outline.Points[shapeStarts + 2].Y.Value);
                        contour.Edges.Add(new CubicSegment(_startPoint, _controlPoint, _controlPoint2, _endPoint, EdgeColor.White));
                        break;
                    default:
                        throw new Exception("Wrong contour for char: " + c);
                }

                shapeStarts = shapeEnds;
            }
            Vector2 _startPoint2 = new Vector2(outline.Points[contourEnd].X.Value, outline.Points[contourEnd].Y.Value);
            Vector2 _endPoint2 = new Vector2(outline.Points[contourStart].X.Value, outline.Points[contourStart].Y.Value);
            contour.Edges.Add(new LinearSegment(_startPoint2, _endPoint2, EdgeColor.White));

            Console.WriteLine("Contour ends:");
            contourStart = contourEnd + 1;

            shape.Contours.Add(contour);
        }

        shape.InverseYAxis = true; // FreeType Y is up, screen Y is down
        return shape;
    }

    public void Dispose()
    {
        foreach (var item in loadedFonts)
        {
            Console.WriteLine(item.Key + ": ");
            item.Value.Dispose();
        }
    }
}