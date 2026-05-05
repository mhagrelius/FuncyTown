using System.Collections.Immutable;
using System.Linq;
using FuncyTown.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FuncyTown.Generators.Internal;

internal static class ErrorUnionDiscovery
{
    public static IncrementalValuesProvider<UnionProjection> CreateUnionProvider(SyntaxValueProvider syntaxProvider)
    {
        return syntaxProvider.ForAttributeWithMetadataName(
            "FuncyTown.ErrorUnionAttribute",
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, ct) => ProjectUnionOrDiagnose(ctx, ct));
    }

    public static IncrementalValuesProvider<CaseProjection> CreateCaseProvider(SyntaxValueProvider syntaxProvider)
    {
        return syntaxProvider.ForAttributeWithMetadataName(
            "FuncyTown.ErrorCaseAttribute",
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, ct) => ProjectCaseOrDiagnose(ctx, ct));
    }

    public static ImmutableArray<ErrorUnionModel> PairUnionsAndCases(
        ImmutableArray<UnionProjection> unionList,
        ImmutableArray<CaseProjection> caseList)
    {
        var unions = unionList
            .Where(static union => union.Model is not null)
            .Select(static union => union.Model!)
            .ToImmutableArray();
        var cases = caseList
            .Where(static @case => @case.Model is not null)
            .ToImmutableArray();

        return unions
            .Select(union =>
            {
                var members = cases
                    .Where(@case => HasAncestor(@case.AncestorFullyQualifiedNames, union.FullyQualifiedName))
                    .Select(static @case => @case.Model!)
                    .ToImmutableArray();
                return union with { Cases = new EquatableArray<ErrorCaseModel>(members) };
            })
            .ToImmutableArray();
    }

    public static string GetHintName(ErrorUnionModel model)
    {
        return $"{model.FullyQualifiedName.Replace("global::", string.Empty).Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_')}.Union.g.cs";
    }

    private static UnionProjection ProjectUnionOrDiagnose(GeneratorAttributeSyntaxContext ctx, System.Threading.CancellationToken ct)
    {
        _ = ct;

        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return default;
        }

        if (ctx.TargetNode is not TypeDeclarationSyntax declaration)
        {
            return default;
        }

        if (HasFileLocalContainingType(declaration))
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.FileLocalContainingTypeUnsupported,
                declaration.Identifier.GetLocation(),
                typeSymbol.ToDisplayString());
            return new UnionProjection(null, diagnostic);
        }

        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToDisplayString();

        return new UnionProjection(new ErrorUnionModel(
            Namespace: ns,
            TypeName: typeSymbol.Name,
            FullyQualifiedName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Accessibility: TypeProjectionHelpers.GetAccessibility(typeSymbol.DeclaredAccessibility),
            ContainingTypeDeclarations: TypeProjectionHelpers.GetContainingTypeDeclarations(declaration),
            Cases: EquatableArray<ErrorCaseModel>.Empty), null);
    }

    private static CaseProjection ProjectCaseOrDiagnose(GeneratorAttributeSyntaxContext ctx, System.Threading.CancellationToken ct)
    {
        _ = ct;

        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return default;
        }

        if (ctx.TargetNode is not TypeDeclarationSyntax declaration)
        {
            return default;
        }

        if (HasFileLocalContainingType(declaration))
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.FileLocalContainingTypeUnsupported,
                declaration.Identifier.GetLocation(),
                typeSymbol.ToDisplayString());
            return new CaseProjection(null, string.Empty, diagnostic);
        }

        var ancestors = GetAncestorFullyQualifiedNames(typeSymbol);
        var model = new ErrorCaseModel(
            FullyQualifiedName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            CaseTypeName: typeSymbol.Name,
            SimpleName: typeSymbol.Name,
            Accessibility: GetEffectiveAccessibility(typeSymbol),
            BaseFullyQualifiedName: typeSymbol.BaseType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty,
            IsSealed: typeSymbol.IsSealed,
            ConstructorParameters: GetPrimaryConstructorParameters(ctx.SemanticModel, declaration));

        return new CaseProjection(
            model,
            ancestors,
            null);
    }

    private static bool HasFileLocalContainingType(TypeDeclarationSyntax declaration)
    {
        return declaration
            .Ancestors()
            .OfType<TypeDeclarationSyntax>()
            .Any(static type => type.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.FileKeyword)));
    }

    private static string GetEffectiveAccessibility(INamedTypeSymbol typeSymbol)
    {
        var hasInternalBoundary = false;
        for (INamedTypeSymbol? current = typeSymbol; current is not null; current = current.ContainingType)
        {
            switch (current.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    break;
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    hasInternalBoundary = true;
                    break;
                default:
                    return "private";
            }
        }

        return hasInternalBoundary ? "internal" : "public";
    }

    private static string GetAncestorFullyQualifiedNames(INamedTypeSymbol typeSymbol)
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        for (var baseType = typeSymbol.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            builder.Add(baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        return string.Join("\n", builder);
    }

    private static bool HasAncestor(string ancestorFullyQualifiedNames, string fullyQualifiedName)
    {
        if (ancestorFullyQualifiedNames.Length == 0)
        {
            return false;
        }

        return ancestorFullyQualifiedNames
            .Split('\n')
            .Any(name => name == fullyQualifiedName);
    }

    private static EquatableArray<ConstructorParameter> GetPrimaryConstructorParameters(
        SemanticModel semanticModel,
        TypeDeclarationSyntax declaration)
    {
        var parameterList = GetPrimaryConstructorParameterList(declaration);
        if (parameterList is null)
        {
            return EquatableArray<ConstructorParameter>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<ConstructorParameter>(parameterList.Parameters.Count);
        foreach (var parameter in parameterList.Parameters)
        {
            if (parameter.Type is null)
            {
                continue;
            }

            var parameterType = semanticModel.GetTypeInfo(parameter.Type).Type;
            builder.Add(new ConstructorParameter(
                parameterType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? parameter.Type.ToString(),
                parameter.Identifier.ValueText));
        }

        return new EquatableArray<ConstructorParameter>(builder.ToImmutable());
    }

    private static ParameterListSyntax? GetPrimaryConstructorParameterList(TypeDeclarationSyntax declaration)
    {
        return declaration switch
        {
            ClassDeclarationSyntax classDeclaration => classDeclaration.ParameterList,
            RecordDeclarationSyntax recordDeclaration => recordDeclaration.ParameterList,
            _ => null,
        };
    }
}

internal readonly record struct UnionProjection(ErrorUnionModel? Model, Diagnostic? Diagnostic);

internal readonly struct CaseProjection : System.IEquatable<CaseProjection>
{
    public CaseProjection(ErrorCaseModel? model, string ancestorFullyQualifiedNames, Diagnostic? diagnostic)
    {
        Model = model;
        AncestorFullyQualifiedNames = ancestorFullyQualifiedNames;
        Diagnostic = diagnostic;
    }

    public ErrorCaseModel? Model { get; }

    public string AncestorFullyQualifiedNames { get; }

    public Diagnostic? Diagnostic { get; }

    public static bool operator ==(CaseProjection left, CaseProjection right) => left.Equals(right);

    public static bool operator !=(CaseProjection left, CaseProjection right) => !left.Equals(right);

    public bool Equals(CaseProjection other)
    {
        if (!DiagnosticsEqual(Diagnostic, other.Diagnostic)
            || AncestorFullyQualifiedNames != other.AncestorFullyQualifiedNames)
        {
            return false;
        }

        var model = Model;
        var otherModel = other.Model;
        if (model is null || otherModel is null)
        {
            return model is null && otherModel is null;
        }

        return model.FullyQualifiedName == otherModel.FullyQualifiedName
            && model.CaseTypeName == otherModel.CaseTypeName
            && model.SimpleName == otherModel.SimpleName
            && model.Accessibility == otherModel.Accessibility
            && model.BaseFullyQualifiedName == otherModel.BaseFullyQualifiedName
            && model.IsSealed == otherModel.IsSealed
            && ConstructorParametersEqual(model.ConstructorParameters, otherModel.ConstructorParameters);
    }

    public override bool Equals(object? obj) => obj is CaseProjection other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = CombineHash(hash, Diagnostic?.Id ?? string.Empty);
            hash = CombineHash(hash, Diagnostic?.GetMessage() ?? string.Empty);
            hash = CombineHash(hash, AncestorFullyQualifiedNames);
            if (Model is not null)
            {
                hash = CombineHash(hash, Model.FullyQualifiedName);
                hash = CombineHash(hash, Model.CaseTypeName);
                hash = CombineHash(hash, Model.SimpleName);
                hash = CombineHash(hash, Model.Accessibility);
                hash = CombineHash(hash, Model.BaseFullyQualifiedName);
                hash = (hash * 31) + Model.IsSealed.GetHashCode();
                foreach (var parameter in Model.ConstructorParameters)
                {
                    hash = CombineHash(hash, parameter.TypeFullyQualifiedName);
                    hash = CombineHash(hash, parameter.Name);
                }
            }

            return hash;
        }
    }

    private static bool ConstructorParametersEqual(
        EquatableArray<ConstructorParameter> left,
        EquatableArray<ConstructorParameter> right)
    {
        return left.Equals(right);
    }

    private static int CombineHash(int current, string value)
    {
        return (current * 31) + System.StringComparer.Ordinal.GetHashCode(value);
    }

    private static bool DiagnosticsEqual(Diagnostic? left, Diagnostic? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return left.Id == right.Id
            && left.GetMessage() == right.GetMessage();
    }
}
