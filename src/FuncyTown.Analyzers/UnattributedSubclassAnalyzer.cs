using System.Collections.Immutable;
using FuncyTown.Analyzers.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FuncyTown.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnattributedSubclassAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.UnattributedSubclass);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var knownTypes = new KnownTypes(compilationContext.Compilation);
            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, knownTypes),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, KnownTypes knownTypes)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class
            || knownTypes.HasErrorCaseAttribute(type))
        {
            return;
        }

        var unionBase = FindErrorUnionBase(type, knownTypes, context.CancellationToken);
        if (unionBase is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UnattributedSubclass,
            GetPrimaryLocation(type),
            type.Name,
            unionBase.Name));
    }

    private static INamedTypeSymbol? FindErrorUnionBase(
        INamedTypeSymbol type,
        KnownTypes knownTypes,
        CancellationToken cancellationToken)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (knownTypes.HasErrorUnionAttribute(current))
            {
                return current;
            }
        }

        return null;
    }

    private static Location GetPrimaryLocation(INamedTypeSymbol type)
    {
        foreach (var location in type.Locations)
        {
            if (location.IsInSource)
            {
                return location;
            }
        }

        return type.Locations.Length > 0
            ? type.Locations[0]
            : Location.None;
    }
}
