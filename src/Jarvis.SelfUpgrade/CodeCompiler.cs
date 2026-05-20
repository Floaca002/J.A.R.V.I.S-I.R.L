using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Jarvis.SelfUpgrade;

/// <summary>
/// Compiles C# source code at runtime using Roslyn and loads it into a collectible
/// AssemblyLoadContext so it can be unloaded later for hot-swapping.
/// </summary>
public sealed class CodeCompiler
{
    public CompilationResult Compile(string sourceCode, string assemblyName)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);

        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        // Add base reference assemblies that may not be loaded yet.
        var coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        foreach (var dll in new[] { "System.Runtime.dll", "netstandard.dll", "System.Collections.dll" })
        {
            var p = Path.Combine(coreDir, dll);
            if (File.Exists(p)) refs.Add(MetadataReference.CreateFromFile(p));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { tree },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emit = compilation.Emit(ms);

        if (!emit.Success)
        {
            var errors = emit.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToList();
            return new CompilationResult(false, null, errors, sourceCode);
        }

        ms.Seek(0, SeekOrigin.Begin);
        var ctx = new CollectibleLoadContext(assemblyName);
        var asm = ctx.LoadFromStream(ms);
        return new CompilationResult(true, asm, new List<string>(), sourceCode);
    }

    private sealed class CollectibleLoadContext(string name) : AssemblyLoadContext(name, isCollectible: true);
}

public sealed record CompilationResult(
    bool Success,
    Assembly? Assembly,
    IReadOnlyList<string> Errors,
    string SourceCode);
