
using System.Diagnostics;
using System.Numerics;
using Remora.MSDFGen;
using Remora.MSDFGen.Graphics;
using SharpFont;
using Silk.NET.Vulkan;

namespace TextCore;

public class FontManager : IDisposable
{
    public static FontManager Instance;
    Dictionary<string, FontAtlas> loadedFonts = new();
    static Library library = new();
    const string preload = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?:;-–()[]{}'\"/\\@#";

    public FontManager()
    {
        Instance = this;
    }
    
    public void LoadFont(string path)
    {
        if (loadedFonts.ContainsKey(path)) return;
        loadedFonts.Add(path, new());
        var face = new Face(library, path);

        uint renderSize = 64 * 4;
        face.SetPixelSizes(0, renderSize);

        int glyphSize = FontAtlas.GLYPH_SIZE;
        int padding = FontAtlas.PADDING;
        double _range = FontAtlas.RANGE;

        loadedFonts[path].Create();
        Fence fence;
        loadedFonts[path].StartRecording(out fence, preload.Length);

        float ascender = face.Size.Metrics.Ascender.ToSingle();
        float descender = face.Size.Metrics.Descender.ToSingle();
        float lineHeight = face.Size.Metrics.Height.ToSingle();

        loadedFonts[path].height = ascender + Math.Abs(descender);
        loadedFonts[path].lineGap = lineHeight - loadedFonts[path].height;

        foreach (var character in preload)
        {
            face.LoadChar(character, LoadFlags.NoScale | LoadFlags.NoBitmap, LoadTarget.Normal);
            Console.WriteLine("Info for char |" + character + "|");
            Console.WriteLine(" >Width: " + face.Glyph.Metrics.Width);
            Console.WriteLine(" >Bearing X: " + face.Glyph.Metrics.HorizontalBearingX);
            Console.WriteLine(" >Bearing Y: " + face.Glyph.Metrics.HorizontalBearingY);
            Console.WriteLine(" >Advance: " + face.Glyph.Advance.X);

            if (face.Glyph.Metrics.Width != 0)
            {
                var _characterShape = BuildShape(face, character);

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
                for (int i = 0; i < glyphSize * glyphSize; i++)
                {
                    var c = _pixmap[i % glyphSize, i / glyphSize];
                    _pixels[i * 4 + 0] = c.R;
                    _pixels[i * 4 + 1] = c.G;
                    _pixels[i * 4 + 2] = c.B;
                    _pixels[i * 4 + 3] = 255;
                }

                GlyphData glyphData = new()
                {
                    Advance = face.Glyph.Advance.X.ToSingle(),
                    BearingX = face.Glyph.Metrics.HorizontalBearingX.ToSingle(),
                    BearingY = face.Glyph.Metrics.HorizontalBearingY.ToSingle(),
                    Width = glyphSize - padding * 2,
                    Height = glyphSize - padding * 2,
                };

                loadedFonts[path].AddGlyph(character, _pixels, ref glyphData);
            }
            else
            {
                GlyphData _glyphData = new()
                {
                    Advance = face.Glyph.Advance.X.ToSingle(),
                    BearingX = face.Glyph.Metrics.HorizontalBearingX.ToSingle(),
                    BearingY = face.Glyph.Metrics.HorizontalBearingY.ToSingle(),
                };
                loadedFonts[path].Glyphs[character] = _glyphData;
            }
        }
        loadedFonts[path].EndRecording(fence);
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
                // FreeType uses 26.6 fixed point — divide by 64
                points.Add(new Vector2((float)p.X / 64f, (float)p.Y / 64f));
                tags.Add(outline.Tags[j]);
            }

            int k = 0;
            while (k < pointCount)
            {
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