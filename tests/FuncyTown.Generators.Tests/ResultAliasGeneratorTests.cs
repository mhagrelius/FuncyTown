namespace FuncyTown.Generators.Tests;

public class ResultAliasGeneratorTests
{
    [Fact]
    public Task DiscoversTwoArgAlias()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;
            [Result<int, FakeError>]
            public readonly partial record struct DummyResult;
            """;
        var result = GeneratorTestHarness.Run(source);
        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task DiscoversOneArgVoidAlias()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;
            [Result<FakeError>]
            public readonly partial record struct VoidResult;
            """;
        var result = GeneratorTestHarness.Run(source);
        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task ImplicitFalse_omits_implicit_conversions()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;
            [Result<int, FakeError>(Implicit = false)]
            public readonly partial record struct ExplicitResult;
            """;
        var result = GeneratorTestHarness.Run(source);
        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task TaskExtensionsAreEmittedForAlias()
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
    public Task AliasWithGeneratedUnionGetsPerCaseFactoriesAndConversions()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class NotFound(System.Guid id) : UserError
            {
                public System.Guid Id { get; } = id;
            }

            [ErrorCase]
            public sealed partial class Invalid(string why) : UserError
            {
                public string Why { get; } = why;
            }

            public sealed record User(System.Guid Id, string Name);

            [Result<User, UserError>]
            public readonly partial record struct UserResult;
            """;

        var result = GeneratorTestHarness.RunWith(source, new ErrorUnionGenerator(), new ResultAliasGenerator());
        var userResult = result.Results
            .SelectMany(static generator => generator.GeneratedSources)
            .Single(static source => source.HintName == "MyApp_UserResult.g.cs")
            .SourceText
            .ToString();

        Assert.Contains("public static UserResult Failure(global::MyApp.NotFound error) => new(global::FuncyTown.Result<global::MyApp.User, global::MyApp.UserError>.Failure(error));", userResult, StringComparison.Ordinal);
        Assert.Contains("public static UserResult Failure(global::MyApp.Invalid error) => new(global::FuncyTown.Result<global::MyApp.User, global::MyApp.UserError>.Failure(error));", userResult, StringComparison.Ordinal);
        Assert.Contains("public static implicit operator UserResult(global::MyApp.NotFound error) => Failure(error);", userResult, StringComparison.Ordinal);
        Assert.Contains("public static implicit operator UserResult(global::MyApp.Invalid error) => Failure(error);", userResult, StringComparison.Ordinal);

        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public void Public_alias_with_generated_union_does_not_expose_internal_case()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class NotFound : UserError
            {
            }

            [ErrorCase]
            internal sealed partial class Hidden : UserError
            {
            }

            public sealed record User(System.Guid Id, string Name);

            [Result<User, UserError>]
            public readonly partial record struct UserResult;
            """;

        var result = GeneratorTestHarness.RunWith(source, new ErrorUnionGenerator(), new ResultAliasGenerator());
        var userResult = result.Results
            .SelectMany(static generator => generator.GeneratedSources)
            .Single(static generated => generated.HintName == "MyApp_UserResult.g.cs")
            .SourceText
            .ToString();

        Assert.Contains("public static UserResult Failure(global::MyApp.NotFound error)", userResult, StringComparison.Ordinal);
        Assert.DoesNotContain("Hidden", userResult, StringComparison.Ordinal);

        var diagnostics = GeneratorTestHarness.CompileWith(source, new ErrorUnionGenerator(), new ResultAliasGenerator());
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Public_alias_with_generated_union_does_not_expose_private_nested_case()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class NotFound : UserError
            {
            }

            public partial class Container
            {
                private partial class HiddenScope
                {
                    [ErrorCase]
                    public sealed partial class Hidden : UserError
                    {
                    }
                }
            }

            public sealed record User(System.Guid Id, string Name);

            [Result<User, UserError>]
            public readonly partial record struct UserResult;
            """;

        var result = GeneratorTestHarness.RunWith(source, new ErrorUnionGenerator(), new ResultAliasGenerator());
        var userResult = result.Results
            .SelectMany(static generator => generator.GeneratedSources)
            .Single(static generated => generated.HintName == "MyApp_UserResult.g.cs")
            .SourceText
            .ToString();

        Assert.Contains("public static UserResult Failure(global::MyApp.NotFound error)", userResult, StringComparison.Ordinal);
        Assert.DoesNotContain("Hidden", userResult, StringComparison.Ordinal);

        var diagnostics = GeneratorTestHarness.CompileWith(source, new ErrorUnionGenerator(), new ResultAliasGenerator());
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Public_alias_with_generated_union_does_not_expose_protected_nested_case()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class NotFound : UserError
            {
            }

            public partial class Container
            {
                protected partial class HiddenScope
                {
                    [ErrorCase]
                    public sealed partial class Hidden : UserError
                    {
                    }
                }
            }

            public sealed record User(System.Guid Id, string Name);

            [Result<User, UserError>]
            public readonly partial record struct UserResult;
            """;

        var result = GeneratorTestHarness.RunWith(source, new ErrorUnionGenerator(), new ResultAliasGenerator());
        var userResult = result.Results
            .SelectMany(static generator => generator.GeneratedSources)
            .Single(static generated => generated.HintName == "MyApp_UserResult.g.cs")
            .SourceText
            .ToString();

        Assert.Contains("public static UserResult Failure(global::MyApp.NotFound error)", userResult, StringComparison.Ordinal);
        Assert.DoesNotContain("Hidden", userResult, StringComparison.Ordinal);

        var diagnostics = GeneratorTestHarness.CompileWith(source, new ErrorUnionGenerator(), new ResultAliasGenerator());
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void NonPartial_target_reports_FT0007()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;
            [Result<int, FakeError>]
            public readonly record struct NotPartialResult;
            """;
        var result = GeneratorTestHarness.Run(source);
        var diagnostics = result.Diagnostics;
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "FT0007");
    }

    [Fact]
    public void Internal_alias_compiles_after_generation()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            internal sealed record FakeError(string Code, string Message) : IError;
            [Result<int, FakeError>]
            internal readonly partial record struct InternalResult;

            internal static class Usage
            {
                public static int Run() => InternalResult.Success(41)
                    .Then(value => InternalResult.Success(value + 1))
                    .Value;
            }
            """;
        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Struct_alias_with_empty_parameter_list_compiles_after_generation()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [Result<int, string>]
            public readonly partial record struct OperationResult();

            internal static class Usage
            {
                public static int Run() => OperationResult.Success(42).Value;
            }
            """;

        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "FT0009");
    }

    [Fact]
    public void Struct_alias_with_primary_constructor_parameters_reports_FT0009()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [Result<int, string>]
            public readonly partial record struct OperationResult(string myVal);
            """;

        var result = GeneratorTestHarness.Run(source);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Id == "FT0009");
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Empty(result.Results.Single().GeneratedSources);

        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "FT0009");
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Nested_alias_compiles_after_generation()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            public static partial class Container
            {
                [Result<int, FakeError>]
                public readonly partial record struct NestedResult;

                public static int Run() => NestedResult.Success(42).Value;
            }
            """;
        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Value_alias_async_instance_methods_compile_after_generation()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;
            public sealed record OtherError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct IntResult;

            internal static class Usage
            {
                public static async Task<int> Run()
                {
                    var same = await IntResult.Success(1)
                        .ThenAsync(async value => { await Task.Yield(); return IntResult.Success(value + 1); });
                    same = await same.TapAsync(async value => { await Task.Yield(); _ = value; });
                    same = await same.TapErrorAsync(async error => { await Task.Yield(); _ = error; });
                    same = await same.EnsureAsync(async value => { await Task.Yield(); return value > 0; }, new FakeError("NEG", "negative"));
                    same = await same.RecoverAsync(async error => { await Task.Yield(); _ = error; return 0; });

                    var mapped = await same.MapAsync(async value => { await Task.Yield(); return value + 1; });
                    var transformed = await same.TransformAsync(async value => { await Task.Yield(); return value.ToString(); });
                    var selected = await same.SelectAsync(async value => { await Task.Yield(); return value.ToString(); });
                    var bound = await same.BindAsync(async value => { await Task.Yield(); return Result<string, FakeError>.Success(value.ToString()); });
                    var then = await same.ThenAsync(async value => { await Task.Yield(); return Result<string, FakeError>.Success(value.ToString()); });
                    var andThen = await same.AndThenAsync(async value => { await Task.Yield(); return Result<string, FakeError>.Success(value.ToString()); });
                    var selectedMany = await same.SelectManyAsync(async value => { await Task.Yield(); return Result<string, FakeError>.Success(value.ToString()); });
                    var mappedError = await same.MapErrorAsync(async error => { await Task.Yield(); return new OtherError(error.Code, error.Message); });
                    var transformedError = await same.TransformErrorAsync(async error => { await Task.Yield(); return new OtherError(error.Code, error.Message); });
                    var sameError = await same.MapErrorAsync(async error => { await Task.Yield(); return new FakeError(error.Code, error.Message); });
                    var sameTransformedError = await same.TransformErrorAsync(async error => { await Task.Yield(); return new FakeError(error.Code, error.Message); });
                    var recovered = await IntResult.Failure(new FakeError("X", "x"))
                        .RecoverWithAsync(async error => { await Task.Yield(); _ = error; return IntResult.Success(3); });
                    var orElse = await recovered.OrElseAsync(async error => { await Task.Yield(); _ = error; return 4; });
                    var orElseThen = await orElse.OrElseThenAsync(async error => { await Task.Yield(); _ = error; return IntResult.Success(5); });
                    var ifSuccessful = await orElseThen.IfSuccessfulAsync(async value => { await Task.Yield(); _ = value; });
                    var ifFailed = await ifSuccessful.IfFailedAsync(async error => { await Task.Yield(); _ = error; });
                    var validated = await ifFailed.ValidateAsync(async value => { await Task.Yield(); return value > 0; }, new FakeError("NEG", "negative"));

                    _ = mapped;
                    _ = transformed;
                    _ = selected;
                    _ = bound;
                    _ = then;
                    _ = andThen;
                    _ = selectedMany;
                    _ = mappedError;
                    _ = transformedError;
                    _ = sameError;
                    _ = sameTransformedError;

                    return await validated.MatchAsync(
                        async value => { await Task.Yield(); return value; },
                        async error => { await Task.Yield(); _ = error; return -1; });
                }
            }
            """;
        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Value_alias_try_methods_compile_for_exceptional_error()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;
            using FuncyTown;
            namespace MyApp;

            public sealed record AppError(string Code, string Message, Exception? Exception = null) : IExceptionalError<AppError>
            {
                public static AppError FromException(Exception exception, string? code = null, string? message = null) =>
                    new(code ?? exception.GetType().Name, message ?? exception.Message, exception);
            }

            [Result<string, AppError>]
            public readonly partial record struct OperationResult;

            [Result<int, AppError>]
            public readonly partial record struct ParsedResult;

            internal static class Usage
            {
                public static int Run(string path)
                {
                    return OperationResult.Success(path)
                        .MapTry(ReadAllText, code: "ReadFailed")
                        .ThenTry(ParseDocument, code: "ParseFailed")
                        .Value;
                }

                public static async Task<int> RunAsync(string path)
                {
                    var result = await OperationResult.Success(path)
                        .MapTryAsync(ReadAllTextAsync, code: "ReadFailed")
                        .ThenTryAsync(ParseDocumentAsync, code: "ParseFailed");

                    return result.Value;
                }

                private static string ReadAllText(string path) => path;

                private static Task<string> ReadAllTextAsync(string path) => Task.FromResult(path);

                private static ParsedResult ParseDocument(string text) => text.Length;

                private static Task<ParsedResult> ParseDocumentAsync(string text) => Task.FromResult(ParsedResult.Success(text.Length));
            }
            """;

        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Value_alias_try_methods_are_not_emitted_for_non_exceptional_error()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<string, FakeError>]
            public readonly partial record struct OperationResult;
            """;

        var result = GeneratorTestHarness.Run(source);
        var generated = result.Results
            .Single()
            .GeneratedSources
            .Single(source => source.HintName == "MyApp_OperationResult.g.cs")
            .SourceText
            .ToString();

        Assert.DoesNotContain("MapTry", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("ThenTry", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Void_alias_async_instance_methods_compile_after_generation()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;
            public sealed record OtherError(string Code, string Message) : IError;

            [Result<FakeError>]
            public readonly partial record struct VoidResult;

            internal static class Usage
            {
                public static async Task<int> Run()
                {
                    var same = await VoidResult.Success()
                        .ThenAsync(async () => { await Task.Yield(); return VoidResult.Success(); });
                    same = await same.TapAsync(async () => await Task.Yield());
                    same = await same.TapErrorAsync(async error => { await Task.Yield(); _ = error; });

                    var mapped = await same.MapAsync(async () => { await Task.Yield(); return 1; });
                    var transformed = await same.TransformAsync(async () => { await Task.Yield(); return 2; });
                    var selected = await same.SelectAsync(async () => { await Task.Yield(); return 3; });
                    var bound = await same.BindAsync(async () => { await Task.Yield(); return Result<int, FakeError>.Success(3); });
                    var then = await same.ThenAsync(async () => { await Task.Yield(); return Result<int, FakeError>.Success(4); });
                    var andThen = await same.AndThenAsync(async () => { await Task.Yield(); return Result<int, FakeError>.Success(5); });
                    var sameSelectedMany = await same.SelectManyAsync(async () => { await Task.Yield(); return VoidResult.Success(); });
                    var selectedMany = await same.SelectManyAsync(async () => { await Task.Yield(); return Result<int, FakeError>.Success(6); });
                    var mappedError = await same.MapErrorAsync(async error => { await Task.Yield(); return new OtherError(error.Code, error.Message); });
                    var transformedError = await same.TransformErrorAsync(async error => { await Task.Yield(); return new OtherError(error.Code, error.Message); });
                    var sameError = await same.MapErrorAsync(async error => { await Task.Yield(); return new FakeError(error.Code, error.Message); });
                    var sameTransformedError = await same.TransformErrorAsync(async error => { await Task.Yield(); return new FakeError(error.Code, error.Message); });
                    var recovered = await VoidResult.Failure(new FakeError("X", "x"))
                        .RecoverWithAsync(async error => { await Task.Yield(); _ = error; return VoidResult.Success(); });
                    var orElseThen = await recovered.OrElseThenAsync(async error => { await Task.Yield(); _ = error; return VoidResult.Success(); });
                    var ifSuccessful = await orElseThen.IfSuccessfulAsync(async () => await Task.Yield());
                    var ifFailed = await ifSuccessful.IfFailedAsync(async error => { await Task.Yield(); _ = error; });

                    _ = mapped;
                    _ = transformed;
                    _ = selected;
                    _ = bound;
                    _ = then;
                    _ = andThen;
                    _ = sameSelectedMany;
                    _ = selectedMany;
                    _ = mappedError;
                    _ = transformedError;
                    _ = sameError;
                    _ = sameTransformedError;

                    return await ifFailed.MatchAsync(
                        async () => { await Task.Yield(); return 1; },
                        async error => { await Task.Yield(); _ = error; return -1; });
                }
            }
            """;
        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Value_alias_task_extensions_compile_after_generation()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct IntResult;

            internal static class Usage
            {
                public static async Task<int> Run()
                {
                    return await Task.FromResult(IntResult.Success(2))
                        .Then(async value => { await Task.Yield(); return IntResult.Success(value + 40); })
                        .OnSuccess(async value => { await Task.Yield(); _ = value; })
                        .OnFailure(async error => { await Task.Yield(); _ = error; })
                        .Match(
                            async value => { await Task.Yield(); return value; },
                            async error => { await Task.Yield(); _ = error; return -1; });
                }
            }
            """;
        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Void_alias_task_extensions_compile_after_generation()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            [Result<FakeError>]
            public readonly partial record struct VoidResult;

            internal static class Usage
            {
                public static async Task<int> Run()
                {
                    return await Task.FromResult(VoidResult.Success())
                        .Then(async () => { await Task.Yield(); return VoidResult.Success(); })
                        .OnSuccess(async () => await Task.Yield())
                        .OnFailure(async error => { await Task.Yield(); _ = error; })
                        .Match(
                            async () => { await Task.Yield(); return 1; },
                            async error => { await Task.Yield(); _ = error; return -1; });
                }
            }
            """;
        var diagnostics = GeneratorTestHarness.Compile(source);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public void Private_nested_alias_does_not_emit_task_extensions()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            public static partial class Container
            {
                [Result<int, FakeError>]
                private readonly partial record struct PrivateResult;
            }
            """;
        var result = GeneratorTestHarness.Run(source);
        var generatedSources = result.Results.Single().GeneratedSources;

        Assert.DoesNotContain(generatedSources, source => source.HintName.Contains("PrivateResultTaskExtensions", StringComparison.Ordinal));
    }

    [Fact]
    public void Generic_nested_alias_does_not_emit_task_extensions()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            public partial class Container<T>
            {
                [Result<T, FakeError>]
                public readonly partial record struct NestedResult;
            }
            """;
        var result = GeneratorTestHarness.Run(source);
        var generatedSources = result.Results.Single().GeneratedSources;

        Assert.DoesNotContain(generatedSources, source => source.HintName.EndsWith(".TaskExtensions.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void Generic_alias_does_not_emit_task_extensions()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            [Result<T, FakeError>]
            public readonly partial record struct GenericResult<T>;
            """;
        var result = GeneratorTestHarness.Run(source);
        var generatedSources = result.Results.Single().GeneratedSources;

        Assert.DoesNotContain(generatedSources, source => source.HintName.EndsWith(".TaskExtensions.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void Aliases_are_emitted_by_error_type_family_after_projection_diagnostics()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record AlphaError(string Code, string Message) : IError;
            public sealed record BetaError(string Code, string Message) : IError;

            [Result<int, AlphaError>]
            public readonly partial record struct AlphaResult;

            [Result<int, BetaError>]
            public readonly partial record struct BetaResult;

            [Result<AlphaError>]
            public readonly partial record struct AlphaVoidResult;

            [Result<int, AlphaError>]
            public readonly record struct MissingPartialResult;
            """;
        var result = GeneratorTestHarness.Run(source);
        var generatedAliasHints = result.Results
            .Single()
            .GeneratedSources
            .Where(static source => !source.HintName.EndsWith(".TaskExtensions.g.cs", StringComparison.Ordinal))
            .Select(static source => source.HintName)
            .ToArray();

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "FT0007");
        Assert.Collection(
            generatedAliasHints,
            hint => Assert.Equal("MyApp_AlphaResult.g.cs", hint),
            hint => Assert.Equal("MyApp_AlphaVoidResult.g.cs", hint),
            hint => Assert.Equal("MyApp_BetaResult.g.cs", hint));
    }

    [Fact]
    public void Implicit_private_containing_alias_does_not_emit_task_extensions()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            public partial class Container
            {
                partial class Hidden
                {
                    [Result<int, FakeError>]
                    public readonly partial record struct HiddenResult;
                }
            }
            """;
        var result = GeneratorTestHarness.Run(source);
        var generatedSources = result.Results.Single().GeneratedSources;

        Assert.DoesNotContain(generatedSources, source => source.HintName.EndsWith(".TaskExtensions.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void Nested_alias_inside_file_local_type_reports_FT0008()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;
            public sealed record FakeError(string Code, string Message) : IError;

            file static partial class Container
            {
                [Result<int, FakeError>]
                public readonly partial record struct NestedResult;
            }
            """;
        var result = GeneratorTestHarness.Run(source);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Id == "FT0008");
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, diagnostic.Severity);
    }
}
