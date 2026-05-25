
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
    const string preload = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?:;-–()[]{}'\"/\\@#";


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
        face.SetPixelSizes(0, renderSize);

        int glyphSize = FontAtlas.GLYPH_SIZE;
        int padding = FontAtlas.PADDING;
        double _range = FontAtlas.RANGE;

        loadedFonts[name].Create(lastId++);

        float ascender = face.Size.Metrics.Ascender.ToSingle();
        float descender = face.Size.Metrics.Descender.ToSingle();
        float lineHeight = face.Size.Metrics.Height.ToSingle();

        loadedFonts[name].height = ascender + Math.Abs(descender);
        loadedFonts[name].lineGap = lineHeight - loadedFonts[name].height;


        for (int i = 0; i < preload.Length; i += 10)
        {
            int _length = i + 10 > preload.Length ? preload.Length - i : 10;

            loadedFonts[name].StartRecording(_length);


            for (int j = i; j < _length + i; j++)
            {
                var _character = preload[j];
                face.LoadChar(_character, LoadFlags.NoBitmap, LoadTarget.Normal);
                // Console.WriteLine("Info for char |" + _character + "|");
                // Console.WriteLine(" >Width: " + face.Glyph.Metrics.Width);
                // Console.WriteLine(" >Bearing X: " + face.Glyph.Metrics.HorizontalBearingX);
                // Console.WriteLine(" >Bearing Y: " + face.Glyph.Metrics.HorizontalBearingY);
                // Console.WriteLine(" >Advance: " + face.Glyph.Advance.X);

                if (face.Glyph.Metrics.Width != 0)
                {
                    var _characterShape = BuildShape(face, _character);

                    _characterShape.Normalize();
                    MSDF.EdgeColoringSimple(_characterShape, Math.PI / 3.0);
                    var _pixmap = new Pixmap<Color3b>(glyphSize, glyphSize);

                    var _scale = new Vector2(
                        glyphSize / ((float)face.Glyph.Metrics.Width / 64f),
                        glyphSize / ((float)face.Glyph.Metrics.Height / 64f)
                    );
                    var _translate = new Vector2(padding, padding);
                    MSDF.GenerateMSDF(_pixmap, _characterShape, _range, _scale, _translate);

                    byte[] _pixels = new byte[glyphSize * glyphSize * 4];
                    for (int k = 0; k < glyphSize * glyphSize; k++)
                    {
                        var c = _pixmap[k % glyphSize, k / glyphSize];
                        _pixels[k * 4 + 0] = c.R;
                        _pixels[k * 4 + 1] = c.G;
                        _pixels[k * 4 + 2] = c.B;
                        _pixels[k * 4 + 3] = 255;
                    }

                    GlyphData glyphData = new()
                    {
                        Advance = face.Glyph.Advance.X.ToSingle(),
                        BearingX = face.Glyph.Metrics.HorizontalBearingX.ToSingle(),
                        BearingY = face.Glyph.Metrics.HorizontalBearingY.ToSingle(),
                        Width = glyphSize - padding * 2,
                        Height = glyphSize - padding * 2,
                    };

                    loadedFonts[name].AddGlyph(_character, _pixels, ref glyphData);
                    loadedFonts[name].Glyphs[_character] = glyphData;

                }
                else
                {
                    GlyphData _glyphData = new()
                    {
                        Advance = face.Glyph.Advance.X.ToSingle(),
                        BearingX = face.Glyph.Metrics.HorizontalBearingX.ToSingle(),
                        BearingY = face.Glyph.Metrics.HorizontalBearingY.ToSingle(),
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
        var points = new List<Vector2>();
        var tags = new List<byte>();
        for (int i = 0; i < outline.Contours.Length; i++)
        {
            var contour = new Contour();
            int contourEnd = outline.Contours[i];

            int pointCount = contourEnd - contourStart + 1;
            points.Clear();
            tags.Clear();

            for (int j = contourStart; j <= contourEnd; j++)
            {
                var p = outline.Points[j];
                Console.WriteLine($"Point {j}: ({p.X}, {p.Y}) tag={outline.Tags[j]}");
                // FreeType uses 26.6 fixed point — divide by 64
                points.Add(new Vector2((float)p.X / 64f, (float)p.Y / 64f));
                tags.Add(outline.Tags[j]);
            }

            int _safetyCounter = 0;

            int k = 0;
            while (k < pointCount)
            {

                _safetyCounter++;
                if (_safetyCounter > pointCount * 3)
                {
                    Console.WriteLine($"Infinite loop detected at k={k} pointCount={pointCount}");
                    break;
                }
                var curr = points[k];
                byte tag = tags[k];

                if (tag == 1) // on-curve point
                {
                    // next point
                    int next = (k + 1) % pointCount;
                    byte nextTag = tags[next];

                    if (nextTag == 1) // next is also on-curve → linear edge
                    {
                        contour.Edges.Add(new LinearSegment(curr, points[next], EdgeColor.White));
                        k++;
                    }
                    else if (nextTag == 0) // next is quadratic control point
                    {
                        int next2 = (k + 2) % pointCount;
                        // if next2 is also off-curve, implied on-curve point between them
                        Vector2 endPoint = tags[next2] == 0
                            ? (points[next] + points[next2]) / 2f
                            : points[next2];

                        contour.Edges.Add(new QuadraticSegment(curr, points[next], endPoint, EdgeColor.White));
                        k += tags[next2] == 0 ? 2 : 2;
                    }
                    else if (nextTag == 2) // cubic control point
                    {
                        int next2 = (k + 2) % pointCount;
                        int next3 = (k + 3) % pointCount;
                        contour.Edges.Add(new CubicSegment(curr, points[next], points[next2], points[next3], EdgeColor.White));
                        k += 3;
                    }
                }
                else
                {
                    k++; // skip — handled by previous iteration
                }
            }

            shape.Contours.Add(contour);
            contourStart = contourEnd + 1;
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