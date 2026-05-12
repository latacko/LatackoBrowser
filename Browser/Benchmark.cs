using AngleSharp;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using HTMLParser;

[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class ParserBenchmark
{
    string _document = "";
    IBrowsingContext context;

    [GlobalSetup]
    public void Setup()
    {
        _document = File.ReadAllText(AppContext.BaseDirectory + "../" + "../" + "../" + "../" + "wiki.html"); // BDN sets working dir correctly
        context = BrowsingContext.New(Configuration.Default);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        context.Dispose();
    }

    // [Benchmark]
    // public void Parse100()
    // {
    //     for (int i = 0; i < 100; i++)
    //         Document.Parse(_document, false);
    // }

    [Benchmark]
    public void Parse1()
    {
        Document.Parse(_document, false);
    }

}