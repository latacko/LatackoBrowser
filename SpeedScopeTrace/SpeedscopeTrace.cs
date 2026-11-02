using System.Diagnostics;
using System.Text;

public static class SpeedscopeTrace
{
    struct Ev { public bool Open; public int Frame; public long Us; public long Bytes; }

    static readonly long start = Stopwatch.GetTimestamp();
    static readonly Dictionary<string, int> frameIds = new();
    static readonly List<string> frameNames = new();

    [ThreadStatic] static List<Ev>? events;
    static readonly List<(int tid, List<Ev> ev)> all = new();
    static readonly object gate = new();
    public static volatile bool Enabled = true;
    static int FrameId(string name)
    {
        lock (gate)
        {
            if (!frameIds.TryGetValue(name, out int id))
            {
                id = frameNames.Count;
                frameNames.Add(name);
                frameIds[name] = id;
            }
            return id;
        }
    }

    static long Now() => (Stopwatch.GetTimestamp() - start) * 1_000_000 / Stopwatch.Frequency;

    public readonly struct Scope : IDisposable
    {
        readonly int frame;

        public Scope(string name)
        {
            if (!Enabled) { frame = -1; return; }

            if (events == null)
            {
                events = new(1 << 20);   // big preallocation, so list growth isn't counted as garbage
                lock (gate) all.Add((Environment.CurrentManagedThreadId, events));
            }
            frame = FrameId(name);
            // measure last, so the profiler's own work is not attributed to your scope
            events.Add(new Ev
            {
                Open = true,
                Frame = frame,
                Us = Now(),
                Bytes = GC.GetAllocatedBytesForCurrentThread()
            });
        }

        public void Dispose()
        {
            if (frame < 0) return;
            // measure first, for the same reason
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            events!.Add(new Ev { Open = false, Frame = frame, Us = Now(), Bytes = bytes });
        }
    }

    public static Scope Begin(string name) => new(name);

    public static void Save(string path)
    {
        var sb = new StringBuilder();
        sb.Append("{\"$schema\":\"https://www.speedscope.app/file-format-schema.json\",");
        sb.Append("\"shared\":{\"frames\":[");
        lock (gate)
        {
            for (int i = 0; i < frameNames.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"name\":\"").Append(frameNames[i]).Append("\"}");
            }
            sb.Append("]},\"profiles\":[");

            bool first = true;
            foreach (var (tid, ev) in all)
            {
                if (ev.Count == 0) continue;
                WriteProfile(sb, ref first, $"Thread {tid} (time)", "microseconds", ev, useBytes: false);
                WriteProfile(sb, ref first, $"Thread {tid} (allocated bytes)", "bytes", ev, useBytes: true);
            }
        }
        sb.Append("],\"name\":\"trace\",\"activeProfileIndex\":0,\"exporter\":\"custom\"}");
        File.WriteAllText(path, sb.ToString());
    }

    static void WriteProfile(StringBuilder sb, ref bool first, string name, string unit, List<Ev> ev, bool useBytes)
    {
        if (!first) sb.Append(',');
        first = false;

        long baseline = useBytes ? ev[0].Bytes : 0;
        long end = (useBytes ? ev[^1].Bytes : ev[^1].Us) - baseline;

        sb.Append("{\"type\":\"evented\",\"name\":\"").Append(name)
          .Append("\",\"unit\":\"").Append(unit)
          .Append("\",\"startValue\":0,\"endValue\":").Append(end)
          .Append(",\"events\":[");

        for (int i = 0; i < ev.Count; i++)
        {
            if (i > 0) sb.Append(',');
            long at = (useBytes ? ev[i].Bytes : ev[i].Us) - baseline;
            sb.Append("{\"type\":\"").Append(ev[i].Open ? 'O' : 'C')
              .Append("\",\"frame\":").Append(ev[i].Frame)
              .Append(",\"at\":").Append(at).Append('}');
        }
        sb.Append("]}");
    }
}