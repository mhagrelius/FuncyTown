using FuncyTown.Analyzers.Tests.Verifiers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FuncyTown.Analyzers.Tests;

public sealed class VerifierScaffoldTests
{
    [Fact]
    public Task Analyzer_verifier_runs_without_diagnostics()
    {
        const string source = """
            internal sealed class C
            {
            }
            """;

        return CSharpAnalyzerVerifier<EmptyDiagnosticAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
