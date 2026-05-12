using System.Xml.Linq;
using AngleSharp;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using HTMLParser;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, warmupCount: 0, iterationCount: 2000)]
// [EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class BenchmarkToXDoc
{
    private string[] _inputs = null!;
    private string[] _inputs2 = null!;
    private int _index = 0;

    [GlobalSetup]
    public void Setup()
    {
        _inputs = Enumerable.Range(0, 2000)
            .Select(_ => @"
            <svg  viewBox=""0 0 640 640"">
                <!--!Font Awesome Free v7.2.0 by @fontawesome - https://fontawesome.com License - https://fontawesome.com/license/free Copyright 2026 Fonticons, Inc.-->
                <path d=""M39 39C48.4 29.6 63.6 29.6 72.9 39L601 567C610.4 576.4 610.4 591.6 601 600.9C591.6 610.2 576.4 610.3 567.1 600.9L39 73C29.7 63.6 29.7 48.4 39 39z""/>
            </svg>" + "")
            .ToArray();
        _inputs2 = Enumerable.Range(0, 2000)
.Select(_ => @"
            <svg  viewBox=""0 0 640 640"">
                <!--!Font Awesome Free v7.2.0 by @fontawesome - https://fontawesome.com License - https://fontawesome.com/license/free Copyright 2026 Fonticons, Inc.-->
                <path d=""M39 39C48.4 29.6 63.6 29.6 72.9 39L601 567C610.4 576.4 610.4 591.6 601 600.9C591.6 610.2 576.4 610.3 567.1 600.9L39 73C29.7 63.6 29.7 48.4 39 39z""/>
            </svg>" + "")
.ToArray();
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

    [Benchmark]
    public object XDocumentParser()
    {
        return XDocument.Parse(_inputs2[_index++ % _inputs2.Length]);
    }
}