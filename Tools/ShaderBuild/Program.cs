using Prowl.Slang;
namespace ShaderBuild;

public class Program
{
    public static int Main(string[] args)
    {

        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: ShaderBuild <srcDir> <outDir>");
            return 2;
        }

        string srcDir = Path.GetFullPath(args[0]);
        string outDir = Path.GetFullPath(args[1]);
        bool debug = args.Contains("--debug");
        Directory.CreateDirectory(outDir);
        Console.WriteLine(srcDir);

        var kinds = new Dictionary<string, ShaderKind>(StringComparer.OrdinalIgnoreCase)
        {
            [".slang"] = ShaderKind.GeometryShader,
        };

        TargetDescription targetDesc = new()
        {
            Format = CompileTarget.Spirv, // Emit vulkan-compatible SPIR-V words
            Profile = GlobalSession.FindProfile("spirv_1_5") // Use SPIR-V 1.5 capabilities
        };

        // Description for a compilation session
        SessionDescription sessionDesc = new()
        {
            Targets = [targetDesc], // Only targeting SPIR-V - add more targets to automatically compile for multiple different APIs
            SearchPaths = [srcDir], // Search paths are relative to CWD
        };

        Session session = GlobalSession.CreateSession(sessionDesc);

        int failures = 0;

        foreach (var file in Directory.EnumerateFiles(srcDir, "*.shader.slang", SearchOption.AllDirectories))
        {
            if (!kinds.TryGetValue(Path.GetExtension(file), out var kind)) continue;
            string rel = Path.GetRelativePath(srcDir, file);
            rel = rel.Replace(".shader.slang", ".spv");
            string outFile = Path.Combine(outDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);
            Console.WriteLine("Testing to compile: " + rel + " out: " + outFile);
            // Incremental: skip if output is newer than source
            if (File.Exists(outFile) && File.GetLastWriteTimeUtc(outFile) >= File.GetLastWriteTimeUtc(file))
                continue;
            string _shader = file.Replace(srcDir, "");

            Module module = session.LoadModule(_shader, out DiagnosticInfo diagnostics);

            string[] names = { "VertexMain", "FragmentMain" };

            var parts = new List<ComponentType> { module };

            foreach (var n in names)
                if (TryFind(module, n, out var ep))
                    parts.Add(ep);

            if (parts.Count == 1)
            {
                Console.Error.WriteLine($"{file} : error SHADER : no known entry points found");
                failures++;
                continue;
            }

            ComponentType program = session.CreateCompositeComponentType( parts.ToArray(), out diagnostics);

            ComponentType linked = program.Link(out diagnostics);

            var result = linked.GetTargetCode(0, out diagnostics);

            if (result.IsEmpty)
            {
                Console.Error.WriteLine($"Compilation failed: {file}");
                failures++;
                continue;
            }

            File.WriteAllBytes(outFile, result.ToArray());
            Console.WriteLine($"Compiled {rel} -> {Path.GetFileName(outFile)}");
        }

        return failures == 0 ? 0 : 1;
    }

    static bool TryFind(Module module, string name, out EntryPoint ep)
    {
        try { ep = module.FindEntryPointByName(name); return true; }
        catch (Exception) { ep = default!; return false; }
    }
}