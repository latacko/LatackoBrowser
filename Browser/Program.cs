using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using BenchmarkDotNet.Running;
using Browser;
using HTMLParser;
using ObjectCore;
using Renderer;
using Silk.NET.Maths;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using TextCore;

// HttpClient httpClient = new();

// var content = await httpClient.GetAsync("http://subs.latacko.pl");
// var html = await content.Content.ReadAsStringAsync();
// var context = BrowsingContext.New(Configuration.Default);
// var document = await context.OpenAsync(req => req.Content(html));
// // var elements = doc.Descendants("div");
// // XmlDocument xmlDoc= new();
// // xmlDoc.LoadXml(html);

// // xmlDoc.GetElementsByTagName("");
// Console.WriteLine(document.ActiveElement);
// foreach (var item in document.Children)
// {
//     Console.WriteLine(item.NodeName);
// }
// // Console.WriteLine(elements);

// Console.WriteLine(AppContext.BaseDirectory);
// const int iterations = 1000;

// var context = BrowsingContext.New(Configuration.Default);
// Stopwatch stopwatch = new();
// stopwatch.Start();
// for (int i = 0; i < iterations; i++)
// {
//     await context.OpenAsync(req => req.Content(_document));
// }
// stopwatch.Stop();
// double anglesharpPerformance = stopwatch.Elapsed.TotalMilliseconds / iterations;
// Console.WriteLine("Time of AngleSharp parsing: " + anglesharpPerformance + "ms");


// stopwatch.Restart();

// for (int i = 0; i < iterations; i++)
// {
//     HTMLParser.Document.Parse(_document);
// }

// stopwatch.Stop();


// double myPerformance = stopwatch.Elapsed.TotalMilliseconds / iterations;
// Console.WriteLine("Time of my parsing: " + (stopwatch.Elapsed.TotalMilliseconds / iterations) + "ms");
// Console.WriteLine("My parser is " + anglesharpPerformance/myPerformance + "x faster");

// #if RELEASE
// // BenchmarkRunner.Run<ParserBenchmark>();
// BenchmarkRunner.Run<BenchmarkToXDoc>();
// return;
// #endif

// string _document = File.ReadAllText(AppContext.BaseDirectory+"../"+"../"+"../"+"../"+"wiki.html");

// Environment.SetEnvironmentVariable("socket","x11");
SiteRenderer mainBrowser = new(1920, 1080);
BrowserWindow browserWindow = new(mainBrowser);

RenderEngine renderEngine = new([KhrSwapchain.ExtensionName], browserWindow.khrSurface, browserWindow.surface);
renderEngine.RegisterShader(new ObjectShader());
renderEngine.RegisterShader(new TextShader());
unsafe
{
    renderEngine.Init(browserWindow.window.VkSurface.GetRequiredExtensions(out uint count), count);
}

browserWindow.Run("Browser");


// SvgLoader.Loader.Parse(_document);

// SvgLoader.Loader.Parse(@"
// <svg  viewBox=""0 0 640 640"" html>
//     <!--!Font Awesome Free v7.2.0 by @fontawesome - https://fontawesome.com License - https://fontawesome.com/license/free Copyright 2026 Fonticons, Inc.-->
//     <path d=""M39 39C48.4 29.6 63.6 29.6 72.9 39L601 567C610.4 576.4 610.4 591.6 601 600.9C591.6 610.2 576.4 610.3 567.1 600.9L39 73C29.7 63.6 29.7 48.4 39 39z"" a/>
// </svg>");

