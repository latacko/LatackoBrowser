using System.Diagnostics;
using System.Runtime.InteropServices;
using Msdfgen;
using SharpFont;
using Silk.NET.Vulkan;
using TextCore.Fonts;

namespace TextCore;

public class FontManager : IDisposable
{
    public static FontManager Instance;
    uint lastId = 0;
    Dictionary<string, FontAtlas> loadedFonts = new();
    static Library library = new();
    // const string preload = "A";
    // const string preload = "WITAJ DME";
    // const string preload = "TAKSI CZUJE";
    const string preload = "ABCDEFGHIJKLMNOPRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?:;-–()[]{}'\"/\\@#";
    

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
        testContour.Edges.Add(new LinearSegment(new Vector2(10, 10), new Vector2(90, 10), EdgeColor.WHITE));
        testContour.Edges.Add(new LinearSegment(new Vector2(90, 10), new Vector2(50, 90), EdgeColor.WHITE));
        testContour.Edges.Add(new LinearSegment(new Vector2(50, 90), new Vector2(10, 10), EdgeColor.WHITE));
        testShape.Contours.Add(testContour);
        testShape.SetYAxisOrientation(YAxisOrientation.Downward);

        var testPixmap = new Bitmap<float>(100, 100);
        // Console.WriteLine("Test1");
        MsdfGenerator.GenerateMSDF(testPixmap, testShape, 4.0, new Vector2(1, 1), new Vector2(0, 0));
        int testNonBlack = 0;
        for (int k = 0; k < 100 * 100; k++)
        {
            var c = testPixmap[k % 100, k / 100];
            if (c.R != 0 || c.G != 0 || c.B != 0) testNonBlack++;
        }
        // Console.WriteLine($"Test shape non-black: {testNonBlack}");
    }

    public void LoadFont(string name, string charset = preload)
    {
        string path = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            path = "/usr/share/fonts/";
        }

        path += name;

        // Console.WriteLine(path);
        var face = new Face(library, path);

        uint renderSize = 64 * 4;
        face.SetCharSize(0, 64 * 64, 72, 72);
        float unitsPerEm = face.UnitsPerEM;

        if (!loadedFonts.ContainsKey(name))
        {
            loadedFonts.Add(name, new(lastId++, name));

            loadedFonts[name].height = (face.Ascender - face.Descender) / (float)unitsPerEm;
            loadedFonts[name].lineGap = face.Height / (float)unitsPerEm - loadedFonts[name].height;
            loadedFonts[name].baseline = face.Ascender / (float)face.UnitsPerEM;
        }

        Stopwatch stopwatch = new();
        stopwatch.Start();

        var threadLocalFace = new ThreadLocal<Face>(() =>
        {
            var f = new Face(library, path);
            f.SetCharSize(0, 64 * 64, 72, 72);
            return f;
        }, trackAllValues: true);

        bool insideOut = name.EndsWith(".otf");

        for (int i = 0; i < charset.Length; i += 10)
        {
            int _length = i + 10 > charset.Length ? charset.Length - i : 10;

            loadedFonts[name].StartRecording(_length);


            Parallel.For(i, _length + i, j =>
            {
                var faceForThread = threadLocalFace.Value;
                var _character = charset[j];
                faceForThread.LoadChar(_character, LoadFlags.NoScale | LoadFlags.NoBitmap, LoadTarget.Normal);

                // Console.WriteLine("Info for char |" + _character + "|");
                // Console.WriteLine(" >Width: " + face.Glyph.Metrics.Width);
                // Console.WriteLine(" >Bearing X: " + face.Glyph.Metrics.HorizontalBearingX);
                // Console.WriteLine(" >Bearing Y: " + face.Glyph.Metrics.HorizontalBearingY);
                // Console.WriteLine(" >Advance: " + face.Glyph.Advance.X);

                if (faceForThread.Glyph.Metrics.Width != 0)
                {
                    float glyphHeight = faceForThread.Glyph.Metrics.Height.Value;
                    float bearingX = faceForThread.Glyph.Metrics.HorizontalBearingX.Value;
                    float bearingY = faceForThread.Glyph.Metrics.HorizontalBearingY.Value;
                    float glyphWidth = faceForThread.Glyph.Metrics.Width.Value;
                    float AdvanceX = faceForThread.Glyph.Advance.X.Value;
                    float fromTop = glyphHeight - bearingY;
                    float charHeight = (faceForThread.BBox.Top - faceForThread.BBox.Bottom) / unitsPerEm;

                    var _characterShape = BuildShape(faceForThread, _character, bearingX, fromTop, insideOut);

                    var _isValid = _characterShape.Validate();
                    if (!_isValid)
                        throw new Exception("Not a shape for " + _character);

                    _characterShape.Normalize();
                    MSDF
                    MSDF.EdgeColoringSimple(_characterShape, Math.PI / 3.0);

                    double _left = 0;
                    double _right = 0;
                    double _top = 0;
                    double _bottom = 0;
                    _characterShape.GetBounds(ref _left, ref _bottom, ref _right, ref _top);

                    byte[][] _mipmapPixels = new byte[FontAtlas.MIP_LAYERS][];


                    float _occupiedWidthPx = 0;
                    float _occupiedHeightPx = 0;

                    for (int miplevel = 0; miplevel < FontAtlas.MIP_LAYERS; miplevel++)
                    {
                        var _glyphSize = (int)FontAtlas.MSDFMipLevelsData[miplevel].GlyphSize;
                        var _padding = FontAtlas.MSDFMipLevelsData[miplevel].Padding;

                        var innerSize = _glyphSize - _padding * 2;

                        float _scaleByWidth = innerSize / glyphWidth;
                        float _scaleByHeight = innerSize / glyphHeight;
                        float _uniformScale = Math.Min(_scaleByHeight, _scaleByWidth);

                        if (miplevel == 0)
                        {
                            _occupiedWidthPx = Math.Min(glyphWidth * _uniformScale + 1, innerSize);
                            _occupiedHeightPx = Math.Min(glyphHeight * _uniformScale + 1, innerSize);
                        }

                        var _scale = new Vector2(_uniformScale, _uniformScale);
                        double _range = FontAtlas.MSDFMipLevelsData[miplevel].Range / _uniformScale;

                        var _pixmap = new Pixmap<Color3>(_glyphSize, _glyphSize);
                        var _translate = new Vector2(
                            _padding,           // X: bearingX=0 so no shift needed
                            _padding + innerSize - glyphHeight * _uniformScale            // Y: with InverseYAxis, row is flipped internally
                        );

                        MSDF.GenerateMSDF(_pixmap, _characterShape, _range, _scale, _translate);
                        // SDF.GenerateSDF()
                        byte[] _pixels = new byte[_glyphSize * _glyphSize * 4];
                        for (int k = 0; k < _glyphSize * _glyphSize; k++)
                        {
                            var c = _pixmap[k % _glyphSize, k / _glyphSize];
                            _pixels[k * 4 + 0] = (byte)Math.Clamp(c.R * 255f, 0, 255);
                            _pixels[k * 4 + 1] = (byte)Math.Clamp(c.G * 255f, 0, 255);
                            _pixels[k * 4 + 2] = (byte)Math.Clamp(c.B * 255f, 0, 255);
                            _pixels[k * 4 + 3] = 255;
                        }

                        _mipmapPixels[miplevel] = _pixels;
                    }


                    GlyphData _glyphData = new()
                    {
                        Advance = AdvanceX / unitsPerEm,
                        BearingX = bearingX / unitsPerEm,
                        BearingY = bearingY / unitsPerEm,
                        // Width = face.Glyph.Metrics.Width.Value / unitsPerEm,
                        Width = (float)_right / unitsPerEm,
                        Height = (float)_top / unitsPerEm,
                        OccupiedWidthPx = _occupiedWidthPx,
                        OccupiedHeightPx = _occupiedHeightPx,
                    };

                    // Console.WriteLine("Char: " + _character + " glyph: " + _glyphData);


                    lock (loadedFonts[name])
                    {
                        loadedFonts[name].AddGlyph(_character, _mipmapPixels, ref _glyphData);
                        // Console.WriteLine($"Glyph '{_character}': Data= {_glyphData} ");
                        loadedFonts[name].Glyphs[_character] = _glyphData;
                    }
                }
                else
                {
                    GlyphData _glyphData = new()
                    {
                        Advance = faceForThread.Glyph.Metrics.HorizontalAdvance.Value / unitsPerEm,
                        BearingX = faceForThread.Glyph.Metrics.HorizontalBearingX.Value / unitsPerEm,
                        BearingY = faceForThread.Glyph.Metrics.HorizontalBearingY.Value / unitsPerEm,
                    };
                    loadedFonts[name].Glyphs[_character] = _glyphData;
                }
            });

            loadedFonts[name].EndRecording();
        }

        foreach (var f in threadLocalFace.Values) f.Dispose();
        stopwatch.Stop();

        // Console.WriteLine("czciąke " + name + " załadowałem w " + stopwatch.ElapsedMilliseconds + "ms");
    }

    public FontAtlas GetFontAtlas(string path) => loadedFonts[path];

    record struct PointInfo
    {
        public Vector2 Pos;
        public byte Tag;
    }

    
    public void Dispose()
    {
        library.Dispose();
        foreach (var item in loadedFonts)
        {
            item.Value.Dispose();
        }
    }
}