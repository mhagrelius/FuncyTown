using System.Collections.Immutable;
using FuncyTown.Analyzers.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FuncyTown.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DiscardedResultAnalyzer : DiagnosticAnalyzer
{
    // Tap-style chain method names (sync + async). When the terminal call in a chain is one
    // of these and its receiver is itself a Result/alias, the user's intent is the side
    // effect — the unchanged Result passing through is by design, not a discard.
    private static readonly ImmutableHashSet<string> TapStyleMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "OnSuccess",
        "OnFailure",
        "Tap",
        "TapError",
        "IfSuccessful",
        "IfFailed",
        "OnSuccessAsync",
        "OnFailureAsync",
        "TapAsync",
        "TapErrorAsync",
        "IfSuccessfulAsync",
        "IfFailedAsync");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DiscardedResult);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var knownTypes = new KnownTypes(compilationContext.Compilation);
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeExpressionStatement(
                    operationContext,
                    knownTypes,
                    (IExpressionStatementOperation)operationContext.Operation),
                OperationKind.ExpressionStatement);
        });
    }

    private static void AnalyzeExpressionStatement(
        OperationAnalysisContext context,
        KnownTypes knownTypes,
        IExpressionStatementOperation expressionStatement)
    {
        if (expressionStatement.Operation is ISimpleAssignmentOperation or ICompoundAssignmentOperation)
        {
            return;
        }

        var expression = Unwrap(expressionStatement.Operation);
        if (expression is IAwaitOperation awaitOperation)
        {
            AnalyzeReturnedType(context, knownTypes, awaitOperation.Type, awaitOperation.Operation, awaitOperation.Syntax.GetLocation());
            return;
        }

        AnalyzeReturnedType(context, knownTypes, expression.Type, expression, expression.Syntax.GetLocation());
    }

    private static void AnalyzeReturnedType(
        OperationAnalysisContext context,
        KnownTypes knownTypes,
        ITypeSymbol? type,
        IOperation expression,
        Location location)
    {
        if (!knownTypes.TryGetResultTypes(type, out _, out _)
            && !knownTypes.TryGetTaskResultTypes(type, out _, out _, out _))
        {
            return;
        }

        if (IsTerminalTapStyleChainStep(expression, knownTypes))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DiscardedResult,
            location,
            GetOperationName(expression)));
    }

    private static bool IsTerminalTapStyleChainStep(IOperation expression, KnownTypes knownTypes)
    {
        var unwrapped = Unwrap(expression);
        if (unwrapped is IAwaitOperation awaitOperation)
        {
            unwrapped = Unwrap(awaitOperation.Operation);
        }

        if (unwrapped is not IInvocationOperation invocation)
        {
            return false;
        }

        if (!TapStyleMethodNames.Contains(invocation.TargetMethod.Name))
        {
            return false;
        }

        // The "receiver" might be the instance (chain on Result<T,E>) or the first
        // parameter for an extension method (chain on Task<Result<T,E>>).
        var receiverType = invocation.Instance?.Type;
        if (receiverType is null && invocation.TargetMethod.IsExtensionMethod && invocation.Arguments.Length > 0)
        {
            receiverType = invocation.Arguments[0].Value.Type;
        }

        return knownTypes.TryGetResultTypes(receiverType, out _, out _)
            || knownTypes.TryGetTaskResultTypes(receiverType, out _, out _, out _);
    }

    private static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    private static string GetOperationName(IOperation operation)
    {
        operation = Unwrap(operation);
        return operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod.Name,
            IAwaitOperation awaitOperation => GetOperationName(awaitOperation.Operation),
            _ => operation.Syntax.ToString(),
        };
    }
}
