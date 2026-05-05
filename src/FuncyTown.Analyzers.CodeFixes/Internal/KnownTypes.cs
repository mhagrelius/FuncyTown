using Microsoft.CodeAnalysis;

namespace FuncyTown.Analyzers.CodeFixes.Internal;

internal sealed class KnownTypes
{
    private readonly Compilation _compilation;

    private INamedTypeSymbol? _result;
    private INamedTypeSymbol? _resultAttribute2;
    private INamedTypeSymbol? _resultAttribute1;
    private INamedTypeSymbol? _task;
    private INamedTypeSymbol? _unit;

    public KnownTypes(Compilation compilation)
    {
        _compilation = compilation;
    }

    public INamedTypeSymbol? Result => _result ??= _compilation.GetTypeByMetadataName("FuncyTown.Result`2");

    public INamedTypeSymbol? ResultAttribute2 => _resultAttribute2 ??= _compilation.GetTypeByMetadataName("FuncyTown.ResultAttribute`2");

    public INamedTypeSymbol? ResultAttribute1 => _resultAttribute1 ??= _compilation.GetTypeByMetadataName("FuncyTown.ResultAttribute`1");

    public INamedTypeSymbol? Task => _task ??= _compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

    public INamedTypeSymbol? Unit => _unit ??= _compilation.GetTypeByMetadataName("FuncyTown.Unit");

    public bool TryGetResultTypes(
        ITypeSymbol? type,
        out ITypeSymbol? successType,
        out ITypeSymbol? errorType)
    {
        successType = null;
        errorType = null;

        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (Result is not null
            && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, Result)
            && named.TypeArguments.Length == 2)
        {
            successType = named.TypeArguments[0];
            errorType = named.TypeArguments[1];
            return true;
        }

        foreach (var attribute in named.GetAttributes())
        {
            if (attribute.AttributeClass is null)
            {
                continue;
            }

            var attributeDefinition = attribute.AttributeClass.OriginalDefinition;
            if (ResultAttribute2 is not null
                && SymbolEqualityComparer.Default.Equals(attributeDefinition, ResultAttribute2)
                && attribute.AttributeClass.TypeArguments.Length == 2)
            {
                successType = SubstituteAliasTypeArguments(named, attribute.AttributeClass.TypeArguments[0]);
                errorType = SubstituteAliasTypeArguments(named, attribute.AttributeClass.TypeArguments[1]);
                return true;
            }

            if (ResultAttribute1 is not null
                && SymbolEqualityComparer.Default.Equals(attributeDefinition, ResultAttribute1)
                && attribute.AttributeClass.TypeArguments.Length == 1)
            {
                successType = Unit;
                errorType = SubstituteAliasTypeArguments(named, attribute.AttributeClass.TypeArguments[0]);
                return successType is not null;
            }
        }

        return false;
    }

    public bool TryGetTaskResultTypes(
        ITypeSymbol? type,
        out ITypeSymbol? taskResultType,
        out ITypeSymbol? successType,
        out ITypeSymbol? errorType)
    {
        taskResultType = null;
        successType = null;
        errorType = null;

        if (type is not INamedTypeSymbol named
            || Task is null
            || !SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, Task)
            || named.TypeArguments.Length != 1)
        {
            return false;
        }

        taskResultType = named.TypeArguments[0];
        return TryGetResultTypes(taskResultType, out successType, out errorType);
    }

    private ITypeSymbol SubstituteAliasTypeArguments(INamedTypeSymbol aliasType, ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            return TryGetSubstitutedTypeParameter(aliasType, typeParameter) ?? type;
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            var substitutedElement = SubstituteAliasTypeArguments(aliasType, arrayType.ElementType);
            return SymbolEqualityComparer.Default.Equals(substitutedElement, arrayType.ElementType)
                ? type
                : _compilation.CreateArrayTypeSymbol(substitutedElement, arrayType.Rank);
        }

        if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
        {
            var substituted = new ITypeSymbol[namedType.TypeArguments.Length];
            var changed = false;
            for (var i = 0; i < namedType.TypeArguments.Length; i++)
            {
                substituted[i] = SubstituteAliasTypeArguments(aliasType, namedType.TypeArguments[i]);
                changed |= !SymbolEqualityComparer.Default.Equals(substituted[i], namedType.TypeArguments[i]);
            }

            return changed ? namedType.ConstructedFrom.Construct(substituted) : type;
        }

        return type;
    }

    private static ITypeSymbol? TryGetSubstitutedTypeParameter(
        INamedTypeSymbol aliasType,
        ITypeParameterSymbol typeParameter)
    {
        for (INamedTypeSymbol? current = aliasType; current is not null; current = current.ContainingType)
        {
            var originalDefinition = current.OriginalDefinition;
            for (var i = 0; i < originalDefinition.TypeParameters.Length; i++)
            {
                if (SymbolEqualityComparer.Default.Equals(originalDefinition.TypeParameters[i], typeParameter)
                    || SymbolEqualityComparer.Default.Equals(current.TypeParameters[i], typeParameter))
                {
                    return current.TypeArguments[i];
                }
            }
        }

        return null;
    }
}
