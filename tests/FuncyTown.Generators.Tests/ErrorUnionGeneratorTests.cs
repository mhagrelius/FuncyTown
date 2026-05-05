using Microsoft.CodeAnalysis;

namespace FuncyTown.Generators.Tests;

public class ErrorUnionGeneratorTests
{
    [Fact]
    public Task BasicUnionEmitsMatchSwitchAndCombine()
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
            public sealed partial class Invalid(string reason) : UserError
            {
                public string Reason { get; } = reason;
            }
            """;

        var result = GeneratorTestHarness.RunWith(source, new ErrorUnionGenerator());
        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public void Pure_union_source_produces_generated_source()
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
            """;

        var result = GeneratorTestHarness.RunWith(source, new ErrorUnionGenerator());
        var generatorResult = Assert.Single(result.Results);
        var generatedSource = Assert.Single(generatorResult.GeneratedSources);

        Assert.Equal("MyApp_UserError.Union.g.cs", generatedSource.HintName);
        Assert.Contains("global::MyApp.NotFound", generatedSource.SourceText.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Pure_union_source_emits_json_polymorphism_metadata_for_exposed_cases_and_many()
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
            public sealed partial class Invalid(string reason) : UserError
            {
                public string Reason { get; } = reason;
            }
            """;

        var result = GeneratorTestHarness.RunWith(source, new ErrorUnionGenerator());
        var generatorResult = Assert.Single(result.Results);
        var generatedSource = Assert.Single(generatorResult.GeneratedSources).SourceText.ToString();

        Assert.Contains(
            """[global::System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "$case")]""",
            generatedSource,
            StringComparison.Ordinal);
        Assert.Contains(
            """[global::System.Text.Json.Serialization.JsonDerivedType(typeof(global::MyApp.NotFound), "NotFound")]""",
            generatedSource,
            StringComparison.Ordinal);
        Assert.Contains(
            """[global::System.Text.Json.Serialization.JsonDerivedType(typeof(global::MyApp.Invalid), "Invalid")]""",
            generatedSource,
            StringComparison.Ordinal);
        Assert.Contains(
            """[global::System.Text.Json.Serialization.JsonDerivedType(typeof(global::MyApp.UserError.Many), "Many")]""",
            generatedSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Basic_union_compiles_after_generation()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError
            {
            }

            [ErrorCase]
            public sealed partial class NotFound : UserError
            {
            }
            """;

        var diagnostics = GeneratorTestHarness.CompileWith(source, new ErrorUnionGenerator());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Basic_union_generated_api_compiles_after_generation()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class NotFound(Guid id) : UserError
            {
                public Guid Id { get; } = id;
            }

            [ErrorCase]
            public sealed partial class Invalid(string reason) : UserError
            {
                public string Reason { get; } = reason;
            }

            internal static class Usage
            {
                public static string Run(UserError error)
                {
                    var matched = error.Match(
                        notFound: nf => $"nf={nf.Id}",
                        invalid: invalid => $"invalid={invalid.Reason}",
                        many: many => $"many={many.Errors.Count}");
                    var fallback = error.Match<string>(
                        notFound: null,
                        invalid: null,
                        many: null,
                        fallback: other => other.Code);
                    var switched = string.Empty;
                    error.Switch(
                        notFound: nf => switched = nf.Id.ToString(),
                        invalid: invalid => switched = invalid.Reason,
                        many: many => switched = many.Errors.Count.ToString());
                    error.Switch(
                        notFound: null,
                        invalid: null,
                        many: null,
                        fallback: other => switched = other.Message);
                    UserError combined = Combine(new List<UserError>
                    {
                        new NotFound(Guid.Empty),
                        new Invalid("bad"),
                    });
                    return matched + fallback + switched + combined.Code;
                }

                private static TError Combine<TError>(IReadOnlyList<TError> errors)
                    where TError : ICombinableError<TError>
                {
                    return TError.Combine(errors);
                }
            }
            """;

        var diagnostics = GeneratorTestHarness.CompileWith(source, new ErrorUnionGenerator());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Zero_case_union_generated_api_compiles_with_many_handler()
    {
        const string source = """
            using System.Collections.Generic;
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            internal static class Usage
            {
                public static string Run(UserError error)
                {
                    var many = new UserError.Many(new List<UserError>());
                    var matched = many.Match(
                        many: value => value.Errors.Count.ToString());
                    var fallback = error.Match<string>(
                        many: null,
                        fallback: other => other.Code);
                    var switched = string.Empty;
                    many.Switch(
                        many: value => switched = value.Errors.Count.ToString());
                    error.Switch(
                        many: null,
                        fallback: other => switched = other.Message);
                    return matched + fallback + switched;
                }
            }
            """;

        var diagnostics = GeneratorTestHarness.CompileWith(source, new ErrorUnionGenerator());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Public_union_does_not_expose_internal_case_in_generated_handlers()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            internal sealed partial class Hidden : UserError
            {
            }
            """;

        var result = GeneratorTestHarness.RunWith(source, new ErrorUnionGenerator());
        var generatorResult = Assert.Single(result.Results);
        var generatedSource = Assert.Single(generatorResult.GeneratedSources).SourceText.ToString();

        Assert.DoesNotContain("global::System.Func<global::MyApp.Hidden", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("global::System.Action<global::MyApp.Hidden", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonDerivedType(typeof(global::MyApp.Hidden)", generatedSource, StringComparison.Ordinal);

        var diagnostics = GeneratorTestHarness.CompileWith(source, new ErrorUnionGenerator());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Public_union_does_not_expose_case_from_internal_containing_type()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            internal partial class Cases
            {
                [ErrorCase]
                public sealed partial class Hidden : UserError
                {
                }
            }
            """;

        var result = GeneratorTestHarness.RunWith(source, new ErrorUnionGenerator());
        var generatorResult = Assert.Single(result.Results);
        var generatedSource = Assert.Single(generatorResult.GeneratedSources).SourceText.ToString();

        Assert.DoesNotContain("global::System.Func<global::MyApp.Cases.Hidden", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("global::System.Action<global::MyApp.Cases.Hidden", generatedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonDerivedType(typeof(global::MyApp.Cases.Hidden)", generatedSource, StringComparison.Ordinal);

        var diagnostics = GeneratorTestHarness.CompileWith(source, new ErrorUnionGenerator());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Case_names_that_normalize_to_same_handler_name_compile()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class URL : UserError
            {
            }

            [ErrorCase]
            public sealed partial class uRL : UserError
            {
            }
            """;

        var diagnostics = GeneratorTestHarness.CompileWith(source, new ErrorUnionGenerator());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Case_handler_names_do_not_collide_with_generated_pattern_locals()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class Foo : UserError
            {
            }

            [ErrorCase]
            public sealed partial class FooValue : UserError
            {
            }
            """;

        var diagnostics = GeneratorTestHarness.CompileWith(source, new ErrorUnionGenerator());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Case_handler_names_do_not_collide_with_fallback_parameter()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class Fallback : UserError
            {
            }
            """;

        var diagnostics = GeneratorTestHarness.CompileWith(source, new ErrorUnionGenerator());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Error_union_inside_file_local_type_reports_FT0008()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            file static partial class Container
            {
                [ErrorUnion]
                public partial class NestedError;
            }
            """;

        var result = GeneratorTestHarness.RunWith(source, new ErrorUnionGenerator());
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Id == "FT0008");

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void Error_case_inside_file_local_type_reports_FT0008()
    {
        const string source = """
            using FuncyTown;
            namespace MyApp;

            [ErrorUnion]
            public partial class UserError;

            file static partial class Container
            {
                [ErrorCase]
                public sealed partial class Hidden : UserError
                {
                }
            }
            """;

        var result = GeneratorTestHarness.RunWith(source, new ErrorUnionGenerator());
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Id == "FT0008");

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }
}
