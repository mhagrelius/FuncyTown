using Microsoft.CodeAnalysis;

namespace FuncyTown.Generators.Tests;

public class CrossAliasGeneratorTests
{
    [Fact]
    public Task SiblingsInSameErrorFamilyGetCrossOverloads()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;
            [Result<int, FakeError>] public readonly partial record struct IntResult;
            [Result<string, FakeError>] public readonly partial record struct StringResult;
            """;
        var result = GeneratorTestHarness.Run(source);
        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public void Value_cross_alias_instance_methods_compile_after_generation()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct IntResult;

            [Result<string, FakeError>]
            public readonly partial record struct StringResult;

            internal static class Usage
            {
                public static string Run()
                {
                    StringResult bound = IntResult.Success(1)
                        .Bind(value => StringResult.Success(value.ToString()));
                    StringResult then = IntResult.Success(2)
                        .Then(value => StringResult.Success(value.ToString()));
                    StringResult andThen = IntResult.Success(3)
                        .AndThen(value => StringResult.Success(value.ToString()));
                    StringResult selectedMany = IntResult.Success(4)
                        .SelectMany(value => StringResult.Success(value.ToString()));

                    return string.Concat(bound.Value, then.Value, andThen.Value, selectedMany.Value);
                }

                public static async Task<string> RunAsync()
                {
                    StringResult result = await IntResult.Success(5)
                        .ThenAsync(async value =>
                        {
                            await Task.Yield();
                            return StringResult.Success(value.ToString());
                        });

                    return result.Value;
                }
            }
            """;
        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Task_cross_alias_extensions_compile_after_generation()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct IntResult;

            [Result<string, FakeError>]
            public readonly partial record struct StringResult;

            internal static class Usage
            {
                public static async Task<string> Run()
                {
                    Task<StringResult> syncTask = Task.FromResult(IntResult.Success(1))
                        .Then(value => StringResult.Success(value.ToString()));
                    Task<StringResult> asyncTask = Task.FromResult(IntResult.Success(2))
                        .Then(async value =>
                        {
                            await Task.Yield();
                            return StringResult.Success(value.ToString());
                        });

                    var syncResult = await syncTask;
                    var asyncResult = await asyncTask;
                    return syncResult.Value + asyncResult.Value;
                }
            }
            """;
        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Void_cross_alias_instance_and_task_methods_compile_after_generation()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            [Result<FakeError>]
            public readonly partial record struct VoidResult;

            [Result<int, FakeError>]
            public readonly partial record struct IntResult;

            internal static class Usage
            {
                public static async Task<int> Run()
                {
                    IntResult fromVoid = VoidResult.Success()
                        .Then(() => IntResult.Success(42));
                    Task<IntResult> fromTask = Task.FromResult(VoidResult.Success())
                        .Then(() => IntResult.Success(43));
                    Task<IntResult> fromTaskAsync = Task.FromResult(VoidResult.Success())
                        .Then(async () =>
                        {
                            await Task.Yield();
                            return IntResult.Success(44);
                        });

                    var taskResult = await fromTask;
                    var taskAsyncResult = await fromTaskAsync;
                    return fromVoid.Value + taskResult.Value + taskAsyncResult.Value;
                }
            }
            """;
        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Aliases_with_different_error_types_do_not_get_cross_overloads()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;
            public sealed record OtherError(string Code, string Message) : IError;
            [Result<int, FakeError>] public readonly partial record struct IntResult;
            [Result<string, OtherError>] public readonly partial record struct StringResult;
            """;
        var result = GeneratorTestHarness.Run(source);
        var generatedSources = result.Results.Single().GeneratedSources;
        var intResult = generatedSources.Single(source => source.HintName == "MyApp_IntResult.g.cs").SourceText.ToString();
        var intTaskExtensions = generatedSources.Single(source => source.HintName == "MyApp_IntResult.TaskExtensions.g.cs").SourceText.ToString();
        var diagnostics = GeneratorTestHarness.Compile(source);

        Assert.DoesNotContain("global::MyApp.StringResult", intResult, StringComparison.Ordinal);
        Assert.DoesNotContain("global::MyApp.StringResult", intTaskExtensions, StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Public_alias_does_not_expose_internal_same_error_sibling()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct PublicResult;

            [Result<string, FakeError>]
            internal readonly partial record struct InternalResult;
            """;
        var result = GeneratorTestHarness.Run(source);
        var generatedSources = result.Results.Single().GeneratedSources;
        var publicResult = generatedSources.Single(source => source.HintName == "MyApp_PublicResult.g.cs").SourceText.ToString();
        var publicTaskExtensions = generatedSources.Single(source => source.HintName == "MyApp_PublicResult.TaskExtensions.g.cs").SourceText.ToString();
        var diagnostics = GeneratorTestHarness.Compile(source);

        Assert.DoesNotContain("global::MyApp.InternalResult", publicResult, StringComparison.Ordinal);
        Assert.DoesNotContain("global::MyApp.InternalResult", publicTaskExtensions, StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Public_alias_does_not_reference_private_nested_same_error_sibling()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct PublicResult;

            public partial class Container
            {
                private partial class Hidden
                {
                    [Result<string, FakeError>]
                    public readonly partial record struct HiddenResult;
                }
            }
            """;
        var result = GeneratorTestHarness.Run(source);
        var generatedSources = result.Results.Single().GeneratedSources;
        var publicResult = generatedSources.Single(source => source.HintName == "MyApp_PublicResult.g.cs").SourceText.ToString();
        var publicTaskExtensions = generatedSources.Single(source => source.HintName == "MyApp_PublicResult.TaskExtensions.g.cs").SourceText.ToString();
        var diagnostics = GeneratorTestHarness.Compile(source);

        Assert.DoesNotContain("HiddenResult", publicResult, StringComparison.Ordinal);
        Assert.DoesNotContain("HiddenResult", publicTaskExtensions, StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Public_alias_does_not_reference_protected_nested_same_error_sibling()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct PublicResult;

            public partial class Container
            {
                protected partial class Hidden
                {
                    [Result<string, FakeError>]
                    public readonly partial record struct HiddenResult;
                }
            }
            """;
        var result = GeneratorTestHarness.Run(source);
        var generatedSources = result.Results.Single().GeneratedSources;
        var publicResult = generatedSources.Single(source => source.HintName == "MyApp_PublicResult.g.cs").SourceText.ToString();
        var publicTaskExtensions = generatedSources.Single(source => source.HintName == "MyApp_PublicResult.TaskExtensions.g.cs").SourceText.ToString();
        var diagnostics = GeneratorTestHarness.Compile(source);

        Assert.DoesNotContain("HiddenResult", publicResult, StringComparison.Ordinal);
        Assert.DoesNotContain("HiddenResult", publicTaskExtensions, StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }
}
