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