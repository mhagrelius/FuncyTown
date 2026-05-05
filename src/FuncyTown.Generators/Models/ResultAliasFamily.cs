using FuncyTown.Generators.Internal;

namespace FuncyTown.Generators.Models;

internal sealed record ResultAliasFamily(
    string ErrorTypeFullyQualifiedName,
    EquatableArray<ResultAliasModel> Members,
    ErrorUnionModel? ErrorUnion);
