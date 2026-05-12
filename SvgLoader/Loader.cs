using System.Diagnostics;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SvgLoader;

public static class Loader
{

    // Fist parser: 30187ms
    // Hange to switch 

    public static List<Vector2[]> Parse(string svgContent)
    {
        // const int startup_iteractions = 200000;
        // for (int i = 0; i < startup_iteractions; i++)
        // {
        //     var doc = XDocument.Parse(svgContent);
        // }

        // for (int i = 0; i < startup_iteractions; i++)
        // {
        //     new HTMLParser.DocumentParser(svgContent, false);
        // }

        const int iteractions = 100;
        Stopwatch stopwatch = new();

        stopwatch.Start();
        // for (int i = 0; i < iteractions; i++)
        // {
        //     var doc = XDocument.Parse(svgContent);
        // }
        // stopwatch.Stop();

        Console.WriteLine("Time for XDOcument parser: " + stopwatch.ElapsedMilliseconds + "ms");

        stopwatch.Restart();
        for (int i = 0; i < iteractions; i++)
        {
            new HTMLParser.DocumentParser(svgContent, false);
        }
        stopwatch.Stop();

        Console.WriteLine("Time for my parser: " + stopwatch.ElapsedMilliseconds + "ms");
        return new();
    }
}
