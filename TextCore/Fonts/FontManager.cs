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
    const string preload = "AMIE WTJDa";
    // const string preload = "AĄBCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?:;-–()[]{}'\"/\\@#";


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
        // Console.WriteLine("Test1");
        MSDF.GenerateMSDF(testPixmap, testShape, 4.0, new Vector2(1, 1), new Vector2(0, 0));
        // Console.WriteLine("Test2");

        int testNonBlack = 0;
        for (int k = 0; k < 100 * 100; k++)
        {
            var c = testPixmap[k % 100, k / 100];
            if (c.R != 0 || c.G != 0 || c.B != 0) testNonBlack++;
        }
        // Console.WriteLine($"Test shape non-black: {testNonBlack}");
    }

    public void LoadFont(string name)
    {
        string path = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            path = "/usr/share/fonts/";
        }

        path += name;
        if (loadedFonts.ContainsKey(name)) return;
        loadedFonts.Add(name, new());
        Console.WriteLine(path);
        var face = new Face(library, path);

        uint renderSize = 64 * 4;
        face.SetCharSize(0, 64 * 64, 72, 72);
        int glyphSize = FontAtlas.GLYPH_SIZE;

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
                    float glyphHeight = face.Glyph.Metrics.Height.Value;
                    float bearingX = face.Glyph.Metrics.HorizontalBearingX.Value;
                    float bearingY = face.Glyph.Metrics.HorizontalBearingY.Value;
                    float fromTop = glyphHeight - bearingY;
                    var _characterShape = BuildShape(face, _character, bearingX, fromTop);



                    // Console.WriteLine(_characterShape.Contours.Count);

                    var _isValid = _characterShape.Validate();
                    if (!_isValid)
                        throw new Exception("Not a shape for " + _character);
                    _characterShape.Normalize();
                    MSDF.EdgeColoringSimple(_characterShape, Math.PI / 3.0);
                    var _pixmap = new Pixmap<Color3>(glyphSize, glyphSize);

                    float glyphWidth = face.Glyph.Metrics.Width.Value;
                    float AdvanceX = face.Glyph.Advance.X.Value;

                    var innerSize = glyphSize - FontAtlas.PADDING * 2;
                    double _range = FontAtlas.RANGE * ((double)glyphWidth / innerSize);
                    // Console.WriteLine(_range + "range");
                    // var _scale = new Vector2(
                    //     innerSize / glyphWidth,
                    //     innerSize / glyphHeight
                    // );

                    // // Shift so the glyph bottom-left maps to (padding, padding)
                    var _translate = new Vector2(
                        FontAtlas.PADDING,           // X: bearingX=0 so no shift needed
                        FontAtlas.PADDING            // Y: with InverseYAxis, row is flipped internally
                    );


                    // float pad = FontAtlas.PADDING;

                    // give MSDF actual breathing room

                    var _scale = new Vector2(
                        innerSize / glyphWidth,
                        innerSize / glyphHeight
                    );

                    // Console.WriteLine("Sca;e " + _scale);
                    double _left = 0;
                    double _right = 0;
                    double _top = 0;
                    double _bottom = 0;
                    _characterShape.GetBounds(ref _left, ref _bottom, ref _right, ref _top);

                    // Console.WriteLine("For char: " + _character + $" bounding box is: MIN({_left}, {_top}) MAX({_right},{_bottom})");
                    // double centerX = (_left + _right) * 0.5;
                    // double centerY = (_top + _bottom) * 0.5;
                    // var boundsCenter = new Vector2((float)centerX, (float)centerY) * _scale;
                    // var cellCenter = new Vector2(glyphSize, glyphSize) * new Vector2(0.5f, 0.5f);

                    // var _translate = cellCenter - boundsCenter;
                    // var _translate = new Vector2(
                    //     0,
                    //     0
                    // );

                    // double _range = FontAtlas.RANGE * ((double)glyphWidth / inner);
                    // double _range = FontAtlas.RANGE;

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
                        // Width = face.Glyph.Metrics.Width.Value / unitsPerEm,
                        Width = (float)_right / unitsPerEm,
                        Height = face.Glyph.Metrics.Height.Value / unitsPerEm,
                    };


                    loadedFonts[name].AddGlyph(_character, _pixels, ref glyphData);
                    // Console.WriteLine($"Glyph '{_character}': Data= {glyphData} ");
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

    record struct PointInfo
    {
        public Vector2 Pos;
        public byte Tag;
    }

    //For char: A bounding box is: MIN(0, 0) MAX(1296,-1468)
    //For char: A bounding box is: MIN(0, 1468) MAX(1296,0)
    //GlyphData { UVMin = <0,005859375  0,005859375>, UVMax = <0,041015625  0,041015625>, BearingX = 0, BearingY = 0,7167969, Advance = 0,6328125, Width = 0,6328125, Height = 0,7167969 }
    Shape BuildShape(Face face, char c, float leftPadding, float topPadding)
    {
        Vector2 GetPointCoords(FTVector pos)
        {
            // return new Vector2(pos.X.Value, pos.Y.Value);
            return new Vector2(pos.X.Value - leftPadding, pos.Y.Value + topPadding);
        }
        var outline = face.Glyph.Outline;
        var shape = new Shape();

        int contourStart = 0;
        List<PointInfo> _points = new();
        for (int i = 0; i < outline.Contours.Length; i++)
        {
            int contourEnd = outline.Contours[i];

            Contour contour = new();

            //? Iterating throught points

            _points.Clear();
            // Console.WriteLine("Contour starts:");
            for (int j = contourStart; j <= contourEnd; j++)
            {
                // Console.WriteLine("Point: " + j + " has pos of " + GetPointCoords(outline.Points[j]) + " tag: " + outline.Tags[j]);
                _points.Add(new()
                {
                    Pos = GetPointCoords(outline.Points[j]),
                    Tag = outline.Tags[j],
                });

            }

            int _outlineStart = 0;
            for (int j = 0; j < _points.Count; j++)
            {
                // Console.WriteLine("J: " + j + $" tag={Convert.ToString(_points[j].Tag, 2).PadLeft(8, '0')}");
                if ((_points[j].Tag & 0b00000001) == 0)
                {
                    if (j + 1 < _points.Count && (_points[j + 1].Tag & 0b00000001) == 0)
                    {
                        // Console.WriteLine("Two consecutives points off the curve. Adding point in the middle");
                        _points.Insert(j + 1, new()
                        {
                            Pos = Vector2.Lerp(_points[j].Pos, _points[j + 1].Pos, 0.5f),
                            Tag = 0b00000001,
                        });
                        // Console.WriteLine(string.Join(", ", _points));
                    }
                    // Console.WriteLine($"Skipping {j} of the curve");
                    continue;
                }

                if (j == 0)
                {
                    // Console.WriteLine($"Skipping {j} it is start and the end.");
                    continue;
                }

                int _outLineEnds = j;

                Vector2 _startPoint = _points[_outlineStart].Pos;
                Vector2 _endPoint = _points[_outLineEnds].Pos;

                // Console.Write("From " + _outlineStart + " to " + _outLineEnds + " is ");
                switch (_outLineEnds - _outlineStart)
                {
                    case 1:
                        // Console.WriteLine(" linear");
                        contour.Edges.Add(new LinearSegment(_startPoint, _endPoint, EdgeColor.White));
                        break;
                    case 2:
                        // Console.WriteLine(" quadratic");
                        Vector2 _controlPoint = _points[_outlineStart + 1].Pos;
                        contour.Edges.Add(new QuadraticSegment(_startPoint, _controlPoint, _endPoint, EdgeColor.White));
                        break;
                    case 3:
                        // Console.WriteLine(" cubic");
                        _controlPoint = _points[_outlineStart + 1].Pos;
                        Vector2 _controlPoint2 = _points[_outlineStart + 2].Pos;
                        contour.Edges.Add(new CubicSegment(_startPoint, _controlPoint, _controlPoint2, _endPoint, EdgeColor.White));
                        break;
                    default:
                        throw new Exception("Wrong contour for char: " + c);
                }

                _outlineStart = _outLineEnds;
            }
            Vector2 _endPoint2 = _points[0].Pos;
            if ((_points[_points.Count - 1].Tag & 0b00000001) == 0)
            {
                Vector2 _startPos = _points[_points.Count - 2].Pos;
                Vector2 _controlPoint = _points[_points.Count - 1].Pos;
                // Console.WriteLine("Closing shape from " + (_points.Count - 2) + " to 0 is quadratic");
                contour.Edges.Add(new QuadraticSegment(_startPos, _controlPoint, _endPoint2, EdgeColor.White));
            }
            else
            {
                // Console.WriteLine("Closing shape from " + (_points.Count - 1) + " to 0 is line");
                Vector2 _startPos = _points[_points.Count - 1].Pos;
                contour.Edges.Add(new LinearSegment(_startPos, _endPoint2, EdgeColor.White));
            }

            // Console.WriteLine("Contour ends:");
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