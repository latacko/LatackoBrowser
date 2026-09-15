using System.Diagnostics;
using System.Runtime.InteropServices;
using AtlasGeneratorCore;
using Msdfgen;
using SharpFont;
using Silk.NET.Vulkan;

namespace TextCore;

public class FontsManager : IDisposable
{
    public static FontsManager Instance;
    Dictionary<string, FontManager> loadedFonts = new();
    static Library library = new();
    
    const string preload = "ABCDEFGHIJKLMNOPRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?:;-–()[]{}'\"/\\@#";
    

    public FontsManager()
    {
        Instance = this;
    }

    public void Tick(uint frameInFlight)
    {
        foreach (var item in loadedFonts)
        {
            item.Value.Tick(frameInFlight);
        }
    }

    public void LoadFont(string name, string charset = preload)
    {
        FontManager _fontManager;
        if (!loadedFonts.TryGetValue(name, out _fontManager!))
        {
            _fontManager = new(library, name, TextManager.Instance.RegisterTexture, new Progress<double>(progress =>
            {
                Console.WriteLine("Atlas <"+ name+"> has loaded " + progress + "% of characters");
            }));
            loadedFonts.Add(name, _fontManager);
        }

        Stopwatch stopwatch = new();
        stopwatch.Start();
        _fontManager.LoadCharset(charset);
        stopwatch.Stop();
        Console.WriteLine("czciąke " + name + " załadowałem w " + stopwatch.ElapsedMilliseconds + "ms");
    }

    public FontAtlas GetFontAtlas(string path) => loadedFonts[path].GetFontAtlas();

    public void Dispose()
    {
        library.Dispose();
        foreach (var item in loadedFonts)
        {
            item.Value.Dispose();
        }
    }
}