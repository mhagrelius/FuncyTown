using Microsoft.CodeAnalysis;

namespace FuncyTown.Analyzers.Internal;

internal sealed class KnownTypes
{
    private readonly Compilation _compilation;

    private INamedTypeSymbol? _result;
    private INamedTypeSymbol? _resultAttribute2;
    private INamedTypeSymbol? _resultAttribute1;
    private INamedTypeSymbol? _errorUnionAttribute;
    private INamedTypeSymbol? _errorCaseAttribute;
    private INamedTypeSymbol? _iError;
    private INamedTypeSymbol? _iCombinableError;
    private INamedTypeSymbol? _task;
    private INamedTypeSymbol? _unit;

    public KnownTypes(Compilation compilation)
    {
        _compilation = compilation;
    }

    public INamedTypeSymbol? Result => _result ??= _compilation.GetTypeByMetadataName("FuncyTown.Result`2");

    public INamedTypeSymbol? ResultAttribute2 => _resultAttribute2 ??= _compilation.GetTypeByMetadataName("FuncyTown.ResultAttribute`2");

    public INamedTypeSymbol? ResultAttribute1 => _resultAttribute1 ??= _compilation.GetTypeByMetadataName("FuncyTown.ResultAttribute`1");

    public INamedTypeSymbol? ErrorUnionAttribute => _errorUnionAttribute ??= _compilation.GetTypeByMetadataName("FuncyTown.ErrorUnionAttribute");

    public INamedTypeSymbol? ErrorCaseAttribute => _errorCaseAttribute ??= _compilation.GetTypeByMetadataName("FuncyTown.ErrorCaseAttribute");

    public INamedTypeSymbol? IError => _iError ??= _compilation.GetTypeByMetadataName("FuncyTown.IError");

    public INamedTypeSymbol? ICombinableError => _iCombinableError ??= _compilation.GetTypeByMetadataName("FuncyTown.ICombinableError`1");

    public INamedTypeSymbol? Task => _task ??= _compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

    public INamedTypeSymbol? Unit => _unit ??= _compilation.GetTypeByMetadataName("FuncyTown.Unit");

    public bool IsResultType(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named
            && Result is not null
            && IsSame(named.OriginalDefinition, Result);
    }

    public bool IsResultAlias(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named
            && (HasAttribute(named, ResultAttribute2) || HasAttribute(named, ResultAttribute1));
    }

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

        if (IsResultType(named) && named.TypeArguments.Length == 2)
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
                && IsSame(attributeDefinition, ResultAttribute2)
                && attribute.AttributeClass.TypeArguments.Length == 2)
            {
                successType = SubstituteAliasTypeArguments(named, attribute.AttributeClass.TypeArguments[0]);
                errorType = SubstituteAliasTypeArguments(named, attribute.AttributeClass.TypeArguments[1]);
                return true;
            }

            if (ResultAttribute1 is not null
                && IsSame(attributeDefinition, ResultAttribute1)
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
            || !IsSame(named.OriginalDefinition, Task)
            || named.TypeArguments.Length != 1)
        {
            return false;
        }

        taskResultType = named.TypeArguments[0];
        return TryGetResultTypes(taskResultType, out successType, out errorType);
    }

    public bool HasErrorUnionAttribute(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named && HasAttribute(named, ErrorUnionAttribute);
    }

    public bool HasErrorCaseAttribute(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named && HasAttribute(named, ErrorCaseAttribute);
    }

    public static bool IsSame(ISymbol? left, ISymbol? right)
    {
        return SymbolEqualityComparer.Default.Equals(left, right);
    }

    private static bool HasAttribute(INamedTypeSymbol type, INamedTypeSymbol? attributeType)
    {
        if (attributeType is null)
        {
            return false;
        }

        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass is not null
                && IsSame(attribute.AttributeClass.OriginalDefinition, attributeType))
            {
                return true;
            }
        }

        return false;
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
            return IsSame(substitutedElement, arrayType.ElementType)
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
                changed |= !IsSame(substituted[i], namedType.TypeArguments[i]);
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
                if (IsSame(originalDefinition.TypeParameters[i], typeParameter)
                    || IsSame(current.TypeParameters[i], typeParameter))
                {
                    return current.TypeArguments[i];
                }
            }
        }

        return null;
    }
}
