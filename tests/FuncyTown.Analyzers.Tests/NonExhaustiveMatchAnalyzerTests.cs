using FuncyTown.Analyzers.Tests.Verifiers;
using Xunit;

namespace FuncyTown.Analyzers.Tests;

public sealed class NonExhaustiveMatchAnalyzerTests
{
    [Fact]
    public Task Reports_on_result_match_with_generated_union_error()
    {
        const string source = """
            using FuncyTown;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class NotFound : UserError;

            [ErrorCase]
            public sealed partial class Invalid : UserError;

            public sealed class C
            {
                public Result<int, UserError> Get() => Result<int, UserError>.Failure(new NotFound());

                public string Use() =>
                    Get().{|FT0003:Match|}(
                        static value => value.ToString(),
                        static error => error.Message);
            }
            """;

        return CSharpAnalyzerVerifier<NonExhaustiveMatchAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_alias_match_with_generated_union_error()
    {
        const string source = """
            using FuncyTown;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class NotFound : UserError;

            [ErrorCase]
            public sealed partial class Invalid : UserError;

            [Result<int, UserError>]
            public readonly partial record struct UserResult;

            public sealed class C
            {
                public UserResult Get() => UserResult.Failure(new NotFound());

                public string Use() =>
                    Get().{|FT0003:Match|}(
                        static value => value.ToString(),
                        static error => error.Message);
            }
            """;

        return CSharpAnalyzerVerifier<NonExhaustiveMatchAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_many_for_zero_case_union()
    {
        const string source = """
            using FuncyTown;

            [ErrorUnion]
            public partial class UserError;

            public sealed class C
            {
                public Result<int, UserError> Get() => Result<int, UserError>.Failure(new UserError.Many([]));

                public string Use() =>
                    Get().{|FT0003:Match|}(
                        static value => value.ToString(),
                        static error => error.Message);
            }
            """;

        return CSharpAnalyzerVerifier<NonExhaustiveMatchAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_list_internal_case_for_public_union()
    {
        const string source = """
            using FuncyTown;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            internal sealed partial class Hidden : UserError;

            public sealed class C
            {
                public Result<int, UserError> Get() => Result<int, UserError>.Failure(new UserError.Many([]));

                public string Use() =>
                    Get().{|FT0003:Match|}(
                        static value => value.ToString(),
                        static error => error.Message);
            }
            """;

        return CSharpAnalyzerVerifier<NonExhaustiveMatchAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Lists_cross_namespace_cases()
    {
        const string source = """
            using FuncyTown;

            namespace MyApp.Errors
            {
                [ErrorUnion]
                public partial class UserError;

                [ErrorCase]
                public sealed partial class NotFound : UserError;
            }

            namespace MyApp.Cases
            {
                using MyApp.Errors;

                [ErrorCase]
                public sealed partial class Invalid : UserError;
            }

            namespace MyApp
            {
                using MyApp.Errors;

                public sealed class C
                {
                    public Result<int, UserError> Get() => Result<int, UserError>.Failure(new NotFound());

                    public string Use() =>
                        Get().Match(
                            static value => value.ToString(),
                            static error => error.Message);
                }
            }
            """;

        return CSharpAnalyzerVerifier<NonExhaustiveMatchAnalyzer>.VerifyAnalyzerAsync(
            source,
            CSharpAnalyzerVerifier<NonExhaustiveMatchAnalyzer>.Diagnostic()
                .WithSpan(29, 19, 29, 24)
                .WithArguments("UserError", "NotFound, Invalid, Many"));
    }

    [Fact]
    public Task Lists_internal_case_for_public_union_in_internal_container()
    {
        const string source = """
            using FuncyTown;

            internal static partial class Container
            {
                [ErrorUnion]
                public partial class UserError;

                [ErrorCase]
                internal sealed partial class Hidden : UserError;

                public sealed class C
                {
                    public Result<int, UserError> Get() => Result<int, UserError>.Failure(new Hidden());

                    public string Use() =>
                        Get().Match(
                            static value => value.ToString(),
                            static error => error.Message);
                }
            }
            """;

        return CSharpAnalyzerVerifier<NonExhaustiveMatchAnalyzer>.VerifyAnalyzerAsync(
            source,
            CSharpAnalyzerVerifier<NonExhaustiveMatchAnalyzer>.Diagnostic()
                .WithSpan(16, 19, 16, 24)
                .WithArguments("UserError", "Hidden, Many"));
    }

    [Fact]
    public Task Does_not_report_unrelated_two_argument_match()
    {
        const string source = """
            using FuncyTown;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class NotFound : UserError;

            [Result<int, UserError>]
            public readonly partial record struct UserResult
            {
                public string Match(string first, string second) => first + second;
            }

            public sealed class C
            {
                public UserResult Get() => UserResult.Failure(new NotFound());

                public string Use() => Get().Match("a", "b");
            }
            """;

        return CSharpAnalyzerVerifier<NonExhaustiveMatchAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_generated_union_exhaustive_or_fallback_match()
    {
        const string source = """
            using FuncyTown;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class NotFound : UserError;

            [ErrorCase]
            public sealed partial class Invalid : UserError;

            public sealed class C
            {
                public string Use(UserError error)
                {
                    var exhaustive = error.Match(
                        notFound: static _ => "missing",
                        invalid: static _ => "invalid",
                        many: static _ => "many");

                    var fallback = error.Match<string>(
                        notFound: null,
                        invalid: null,
                        many: null,
                        fallback: static other => other.Message);

                    return exhaustive + fallback;
                }
            }
            """;

        return CSharpAnalyzerVerifier<NonExhaustiveMatchAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_non_generated_error_match()
    {
        const string source = """
            using FuncyTown;

            public sealed record PlainError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, PlainError> Get() => Result<int, PlainError>.Failure(new PlainError("bad", "bad"));

                public string Use() =>
                    Get().Match(
                        static value => value.ToString(),
                        static error => error.Message);
            }
            """;

        return CSharpAnalyzerVerifier<NonExhaustiveMatchAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
