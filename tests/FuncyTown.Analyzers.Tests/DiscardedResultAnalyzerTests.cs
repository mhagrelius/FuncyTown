using FuncyTown.Analyzers.CodeFixes;
using FuncyTown.Analyzers.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using Shouldly;
using Xunit;

namespace FuncyTown.Analyzers.Tests;

public sealed class DiscardedResultAnalyzerTests
{
    private static readonly DiagnosticDescriptor DiscardedResultDescriptor = GetDiscardedResultDescriptor();

    private static readonly string[] ExpectedDiscardActionTitles = ["Discard explicitly"];

    [Fact]
    public Task Reports_on_discarded_result()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, FakeError> M() => Result<int, FakeError>.Success(1);

                public void Use()
                {
                    {|FT0001:M()|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<DiscardedResultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_discarded_generated_alias()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct IntResult;

            public sealed class C
            {
                public IntResult M() => IntResult.Success(1);

                public void Use()
                {
                    {|FT0001:M()|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<DiscardedResultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_discarded_task_result()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Task<Result<int, FakeError>> MAsync() =>
                    Task.FromResult(Result<int, FakeError>.Success(1));

                public void Use()
                {
                    {|FT0001:MAsync()|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<DiscardedResultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_discarded_task_result_alias()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct IntResult;

            public sealed class C
            {
                public Task<IntResult> MAsync() =>
                    Task.FromResult(IntResult.Success(1));

                public void Use()
                {
                    {|FT0001:MAsync()|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<DiscardedResultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_explicit_discard()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, FakeError> M() => Result<int, FakeError>.Success(1);

                public void Use()
                {
                    _ = M();
                }
            }
            """;

        return CSharpAnalyzerVerifier<DiscardedResultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_stored_returned_or_matched_result()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, FakeError> M() => Result<int, FakeError>.Success(1);

                public Result<int, FakeError> ReturnIt() => M();

                public string Use()
                {
                    var result = M();
                    return result.Match(
                        static value => value.ToString(),
                        static error => error.Message);
                }
            }
            """;

        return CSharpAnalyzerVerifier<DiscardedResultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_assignment_to_existing_storage()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                private Result<int, FakeError> _field = Result<int, FakeError>.Success(0);
                private Task<Result<int, FakeError>> _pending = Task.FromResult(Result<int, FakeError>.Success(0));

                public Result<int, FakeError> Property { get; private set; } = Result<int, FakeError>.Success(0);

                public Result<int, FakeError> M() => Result<int, FakeError>.Success(1);

                public Task<Result<int, FakeError>> MAsync() =>
                    Task.FromResult(Result<int, FakeError>.Success(1));

                public void Use()
                {
                    var local = Result<int, FakeError>.Success(0);
                    local = M();
                    _field = M();
                    Property = M();
                    _pending = MAsync();
                }
            }
            """;

        return CSharpAnalyzerVerifier<DiscardedResultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_terminal_tap_style_chain_step()
    {
        // result.IfFailed(...) returns the unchanged Result by design; firing FT0001 here
        // would punish exactly the side-effect-as-terminal pattern the library encourages.
        const string source = """
            using System;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, FakeError> M() => Result<int, FakeError>.Success(1);

                public void Use()
                {
                    M().IfFailed(_ => Console.WriteLine("oops"));
                    M().OnFailure(_ => Console.WriteLine("oops"));
                    M().Tap(value => Console.WriteLine(value));
                    M().TapError(_ => { });
                    M().OnSuccess(_ => { });
                    M().IfSuccessful(_ => { });
                }
            }
            """;

        return CSharpAnalyzerVerifier<DiscardedResultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_awaited_terminal_tap_style_on_task_result()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Task<Result<int, FakeError>> MAsync() =>
                    Task.FromResult(Result<int, FakeError>.Success(1));

                public async Task UseAsync()
                {
                    await MAsync().IfFailed(_ => { });
                    await MAsync().OnFailure(_ => { });
                }
            }
            """;

        return CSharpAnalyzerVerifier<DiscardedResultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Still_reports_when_terminal_is_a_transform_chain_step()
    {
        // Map produces a NEW Result with no consumer — that's a real bug, FT0001 fires.
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, FakeError> M() => Result<int, FakeError>.Success(1);

                public void Use()
                {
                    {|FT0001:M().Map(value => value + 1)|};
                }
            }
            """;

        return CSharpAnalyzerVerifier<DiscardedResultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_awaited_and_stored_result()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Task<Result<int, FakeError>> MAsync() =>
                    Task.FromResult(Result<int, FakeError>.Success(1));

                public async Task<int> UseAsync()
                {
                    var result = await MAsync();
                    return result.Match(
                        static value => value,
                        static error => 0);
                }
            }
            """;

        return CSharpAnalyzerVerifier<DiscardedResultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Discard_code_fix_prepends_explicit_discard()
    {
        const string source = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, FakeError> M() => Result<int, FakeError>.Success(1);

                public void Use()
                {
                    {|FT0001:M()|};
                }
            }
            """;

        const string fixedSource = """
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Result<int, FakeError> M() => Result<int, FakeError>.Success(1);

                public void Use()
                {
                    _ = M();
                }
            }
            """;

        return CSharpCodeFixVerifier<DiscardedResultAnalyzer, DiscardedResultCodeFix>.VerifyCodeFixAsync(
            source,
            fixedSource);
    }

    [Fact]
    public Task Await_discard_code_fix_awaits_task_result_when_legal()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Task<Result<int, FakeError>> MAsync() =>
                    Task.FromResult(Result<int, FakeError>.Success(1));

                public async Task UseAsync()
                {
                    {|FT0001:MAsync()|};
                }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Task<Result<int, FakeError>> MAsync() =>
                    Task.FromResult(Result<int, FakeError>.Success(1));

                public async Task UseAsync()
                {
                    _ = await MAsync();
                }
            }
            """;

        return CSharpCodeFixVerifier<DiscardedResultAnalyzer, DiscardedResultCodeFix>.VerifyCodeFixAsync(
            source,
            codeActionIndex: 1,
            fixedSource);
    }

    [Fact]
    public Task Await_discard_code_fix_awaits_task_alias_when_legal()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct IntResult;

            public sealed class C
            {
                public Task<IntResult> MAsync() =>
                    Task.FromResult(IntResult.Success(1));

                public async Task UseAsync()
                {
                    {|FT0001:MAsync()|};
                }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            [Result<int, FakeError>]
            public readonly partial record struct IntResult;

            public sealed class C
            {
                public Task<IntResult> MAsync() =>
                    Task.FromResult(IntResult.Success(1));

                public async Task UseAsync()
                {
                    _ = await MAsync();
                }
            }
            """;

        return CSharpCodeFixVerifier<DiscardedResultAnalyzer, DiscardedResultCodeFix>.VerifyCodeFixAsync(
            source,
            codeActionIndex: 1,
            fixedSource);
    }

    [Fact]
    public async Task Await_discard_code_fix_is_not_offered_when_await_is_illegal()
    {
        const string source = """
            using System.Threading.Tasks;
            using FuncyTown;

            public sealed record FakeError(string Code, string Message) : IError;

            public sealed class C
            {
                public Task<Result<int, FakeError>> MAsync() =>
                    Task.FromResult(Result<int, FakeError>.Success(1));

                public void Use()
                {
                    MAsync();
                }
            }
            """;

        var codeActions = await GetRegisteredCodeActionsAsync(source);

        codeActions.Select(static action => action.Title).ShouldBe(ExpectedDiscardActionTitles);
    }

    private static async Task<IReadOnlyList<CodeAction>> GetRegisteredCodeActionsAsync(string source)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace
            .AddProject("CodeFixTest", LanguageNames.CSharp)
            .WithParseOptions(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))
            .WithMetadataReferences(
                (await ReferenceAssemblyResolver.Net10.ResolveAsync(LanguageNames.CSharp, CancellationToken.None).ConfigureAwait(false))
                .Add(MetadataReference.CreateFromFile(typeof(Result<,>).Assembly.Location)));

        var document = project.AddDocument("Test.cs", source);
        var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
        var invocation = root!
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(static node => node.Expression.ToString() == "MAsync");

        var diagnostic = Diagnostic.Create(
            DiscardedResultDescriptor,
            invocation.GetLocation(),
            "MAsync");

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await new DiscardedResultCodeFix().RegisterCodeFixesAsync(context).ConfigureAwait(false);
        return actions;
    }

    private static DiagnosticDescriptor GetDiscardedResultDescriptor()
    {
        var descriptorType = typeof(DiscardedResultAnalyzer).Assembly.GetType("FuncyTown.Analyzers.DiagnosticDescriptors");
        var field = descriptorType?.GetField("DiscardedResult", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        return field?.GetValue(null) as DiagnosticDescriptor
            ?? throw new InvalidOperationException("Could not resolve the FT0001 diagnostic descriptor.");
    }
}
