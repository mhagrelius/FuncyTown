using Microsoft.CodeAnalysis;

namespace FuncyTown.Generators.Models;

internal static class DiagnosticDescriptors
{
    private const string Category = "FuncyTown.Generation";
    private const string HelpLinkRoot = "https://github.com/mhagrelius/FuncyTown/blob/main/docs/rules/";

    private static string HelpLink(string id) => HelpLinkRoot + id + ".md";

    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "FT0007",
        title: "FuncyTown Result alias must be partial",
        messageFormat: "Type '{0}' is decorated with [Result<...>] but is not declared partial; the source generator cannot emit members for it",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The FuncyTown Result alias source generator emits additional members into a partial declaration.",
        helpLinkUri: HelpLink("FT0007"));

    public static readonly DiagnosticDescriptor FileLocalContainingTypeUnsupported = new(
        id: "FT0008",
        title: "FuncyTown generated type cannot be nested in a file-local type",
        messageFormat: "Type '{0}' is nested inside a file-local containing type; the source generator cannot emit matching generated members for that shape",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "File-local type identity is scoped to a single source file, so generated partial declarations and generated type references cannot extend types nested inside file-local containing types.",
        helpLinkUri: HelpLink("FT0008"));

    public static readonly DiagnosticDescriptor PrimaryConstructorParametersUnsupported = new(
        id: "FT0009",
        title: "FuncyTown Result alias cannot declare primary constructor parameters",
        messageFormat: "Type '{0}' is decorated with [Result<...>] but declares primary constructor parameters; move that state into the Result success type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "FuncyTown Result aliases wrap generated Result state. Additional primary constructor parameters create separate record state that cannot be initialized by the generated Success and Failure factories.",
        helpLinkUri: HelpLink("FT0009"));
}
