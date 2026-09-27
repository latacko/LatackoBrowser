using System;
using System.Runtime.InteropServices;
using MsdfAtlasGen;
using SharpFont;
using VulkanManager;

namespace AtlasGeneratorCore;

public class FontManager : IDisposable
{
    static uint lastId = 0;

    readonly FontAtlas fontAtlas;
    readonly FontGeometry fontGeometry;
    readonly string path;
    readonly string name;
    readonly Library library;

    public FontManager(Library library, string name, Action<Silk.NET.Vulkan.ImageView, uint> registerTextureCB, IProgress<uint>? progress = null)
    {
        this.name = name;
        this.library = library;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            path = "/usr/share/fonts/";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            path = "C:/Windows/Fonts/";
        }

        path += name;

        fontAtlas = new(lastId++, name, registerTextureCB, progress);

        fontAtlas.SetPixelRange(new(2.0));
        fontAtlas.SetMiterLimit(1.0);
        double emSize = 40;
        fontAtlas.SetScale(emSize);

        fontGeometry = new();
    }

    public FontAtlas GetFontAtlas() => fontAtlas;
    public FontGeometry GetFontGeometry() => fontGeometry;

    public uint LoadCharset(string charset)
    {
        var _face = new Face(library, path);
        var (loaded, toLoadCharacters) = fontGeometry.LoadCharset(_face, 1, charset, true);

        if (toLoadCharacters == null) return 0;

        GlyphGeometry[] _characters = new GlyphGeometry[toLoadCharacters.Length];
        for (int i = 0; i < toLoadCharacters.Length; i++)
        {
            _characters[i] = fontGeometry.GetGlyph(toLoadCharacters[i]);
        }

        _face.Dispose();

        fontAtlas.AddCharacters(_characters);
        return (uint)toLoadCharacters.Length;
    }

    public void Dispose()
    {
        fontAtlas.Dispose();
    }


}
