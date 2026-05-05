using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace FuncyTown.Analyzers.Tests.Verifiers;

internal static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic()
    {
        return CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic();
    }

    public static DiagnosticResult Diagnostic(string diagnosticId)
    {
        return CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);
    }

    public static Task VerifyCodeFixAsync(string source, string fixedSource)
    {
        var test = new Test
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        return test.RunAsync(CancellationToken.None);
    }

    public static Task VerifyCodeFixAsync(string source, int codeActionIndex, string fixedSource)
    {
        var test = new Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionIndex = codeActionIndex,
        };

        return test.RunAsync(CancellationToken.None);
    }

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
    {
        var test = new Test
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        test.ExpectedDiagnostics.Add(expected);
        return test.RunAsync(CancellationToken.None);
    }

    internal sealed class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        public Test()
        {
            ReferenceAssemblies = ReferenceAssemblyResolver.Net10;
            TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
            TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Result<,>).Assembly.Location));
        }

        protected override ParseOptions CreateParseOptions()
        {
            return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion.Latest);
        }

        protected override IEnumerable<Type> GetSourceGenerators()
        {
            return
            [
                typeof(Generators.ResultAliasGenerator),
                typeof(Generators.ErrorUnionGenerator),
            ];
        }
    }
}
