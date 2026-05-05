using FuncyTown.Generators.Internal;

namespace FuncyTown.Generators.Models;

internal sealed record ErrorUnionModel(
    string Namespace,
    string TypeName,
    string FullyQualifiedName,
    string Accessibility,
    string ContainingTypeDeclarations,
    EquatableArray<ErrorCaseModel> Cases);
