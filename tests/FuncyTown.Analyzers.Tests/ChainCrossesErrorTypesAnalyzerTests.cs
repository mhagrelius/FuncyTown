using FuncyTown.Analyzers.Tests.Verifiers;
using Xunit;

namespace FuncyTown.Analyzers.Tests;

public sealed class ChainCrossesErrorTypesAnalyzerTests
{
    [Fact]
    public Task Reports_on_direct_result_chain_with_different_error_type()
    {
        const string source = """
            using FuncyTown;

            public sealed record AlphaError(string Code, string Message) : IError;
            public sealed record BetaError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, AlphaError> Start() => Result<int, AlphaError>.Success(1);

                public Result<string, BetaError> Next(int value) =>
                    Result<string, BetaError>.Success(value.ToString());

                public Result<string, AlphaError> Use() =>
                    Start().{|FT0002:Then|}(value => Next(value));
            }
            """;

        return CSharpAnalyzerVerifier<ChainCrossesErrorTypesAnalyzer>
            .VerifyAnalyzerIgnoringCompilerErrorsAsync(source);
    }

    [Fact]
    public Task Reports_on_generated_alias_chain_with_different_error_type()
    {
        const string source = """
            using FuncyTown;

            public sealed record AlphaError(string Code, string Message) : IError;
            public sealed record BetaError(string Code, string Message) : IError;

            [Result<int, AlphaError>]
            public readonly partial record struct AlphaResult;

            [Result<string, BetaError>]
            public readonly partial record struct BetaResult;

            public sealed class C
            {
                public AlphaResult Start() => AlphaResult.Success(1);

                public BetaResult Next(int value) =>
                    BetaResult.Success(value.ToString());

                public AlphaResult Use() =>
                    Start().{|FT0002:Bind|}(value => Next(value));
            }
            """;

        return CSharpAnalyzerVerifier<ChainCrossesErrorTypesAnalyzer>
            .VerifyAnalyzerIgnoringCompilerErrorsAsync(source);
    }

    [Fact]
    public Task Reports_on_select_many_chain_alias_with_different_error_type()
    {
        const string source = """
            using FuncyTown;

            public sealed record AlphaError(string Code, string Message) : IError;
            public sealed record BetaError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, AlphaError> Start() => Result<int, AlphaError>.Success(1);

                public Result<string, BetaError> Next(int value) =>
                    Result<string, BetaError>.Success(value.ToString());

                public Result<string, AlphaError> Use() =>
                    Start().{|FT0002:SelectMany|}(value => Next(value));
            }
            """;

        return CSharpAnalyzerVerifier<ChainCrossesErrorTypesAnalyzer>
            .VerifyAnalyzerIgnoringCompilerErrorsAsync(source);
    }

    [Fact]
    public Task Reports_on_method_group_chain_with_different_error_type()
    {
        const string source = """
            using FuncyTown;

            public sealed record AlphaError(string Code, string Message) : IError;
            public sealed record BetaError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, AlphaError> Start() => Result<int, AlphaError>.Success(1);

                public Result<string, BetaError> Next(int value) =>
                    Result<string, BetaError>.Success(value.ToString());

                public Result<string, AlphaError> Use() =>
                    Start().{|FT0002:Then|}(Next);
            }
            """;

        return CSharpAnalyzerVerifier<ChainCrossesErrorTypesAnalyzer>
            .VerifyAnalyzerIgnoringCompilerErrorsAsync(source);
    }

    [Fact]
    public Task Reports_on_async_chain_with_different_error_type()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record AlphaError(string Code, string Message) : IError;
            public sealed record BetaError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, AlphaError> Start() => Result<int, AlphaError>.Success(1);

                public Task<Result<string, BetaError>> NextAsync(int value) =>
                    Task.FromResult(Result<string, BetaError>.Success(value.ToString()));

                public Task<Result<string, AlphaError>> UseAsync() =>
                    Start().{|FT0002:ThenAsync|}(value => NextAsync(value));
            }
            """;

        return CSharpAnalyzerVerifier<ChainCrossesErrorTypesAnalyzer>
            .VerifyAnalyzerIgnoringCompilerErrorsAsync(source);
    }

    [Fact]
    public Task Reports_on_block_lambda_when_later_return_crosses_error_type()
    {
        const string source = """
            using FuncyTown;

            public sealed record AlphaError(string Code, string Message) : IError;
            public sealed record BetaError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, AlphaError> Start() => Result<int, AlphaError>.Success(1);

                public Result<string, AlphaError> Same(int value) =>
                    Result<string, AlphaError>.Success(value.ToString());

                public Result<string, BetaError> Different(int value) =>
                    Result<string, BetaError>.Success(value.ToString());

                public Result<string, AlphaError> Use(bool branch) =>
                    Start().{|FT0002:Then|}(value =>
                    {
                        if (branch)
                        {
                            return Same(value);
                        }

                        return Different(value);
                    });
            }
            """;

        return CSharpAnalyzerVerifier<ChainCrossesErrorTypesAnalyzer>
            .VerifyAnalyzerIgnoringCompilerErrorsAsync(source);
    }

    [Fact]
    public Task Reports_on_task_result_chain_with_different_error_type()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record AlphaError(string Code, string Message) : IError;
            public sealed record BetaError(string Code, string Message) : IError;

            public sealed class C
            {
                public Task<Result<int, AlphaError>> StartAsync() =>
                    Task.FromResult(Result<int, AlphaError>.Success(1));

                public Result<string, BetaError> Next(int value) =>
                    Result<string, BetaError>.Success(value.ToString());

                public Task<Result<string, AlphaError>> UseAsync() =>
                    StartAsync().{|FT0002:Then|}(value => Next(value));
            }
            """;

        return CSharpAnalyzerVerifier<ChainCrossesErrorTypesAnalyzer>
            .VerifyAnalyzerIgnoringCompilerErrorsAsync(source);
    }

    [Fact]
    public Task Reports_on_task_alias_chain_with_different_error_type()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record AlphaError(string Code, string Message) : IError;
            public sealed record BetaError(string Code, string Message) : IError;

            [Result<int, AlphaError>]
            public readonly partial record struct AlphaResult;

            [Result<string, BetaError>]
            public readonly partial record struct BetaResult;

            public sealed class C
            {
                public Task<AlphaResult> StartAsync() =>
                    Task.FromResult(AlphaResult.Success(1));

                public BetaResult Next(int value) =>
                    BetaResult.Success(value.ToString());

                public Task<AlphaResult> UseAsync() =>
                    StartAsync().{|FT0002:Then|}(value => Next(value));
            }
            """;

        return CSharpAnalyzerVerifier<ChainCrossesErrorTypesAnalyzer>
            .VerifyAnalyzerIgnoringCompilerErrorsAsync(source);
    }

    [Fact]
    public Task Does_not_report_when_error_type_matches()
    {
        const string source = """
            using FuncyTown;

            public sealed record AlphaError(string Code, string Message) : IError;

            [Result<int, AlphaError>]
            public readonly partial record struct AlphaResult;

            public sealed class C
            {
                public Result<int, AlphaError> Start() => Result<int, AlphaError>.Success(1);

                public Result<string, AlphaError> Next(int value) =>
                    Result<string, AlphaError>.Success(value.ToString());

                public AlphaResult AliasStart() => AlphaResult.Success(1);

                public AlphaResult AliasNext(int value) => AlphaResult.Success(value + 1);

                public void Use()
                {
                    _ = Start().Then(value => Next(value));
                    _ = AliasStart().AndThen(value => AliasNext(value));
                }
            }
            """;

        return CSharpAnalyzerVerifier<ChainCrossesErrorTypesAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_nested_lambda_return_with_different_error_type()
    {
        const string source = """
            using System;
            using FuncyTown;

            public sealed record AlphaError(string Code, string Message) : IError;
            public sealed record BetaError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, AlphaError> Start() => Result<int, AlphaError>.Success(1);

                public Result<string, BetaError> Other(int value) =>
                    Result<string, BetaError>.Success(value.ToString());

                public Result<string, AlphaError> Use() =>
                    Start().Then(value =>
                    {
                        Func<Result<string, BetaError>> nested = () =>
                        {
                            return Other(value);
                        };

                        _ = nested;
                        return Result<string, AlphaError>.Success(value.ToString());
                    });
            }
            """;

        return CSharpAnalyzerVerifier<ChainCrossesErrorTypesAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
