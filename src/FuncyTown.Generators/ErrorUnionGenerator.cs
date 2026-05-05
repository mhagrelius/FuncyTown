using FuncyTown.Generators.Emitters;
using FuncyTown.Generators.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FuncyTown.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class ErrorUnionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var unions = ErrorUnionDiscovery.CreateUnionProvider(context.SyntaxProvider);
        var cases = ErrorUnionDiscovery.CreateCaseProvider(context.SyntaxProvider);
        var combined = unions.Collect().Combine(cases.Collect());

        context.RegisterSourceOutput(combined, static (spc, pair) =>
        {
            var (unionList, caseList) = pair;
            foreach (var union in unionList)
            {
                if (union.Diagnostic is not null)
                {
                    spc.ReportDiagnostic(union.Diagnostic);
                }
            }

            foreach (var @case in caseList)
            {
                if (@case.Diagnostic is not null)
                {
                    spc.ReportDiagnostic(@case.Diagnostic);
                }
            }

            foreach (var union in ErrorUnionDiscovery.PairUnionsAndCases(unionList, caseList))
            {
                spc.AddSource(ErrorUnionDiscovery.GetHintName(union), ErrorUnionEmitter.Emit(union));
            }
        });
    }
}
