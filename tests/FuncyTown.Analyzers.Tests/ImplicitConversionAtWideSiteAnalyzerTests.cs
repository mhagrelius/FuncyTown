using FuncyTown.Analyzers.Tests.Verifiers;
using Xunit;

namespace FuncyTown.Analyzers.Tests;

public sealed class ImplicitConversionAtWideSiteAnalyzerTests
{
    [Fact]
    public Task Reports_on_direct_result_assigned_to_object()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, FakeError> Get() => Result<int, FakeError>.Success(1);

                public void Use()
                {
                    object value = {|FT0004:Get()|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<ImplicitConversionAtWideSiteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_generated_alias_assigned_to_object()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct UserResult;

            public sealed class C
            {
                public UserResult Get() => UserResult.Success(1);

                public void Use()
                {
                    object value = {|FT0004:Get()|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<ImplicitConversionAtWideSiteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_result_passed_to_object_parameter()
    {
        // Argument position to `object` is benign: covers Console.WriteLine, string.Format,
        // logging APIs, and user-defined `void M(object v)` system-boundary signatures.
        // Flagging at Info severity is noisier than useful; assignments and returns still fire.
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, FakeError> Get() => Result<int, FakeError>.Success(1);

                public void Accept(object value)
                {
                    _ = value;
                }

                public void Use() => Accept(Get());
            }
            """;

        return CSharpAnalyzerVerifier<ImplicitConversionAtWideSiteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_generated_alias_passed_to_object_parameter()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct UserResult;

            public sealed class C
            {
                public UserResult Get() => UserResult.Success(1);

                public void Accept(object value)
                {
                    _ = value;
                }

                public void Use() => Accept(Get());
            }
            """;

        return CSharpAnalyzerVerifier<ImplicitConversionAtWideSiteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_console_writeline_or_format_calls()
    {
        const string source = """
            using System;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct UserResult;

            public sealed class C
            {
                public Result<int, FakeError> GetResult() => Result<int, FakeError>.Success(1);

                public UserResult GetAlias() => UserResult.Success(1);

                public string Use()
                {
                    Console.WriteLine(GetResult());
                    Console.WriteLine("{0}", GetResult());
                    Console.WriteLine("{0} {1}", GetResult(), GetAlias());
                    return string.Format("got {0}", GetAlias());
                }
            }
            """;

        return CSharpAnalyzerVerifier<ImplicitConversionAtWideSiteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_direct_result_returned_as_object()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, FakeError> Get() => Result<int, FakeError>.Success(1);

                public object Use()
                {
                    return {|FT0004:Get()|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<ImplicitConversionAtWideSiteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_generated_alias_returned_as_object()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct UserResult;

            public sealed class C
            {
                public UserResult Get() => UserResult.Success(1);

                public object Use()
                {
                    return {|FT0004:Get()|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<ImplicitConversionAtWideSiteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_generated_alias_assigned_to_dynamic()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct UserResult;

            public sealed class C
            {
                public UserResult Get() => UserResult.Success(1);

                public void Use()
                {
                    dynamic value = {|FT0004:Get()|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<ImplicitConversionAtWideSiteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_direct_result_assigned_to_value_type()
    {
        const string source = """
            using System;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, FakeError> Get() => Result<int, FakeError>.Success(1);

                public void Use()
                {
                    ValueType value = {|FT0004:Get()|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<ImplicitConversionAtWideSiteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_generated_alias_assigned_to_value_type()
    {
        const string source = """
            using System;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct UserResult;

            public sealed class C
            {
                public UserResult Get() => UserResult.Success(1);

                public void Use()
                {
                    ValueType value = {|FT0004:Get()|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<ImplicitConversionAtWideSiteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_strongly_typed_assignment_or_explicit_cast()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct UserResult;

            public sealed class C
            {
                public Result<int, FakeError> GetResult() => Result<int, FakeError>.Success(1);

                public UserResult GetAlias() => UserResult.Success(1);

                public void Use()
                {
                    Result<int, FakeError> result = GetResult();
                    UserResult alias = GetAlias();
                    object explicitResult = (object)GetResult();
                    object explicitAlias = (object)GetAlias();
                    _ = result;
                    _ = alias;
                    _ = explicitResult;
                    _ = explicitAlias;
                }
            }
            """;

        return CSharpAnalyzerVerifier<ImplicitConversionAtWideSiteAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
