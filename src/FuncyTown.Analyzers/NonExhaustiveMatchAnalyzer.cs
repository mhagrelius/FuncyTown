using System.Collections.Immutable;
using FuncyTown.Analyzers.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FuncyTown.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonExhaustiveMatchAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.NonExhaustiveMatch);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var knownTypes = new KnownTypes(compilationContext.Compilation);
            var visibleHandlerCacheLock = new object();
            var visibleHandlerCache = new Dictionary<ISymbol, ImmutableArray<string>>(SymbolEqualityComparer.Default);
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(
                    operationContext,
                    knownTypes,
                    visibleHandlerCache,
                    visibleHandlerCacheLock,
                    (IInvocationOperation)operationContext.Operation),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        KnownTypes knownTypes,
        Dictionary<ISymbol, ImmutableArray<string>> visibleHandlerCache,
        object visibleHandlerCacheLock,
        IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.Name != "Match"
            || invocation.Arguments.Length != 2
            || !TryGetReceiverType(invocation, out var receiverType)
            || !knownTypes.TryGetResultTypes(receiverType, out _, out var errorType)
            || errorType is not INamedTypeSymbol namedErrorType
            || !knownTypes.HasErrorUnionAttribute(namedErrorType)
            || !IsOrdinaryResultMatch(invocation, namedErrorType))
        {
            return;
        }

        var missingCases = GetVisibleUnionHandlers(
            context.Compilation,
            namedErrorType,
            knownTypes,
            visibleHandlerCache,
            visibleHandlerCacheLock,
            context.CancellationToken);
        if (missingCases.Length == 0)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NonExhaustiveMatch,
            GetMethodNameLocation(invocation),
            namedErrorType.Name,
            string.Join(", ", missingCases)));
    }

    private static bool TryGetReceiverType(IInvocationOperation invocation, out ITypeSymbol? receiverType)
    {
        receiverType = invocation.Instance?.Type;
        return receiverType is not null;
    }

    private static bool IsOrdinaryResultMatch(IInvocationOperation invocation, INamedTypeSymbol errorType)
    {
        if (invocation.TargetMethod.Parameters.Length != 2)
        {
            return false;
        }

        var onSuccessType = invocation.TargetMethod.Parameters[0].Type as INamedTypeSymbol;
        var onFailureType = invocation.TargetMethod.Parameters[1].Type as INamedTypeSymbol;
        if (onSuccessType?.DelegateInvokeMethod is not { } onSuccessInvoke
            || onFailureType?.DelegateInvokeMethod is not { } onFailureInvoke)
        {
            return false;
        }

        return onSuccessInvoke.Parameters.Length <= 1
            && onFailureInvoke.Parameters.Length == 1
            && KnownTypes.IsSame(onFailureInvoke.Parameters[0].Type, errorType);
    }

    private static ImmutableArray<string> GetVisibleUnionHandlers(
        Compilation compilation,
        INamedTypeSymbol errorType,
        KnownTypes knownTypes,
        Dictionary<ISymbol, ImmutableArray<string>> visibleHandlerCache,
        object visibleHandlerCacheLock,
        CancellationToken cancellationToken)
    {
        lock (visibleHandlerCacheLock)
        {
            if (visibleHandlerCache.TryGetValue(errorType, out var cached))
            {
                return cached;
            }
        }

        var cases = ImmutableArray.CreateBuilder<string>();

        AddVisibleCases(compilation.GlobalNamespace, errorType, knownTypes, cases, cancellationToken);

        foreach (var nestedType in errorType.GetTypeMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (nestedType.Name == "Many")
            {
                cases.Add("Many");
                break;
            }
        }

        var result = cases.ToImmutable();
        lock (visibleHandlerCacheLock)
        {
            visibleHandlerCache[errorType] = result;
        }

        return result;
    }

    private static void AddVisibleCases(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol errorType,
        KnownTypes knownTypes,
        ImmutableArray<string>.Builder cases,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            AddVisibleCases(type, errorType, knownTypes, cases, cancellationToken);
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            AddVisibleCases(childNamespace, errorType, knownTypes, cases, cancellationToken);
        }
    }

    private static void AddVisibleCases(
        INamedTypeSymbol candidate,
        INamedTypeSymbol errorType,
        KnownTypes knownTypes,
        ImmutableArray<string>.Builder cases,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (knownTypes.HasErrorCaseAttribute(candidate)
            && DerivesFrom(candidate, errorType)
            && IsVisibleTo(errorType, candidate))
        {
            cases.Add(candidate.Name);
        }

        foreach (var nestedType in candidate.GetTypeMembers())
        {
            AddVisibleCases(nestedType, errorType, knownTypes, cases, cancellationToken);
        }
    }

    private static bool DerivesFrom(INamedTypeSymbol candidate, INamedTypeSymbol baseType)
    {
        for (var current = candidate.BaseType; current is not null; current = current.BaseType)
        {
            if (KnownTypes.IsSame(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsVisibleTo(INamedTypeSymbol unionType, INamedTypeSymbol candidate)
    {
        var caseAccessibility = GetEffectiveAccessibility(candidate);
        if (IsPubliclyExposedUnion(unionType))
        {
            return caseAccessibility == "public";
        }

        return caseAccessibility is "public" or "internal";
    }

    private static bool IsPubliclyExposedUnion(INamedTypeSymbol unionType)
    {
        if (unionType.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        for (var containingType = unionType.ContainingType; containingType is not null; containingType = containingType.ContainingType)
        {
            if (containingType.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
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

    private static Location GetMethodNameLocation(IInvocationOperation invocation)
    {
        if (invocation.Syntax is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax memberAccess,
            })
        {
            return memberAccess.Name.GetLocation();
        }

        return invocation.Syntax.GetLocation();
    }
}
