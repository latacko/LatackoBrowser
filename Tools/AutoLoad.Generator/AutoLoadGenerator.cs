using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoLoad.Generator;

[Generator]
public sealed class AutoLoadGenerator : IIncrementalGenerator
{
    private const string AttributeMetadataName = "InstanceFinderCore.InstanceAttribute";

    private static readonly DiagnosticDescriptor InvalidInstanceClass = new(
        id: "AL001",
        title: "Invalid [Instance] class",
        messageFormat: "Class '{0}' is marked [Instance] but is abstract, static, generic, not accessible, or has no accessible parameterless constructor",
        category: "AutoLoad",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AttributeNotFound = new(
        id: "AL002",
        title: "InstanceFinderCore.InstanceAttribute not found",
        messageFormat: "The AutoLoad.Generator is referenced, but this project does not reference the shared project containing InstanceFinderCore.InstanceAttribute",
        category: "AutoLoad",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private sealed class InvalidType
    {
        public InvalidType(string name, Location location)
        {
            Name = name;
            Location = location;
        }

        public string Name { get; }
        public Location Location { get; }
    }

    private sealed class ScanResult
    {
        public bool AttributeFound;
        public readonly List<(string Name, int Priority)> Valid = new();
        public readonly List<InvalidType> Invalid = new();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var scan = context.CompilationProvider.Select(static (compilation, ct) => Scan(compilation, ct));
        context.RegisterSourceOutput(scan, static (spc, result) => Emit(spc, result));
    }

    // ---------------------------------------------------------------- scanning

    private static ScanResult Scan(Compilation compilation, CancellationToken ct)
    {
        var result = new ScanResult();

        var attribute = compilation.GetTypeByMetadataName(AttributeMetadataName);
        if (attribute is null)
            return result;

        result.AttributeFound = true;
        var attributeAssembly = attribute.ContainingAssembly;

        // Assemblies to scan: the app itself, the shared assembly, and every referenced
        // assembly that references the shared assembly (skips Silk.NET, BenchmarkDotNet, BCL...).
        var assemblies = new List<IAssemblySymbol> { compilation.Assembly };

        foreach (var referenced in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            ct.ThrowIfCancellationRequested();

            bool relevant =
                SymbolEqualityComparer.Default.Equals(referenced, attributeAssembly) ||
                referenced.Modules.Any(m => m.ReferencedAssemblies.Any(a => a.Name == attributeAssembly.Name));

            if (relevant)
                assemblies.Add(referenced);
        }

        foreach (var assembly in assemblies)
            WalkNamespace(assembly.GlobalNamespace, compilation, attribute, result, ct);

        return result;
    }

    private static void WalkNamespace(INamespaceSymbol ns, Compilation compilation, INamedTypeSymbol attribute, ScanResult result, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol child)
                WalkNamespace(child, compilation, attribute, result, ct);
            else if (member is INamedTypeSymbol type)
                VisitType(type, compilation, attribute, result);
        }
    }

    private static void VisitType(INamedTypeSymbol type, Compilation compilation, INamedTypeSymbol attribute, ScanResult result)
    {
        if (type.TypeKind == TypeKind.Class && type.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute)))
        {
            string fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat); // global::MyApp.MyService

            bool hasCtor = type.InstanceConstructors.Any(c => c.Parameters.Length == 0 && compilation.IsSymbolAccessibleWithin(c, compilation.Assembly));

            bool valid =
                !type.IsAbstract &&
                !type.IsStatic &&
                !type.IsGenericType &&
                hasCtor &&
                compilation.IsSymbolAccessibleWithin(type, compilation.Assembly);

            if (valid)
            {
                var instanceAttribute = type.GetAttributes().First(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute));

                int priority = 0;

                foreach (var argument in instanceAttribute.NamedArguments)
                {
                    if (argument.Key == "Priority" &&
                        argument.Value.Value is int value)
                    {
                        priority = value;
                        break;
                    }
                }

                result.Valid.Add((fullName, priority));
            }
            else
                result.Invalid.Add(new InvalidType(
                    fullName,
                    type.Locations.FirstOrDefault(l => l.IsInSource) ?? Location.None));
        }

        // Nested classes
        foreach (var nested in type.GetTypeMembers())
            VisitType(nested, compilation, attribute, result);
    }

    // ---------------------------------------------------------------- emitting

    private static void Emit(SourceProductionContext spc, ScanResult result)
    {
        if (!result.AttributeFound)
        {
            spc.ReportDiagnostic(Diagnostic.Create(AttributeNotFound, Location.None));
            return;
        }

        foreach (var invalid in result.Invalid)
            spc.ReportDiagnostic(Diagnostic.Create(InvalidInstanceClass, invalid.Location, invalid.Name));
        var types = result.Valid
            .Distinct()
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace AutoLoad");
        sb.AppendLine("{");
        sb.AppendLine("    internal static class InstancesBootstrap");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void CreateInstances()");
        sb.AppendLine("        {");

        foreach (var type in types)
            sb.AppendLine($"            global::InstanceFinderCore.InstanceRegistry.Register(new {type.Name}());");

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        spc.AddSource("InstancesBootstrap.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}
