using System.Collections.Immutable;
using FuncyTown.Analyzers.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FuncyTown.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ChainCrossesErrorTypesAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> ChainMethodNames =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "Bind",
            "Then",
            "AndThen",
            "SelectMany",
            "BindAsync",
            "ThenAsync",
            "AndThenAsync",
            "SelectManyAsync");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ChainCrossesErrorTypes);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var knownTypes = new KnownTypes(compilationContext.Compilation);
            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeInvocation(
                    syntaxContext,
                    knownTypes,
                    (InvocationExpressionSyntax)syntaxContext.Node),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        KnownTypes knownTypes,
        InvocationExpressionSyntax invocation)
    {
        if (!TryGetMemberInvocation(invocation, out var receiver, out var methodNameSyntax, out var methodName)
            || !ChainMethodNames.Contains(methodName))
        {
            return;
        }

        if (!TryGetResultErrorType(
                context.SemanticModel.GetTypeInfo(receiver, context.CancellationToken).Type,
                knownTypes,
                out var receiverErrorType)
            || receiverErrorType is null)
        {
            return;
        }

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (ReturnedErrorTypeCrosses(
                    argument.Expression,
                    context.SemanticModel,
                    knownTypes,
                    context.CancellationToken,
                    receiverErrorType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ChainCrossesErrorTypes,
                    methodNameSyntax.GetLocation(),
                    methodName));
                return;
            }
        }
    }

    private static bool TryGetMemberInvocation(
        InvocationExpressionSyntax invocation,
        out ExpressionSyntax receiver,
        out SimpleNameSyntax methodNameSyntax,
        out string methodName)
    {
        receiver = null!;
        methodNameSyntax = null!;
        methodName = string.Empty;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        receiver = memberAccess.Expression;
        methodNameSyntax = memberAccess.Name;
        methodName = memberAccess.Name.Identifier.ValueText;
        return methodName.Length > 0;
    }

    private static bool ReturnedErrorTypeCrosses(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        KnownTypes knownTypes,
        CancellationToken cancellationToken,
        ITypeSymbol receiverErrorType)
    {
        return expression switch
        {
            ParenthesizedLambdaExpressionSyntax lambda => ReturnedErrorTypeCrosses(lambda, semanticModel, knownTypes, cancellationToken, receiverErrorType),
            SimpleLambdaExpressionSyntax lambda => ReturnedErrorTypeCrosses(lambda, semanticModel, knownTypes, cancellationToken, receiverErrorType),
            AnonymousMethodExpressionSyntax anonymousMethod => ReturnedErrorTypeCrosses(anonymousMethod.Block, semanticModel, knownTypes, cancellationToken, receiverErrorType),
            _ => MethodReturnOrExpressionErrorTypeCrosses(expression, semanticModel, knownTypes, cancellationToken, receiverErrorType),
        };
    }

    private static bool ReturnedErrorTypeCrosses(
        LambdaExpressionSyntax lambda,
        SemanticModel semanticModel,
        KnownTypes knownTypes,
        CancellationToken cancellationToken,
        ITypeSymbol receiverErrorType)
    {
        if (lambda.ExpressionBody is not null)
        {
            return ExpressionErrorTypeCrosses(lambda.ExpressionBody, semanticModel, knownTypes, cancellationToken, receiverErrorType);
        }

        return ReturnedErrorTypeCrosses(lambda.Block, semanticModel, knownTypes, cancellationToken, receiverErrorType);
    }

    private static bool ReturnedErrorTypeCrosses(
        BlockSyntax? block,
        SemanticModel semanticModel,
        KnownTypes knownTypes,
        CancellationToken cancellationToken,
        ITypeSymbol receiverErrorType)
    {
        if (block is null)
        {
            return false;
        }

        foreach (var returnStatement in block
                     .DescendantNodes(static node => !IsNestedReturnScope(node))
                     .OfType<ReturnStatementSyntax>())
        {
            if (returnStatement.Expression is null)
            {
                continue;
            }

            if (TryGetExpressionErrorType(
                    returnStatement.Expression,
                    semanticModel,
                    knownTypes,
                    cancellationToken,
                    out var errorType)
                && errorType is not null
                && !KnownTypes.IsSame(receiverErrorType, errorType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNestedReturnScope(SyntaxNode node)
    {
        return node is LambdaExpressionSyntax
            or AnonymousMethodExpressionSyntax
            or LocalFunctionStatementSyntax;
    }

    private static bool MethodReturnOrExpressionErrorTypeCrosses(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        KnownTypes knownTypes,
        CancellationToken cancellationToken,
        ITypeSymbol receiverErrorType)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
        if (symbolInfo.Symbol is IMethodSymbol method)
        {
            return ResultErrorTypeCrosses(method.ReturnType, knownTypes, receiverErrorType);
        }

        foreach (var candidate in symbolInfo.CandidateSymbols)
        {
            if (candidate is IMethodSymbol candidateMethod
                && ResultErrorTypeCrosses(candidateMethod.ReturnType, knownTypes, receiverErrorType))
            {
                return true;
            }
        }

        return ExpressionErrorTypeCrosses(expression, semanticModel, knownTypes, cancellationToken, receiverErrorType);
    }

    private static bool ExpressionErrorTypeCrosses(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        KnownTypes knownTypes,
        CancellationToken cancellationToken,
        ITypeSymbol receiverErrorType)
    {
        return TryGetExpressionErrorType(expression, semanticModel, knownTypes, cancellationToken, out var errorType)
            && errorType is not null
            && !KnownTypes.IsSame(receiverErrorType, errorType);
    }

    private static bool ResultErrorTypeCrosses(
        ITypeSymbol? type,
        KnownTypes knownTypes,
        ITypeSymbol receiverErrorType)
    {
        return TryGetResultErrorType(type, knownTypes, out var errorType)
            && errorType is not null
            && !KnownTypes.IsSame(receiverErrorType, errorType);
    }

    private static bool TryGetExpressionErrorType(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        KnownTypes knownTypes,
        CancellationToken cancellationToken,
        out ITypeSymbol? errorType)
    {
        var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
        return TryGetResultErrorType(type, knownTypes, out errorType);
    }

    private static bool TryGetResultErrorType(
        ITypeSymbol? type,
        KnownTypes knownTypes,
        out ITypeSymbol? errorType)
    {
        errorType = null;

        if (knownTypes.TryGetResultTypes(type, out _, out errorType)
            || knownTypes.TryGetTaskResultTypes(type, out _, out _, out errorType))
        {
            return errorType is not null;
        }

        return false;
    }
}
