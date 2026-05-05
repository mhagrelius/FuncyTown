using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FuncyTown.Generators.Tests;

internal static class JsonMetadataReferenceLoader
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        _ = typeof(JsonConverterAttribute).Assembly.Location;
    }
}

public class JsonConverterAttributeEmittedTests
{
    [Fact]
    public Task Json_enabled_value_alias_emits_converter_attribute_and_converter()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;
            [Result<int, FakeError>]
            public readonly partial record struct IntResult;
            """;

        var result = GeneratorTestHarness.Run(source);
        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public void Json_false_value_alias_omits_converter_attribute_and_converter()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;
            [Result<int, FakeError>(Json = false)]
            public readonly partial record struct IntResult;
            """;

        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

        var generated = GeneratorTestHarness.Run(source)
            .Results
            .SelectMany(static result => result.GeneratedSources)
            .Single(static source => source.HintName == "MyApp_IntResult.g.cs")
            .SourceText
            .ToString();

        Assert.DoesNotContain("JsonConverter", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("IntResultJsonConverter", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Json_false_void_alias_omits_converter_attribute_and_converter()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;
            [Result<FakeError>(Json = false)]
            public readonly partial record struct VoidResult;
            """;

        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

        var generated = GeneratorTestHarness.Run(source)
            .Results
            .SelectMany(static result => result.GeneratedSources)
            .Single(static source => source.HintName == "MyApp_VoidResult.g.cs")
            .SourceText
            .ToString();

        Assert.DoesNotContain("JsonConverter", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("VoidResultJsonConverter", generated, StringComparison.Ordinal);
    }
}
