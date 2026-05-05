using System.Collections.Immutable;
using System.Linq;
using FuncyTown.Generators.Internal;
using FuncyTown.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FuncyTown.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class ResultAliasGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var twoArg = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "FuncyTown.ResultAttribute`2",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, ct) => ProjectOrDiagnose(ctx, isVoidSuccess: false, ct));

        var oneArg = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "FuncyTown.ResultAttribute`1",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, ct) => ProjectOrDiagnose(ctx, isVoidSuccess: true, ct));

        var aliases = twoArg.Collect().Combine(oneArg.Collect());
        var unions = ErrorUnionDiscovery.CreateUnionProvider(context.SyntaxProvider);
        var cases = ErrorUnionDiscovery.CreateCaseProvider(context.SyntaxProvider);
        var errorUnions = unions.Collect().Combine(cases.Collect());
        var combined = aliases.Combine(errorUnions);

        context.RegisterSourceOutput(combined, static (spc, pair) =>
        {
            var ((twoArg, oneArg), (unionList, caseList)) = pair;
            var entries = twoArg.Concat(oneArg).ToImmutableArray();
            var unions = ErrorUnionDiscovery.PairUnionsAndCases(unionList, caseList);

            foreach (var entry in entries)
            {
                if (entry.Diagnostic is not null)
                {
                    spc.ReportDiagnostic(entry.Diagnostic);
                }
            }

            var families = entries
                .Where(static entry => entry.Model is not null)
                .Select(static entry => entry.Model!)
                .GroupBy(static model => model.ErrorTypeFullyQualifiedName)
                .Select(group => new ResultAliasFamily(
                    group.Key,
                    new EquatableArray<ResultAliasModel>(group.ToImmutableArray()),
                    unions.FirstOrDefault(union => union.FullyQualifiedName == group.Key)))
                .ToImmutableArray();

            foreach (var family in families)
            {
                foreach (var member in family.Members)
                {
                    var source = Emitters.ResultAliasEmitter.Emit(member, family);
                    spc.AddSource(member.HintName, source);
                    if (Emitters.AliasTaskExtensionEmitter.CanEmit(member))
                    {
                        var taskExtensionSource = Emitters.AliasTaskExtensionEmitter.Emit(member, family);
                        spc.AddSource(Emitters.AliasTaskExtensionEmitter.GetHintName(member), taskExtensionSource);
                    }
                }
            }
        });
    }

    private readonly record struct ProjectionResult(ResultAliasModel? Model, Diagnostic? Diagnostic);

    private static ProjectionResult ProjectOrDiagnose(
        GeneratorAttributeSyntaxContext ctx,
        bool isVoidSuccess,
        System.Threading.CancellationToken ct)
    {
        _ = ct;

        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return default;
        }

        if (ctx.TargetNode is not TypeDeclarationSyntax decl)
        {
            return default;
        }

        if (!decl.Modifiers.Any(static m => m.ValueText == "partial"))
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.MustBePartial,
                decl.Identifier.GetLocation(),
                typeSymbol.ToDisplayString());
            return new ProjectionResult(null, diagnostic);
        }

        if (decl.Ancestors().OfType<TypeDeclarationSyntax>().Any(static type => type.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.FileKeyword))))
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.FileLocalContainingTypeUnsupported,
                decl.Identifier.GetLocation(),
                typeSymbol.ToDisplayString());
            return new ProjectionResult(null, diagnostic);
        }

        var attr = ctx.Attributes.FirstOrDefault();
        if (attr is null)
        {
            return default;
        }

        ITypeSymbol? valueType;
        ITypeSymbol? errorType;
        if (isVoidSuccess)
        {
            valueType = ctx.SemanticModel.Compilation.GetTypeByMetadataName("FuncyTown.Unit");
            errorType = attr.AttributeClass?.TypeArguments.FirstOrDefault();
        }
        else
        {
            valueType = attr.AttributeClass?.TypeArguments.ElementAtOrDefault(0);
            errorType = attr.AttributeClass?.TypeArguments.ElementAtOrDefault(1);
        }

        if (valueType is null || errorType is null)
        {
            return default;
        }

        var implicitEnabled = true;
        var jsonConverterEnabled = true;
        var isClassKind = false;
        foreach (var named in attr.NamedArguments)
        {
            if (named.Key == "Implicit" && named.Value.Value is bool implicitValue)
            {
                implicitEnabled = implicitValue;
            }
            else if (named.Key == "Json" && named.Value.Value is bool jsonValue)
            {
                jsonConverterEnabled = jsonValue;
            }
            else if (named.Key == "Kind" && named.Value.Value is int kind)
            {
                isClassKind = kind == 1;
            }
        }

        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToDisplayString();

        var model = new ResultAliasModel(
            AliasNamespace: ns,
            AliasName: typeSymbol.Name,
            AliasAccessibility: TypeProjectionHelpers.GetAccessibility(typeSymbol.DeclaredAccessibility),
            AliasFullyQualifiedName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ValueTypeFullyQualifiedName: valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ErrorTypeFullyQualifiedName: errorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ContainingTypeDeclarations: TypeProjectionHelpers.GetContainingTypeDeclarations(decl),
            IsClassKind: isClassKind,
            ImplicitConversionsEnabled: implicitEnabled,
            JsonConverterEnabled: jsonConverterEnabled,
            IsVoidSuccess: isVoidSuccess);
        return new ProjectionResult(model, null);
    }
}
