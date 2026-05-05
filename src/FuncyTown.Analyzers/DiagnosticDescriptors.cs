using Microsoft.CodeAnalysis;

namespace FuncyTown.Analyzers;

internal static class DiagnosticDescriptors
{
    private const string Category = "FuncyTown.Analyzers";
    private const string HelpLinkRoot = "https://github.com/mhagrelius/FuncyTown/blob/main/docs/rules/";

    private static string HelpLink(string id) => HelpLinkRoot + id + ".md";

    public static readonly DiagnosticDescriptor DiscardedResult = new(
        id: "FT0001",
        title: "Discarded Result return value",
        messageFormat: "The Result returned by '{0}' is being discarded; either await it, store it, match on it, or explicitly assign to '_'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Result values should be observed or explicitly discarded so success/failure state is not accidentally ignored.",
        helpLinkUri: HelpLink("FT0001"));

    public static readonly DiagnosticDescriptor ChainCrossesErrorTypes = new(
        id: "FT0002",
        title: "Result chain crosses error types",
        messageFormat: "Cannot chain '{0}' into a function returning a Result with a different TError; call MapError first",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Result chains should keep a consistent error type unless the error is explicitly transformed.",
        helpLinkUri: HelpLink("FT0002"));

    public static readonly DiagnosticDescriptor NonExhaustiveMatch = new(
        id: "FT0003",
        title: "Match over generated error union is not exhaustive",
        messageFormat: "Match on '{0}' is not exhaustive; missing case(s): {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Generated error unions expose exhaustive Match and Switch members; matching the whole union as a single failure value bypasses that coverage.",
        helpLinkUri: HelpLink("FT0003"));

    public static readonly DiagnosticDescriptor ImplicitConversionAtWideSite = new(
        id: "FT0004",
        title: "Result implicitly converted at wide-typed site",
        messageFormat: "An implicit conversion to '{0}' is occurring at a wide-typed site such as assignment to 'object'; consider being explicit",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Wide target types such as object can hide a Result alias's domain-specific identity.",
        helpLinkUri: HelpLink("FT0004"));

    public static readonly DiagnosticDescriptor UnattributedSubclass = new(
        id: "FT0005",
        title: "Subclass of [ErrorUnion] type is not marked [ErrorCase]",
        messageFormat: "Type '{0}' inherits from [ErrorUnion] base '{1}' but is not marked [ErrorCase]; the union will not see this case",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types that derive from an error union must be marked with [ErrorCase] so generated unions remain closed and exhaustive.",
        helpLinkUri: HelpLink("FT0005"));

    public static readonly DiagnosticDescriptor UnsealedErrorCase = new(
        id: "FT0006",
        title: "[ErrorCase] type must be sealed",
        messageFormat: "Type '{0}' is marked [ErrorCase] but is not sealed; closedness cannot be guaranteed",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "FuncyTown error cases must be sealed so generated union matching remains exhaustive.",
        helpLinkUri: HelpLink("FT0006"));
}
