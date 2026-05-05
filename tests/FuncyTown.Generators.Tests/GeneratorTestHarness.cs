using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FuncyTown.Generators.Tests;

internal static class GeneratorTestHarness
{
    public static GeneratorDriverRunResult Run(string source)
    {
        var compilation = CreateCompilation(source);
        var driver = CreateDriver();
        return driver.RunGenerators(compilation).GetRunResult();
    }

    public static GeneratorDriverRunResult RunWith(string source, params IIncrementalGenerator[] generators)
    {
        var compilation = CreateCompilation(source);
        var driver = CreateDriver(generators);
        return driver.RunGenerators(compilation).GetRunResult();
    }

    public static ImmutableArray<Diagnostic> Compile(string source)
    {
        return CompileWith(source, new ResultAliasGenerator());
    }

    public static ImmutableArray<Diagnostic> CompileWith(string source, params IIncrementalGenerator[] generators)
    {
        var compilation = CreateCompilation(source);
        var driver = CreateDriver(generators);
        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics);

        return outputCompilation.GetDiagnostics().AddRange(generatorDiagnostics);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Latest));
        var references = ImmutableArray.Create<MetadataReference>(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(FuncyTown.Result<,>).Assembly.Location))
            .AddRange(System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies
                .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
                .Cast<MetadataReference>());

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static CSharpGeneratorDriver CreateDriver()
    {
        var generator = new ResultAliasGenerator();
        return CSharpGeneratorDriver.Create(generator);
    }

    private static CSharpGeneratorDriver CreateDriver(params IIncrementalGenerator[] generators)
    {
        return CSharpGeneratorDriver.Create(generators);
    }
}
