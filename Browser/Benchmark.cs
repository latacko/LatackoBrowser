using AngleSharp;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using HTMLParser;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, warmupCount: 0, iterationCount: 30)]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class ParserBenchmark
{
    private string[] _inputs = null!;
    private int _index = 0;

    [GlobalSetup]
    public void Setup()
    {
        _inputs = Enumerable.Range(0, 30)
            .Select(_ => File.ReadAllText(AppContext.BaseDirectory + "../" + "../" + "../" + "../" + "wiki.html") + "")
            .ToArray();

        var _content = File.ReadAllText(AppContext.BaseDirectory + "../" + "../" + "../" + "../" + "wiki.html");
        Console.WriteLine($"File size: {_content.Length:N0} chars ({_content.Length * 2:N0} bytes as UTF-16)");
        Console.WriteLine($"Single parse: {MeasureAlloc(() => new HTMLParser.DocumentParser(_content, false)):N0} bytes");
        Console.WriteLine($"Ratio: {MeasureAlloc(() => new HTMLParser.DocumentParser(_content, false)) / (double)(_content.Length * 2):F1}x file size");
    }

    static long MeasureAlloc(Action a)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        a();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
    }

    // [Benchmark]
    // public void Parse100()
    // {
    //     for (int i = 0; i < 100; i++)
    //         Document.Parse(_document, false);
    // }

    [Benchmark]
    public object MyParser()
    {
        return new HTMLParser.DocumentParser(_inputs[_index++ % _inputs.Length], false);
    }

}