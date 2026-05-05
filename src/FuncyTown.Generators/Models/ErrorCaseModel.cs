using FuncyTown.Generators.Internal;

namespace FuncyTown.Generators.Models;

internal sealed record ErrorCaseModel(
    string FullyQualifiedName,
    string CaseTypeName,
    string SimpleName,
    string Accessibility,
    string BaseFullyQualifiedName,
    bool IsSealed,
    EquatableArray<ConstructorParameter> ConstructorParameters);

internal sealed record ConstructorParameter(string TypeFullyQualifiedName, string Name);
