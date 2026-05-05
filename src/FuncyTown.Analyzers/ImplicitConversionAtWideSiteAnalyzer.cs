using System.Collections.Immutable;
using FuncyTown.Analyzers.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FuncyTown.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ImplicitConversionAtWideSiteAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ImplicitConversionAtWideSite);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var knownTypes = new KnownTypes(compilationContext.Compilation);
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeConversion(operationContext, knownTypes),
                OperationKind.Conversion);
        });
    }

    private static void AnalyzeConversion(OperationAnalysisContext context, KnownTypes knownTypes)
    {
        var conversion = (IConversionOperation)context.Operation;
        if (!conversion.IsImplicit
            || conversion.Type is null
            || !IsWideTargetType(conversion.Type)
            || !knownTypes.TryGetResultTypes(conversion.Operand.Type, out _, out _))
        {
            return;
        }

        // Argument position is benign: `Console.WriteLine(result)`, `string.Format("{0}", result)`,
        // `Debug.Assert(condition, "msg", result)`, `Logger.Log(...)`, or any user-defined method
        // that takes `object` as a system-boundary input. The library can't tell intent there;
        // flagging at Info severity is noisier than useful. Assignments, returns, and direct
        // widening still fire.
        if (IsInsideArgument(conversion))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ImplicitConversionAtWideSite,
            conversion.Syntax.GetLocation(),
            GetTargetTypeName(conversion.Type)));
    }

    private static bool IsWideTargetType(ITypeSymbol targetType)
    {
        return targetType.TypeKind == TypeKind.Dynamic
            || targetType.SpecialType == SpecialType.System_Object
            || targetType.SpecialType == SpecialType.System_ValueType;
    }

    private static bool IsInsideArgument(IOperation operation)
    {
        for (var current = operation.Parent; current is not null; current = current.Parent)
        {
            if (current is IArgumentOperation)
            {
                return true;
            }

            // Stop when we reach the enclosing call site — going higher would cross
            // into the surrounding expression.
            if (current is IInvocationOperation or IObjectCreationOperation)
            {
                return false;
            }
        }

        return false;
    }

    private static string GetTargetTypeName(ITypeSymbol targetType)
    {
        if (targetType.TypeKind == TypeKind.Dynamic)
        {
            return "dynamic";
        }

        return targetType.SpecialType == SpecialType.System_ValueType
            ? "ValueType"
            : "object";
    }
}
