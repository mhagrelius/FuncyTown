namespace FuncyTown.Generators.Models;

/// <summary>
/// Equatable record-based projection of one <c>[Result&lt;T, TError&gt;]</c> declaration site.
/// Used as the value type in the incremental generator pipeline so caching works.
/// </summary>
internal sealed record ResultAliasModel(
    string AliasNamespace,
    string AliasName,
    string AliasAccessibility,
    string AliasFullyQualifiedName,
    string ValueTypeFullyQualifiedName,
    string ErrorTypeFullyQualifiedName,
    string ContainingTypeDeclarations,
    bool IsClassKind,
    bool ImplicitConversionsEnabled,
    bool JsonConverterEnabled,
    bool IsVoidSuccess)
{
    /// <summary>The hint name used for the generated source file.</summary>
    public string HintName => $"{AliasFullyQualifiedName.Replace("global::", string.Empty).Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_')}.g.cs";
}
