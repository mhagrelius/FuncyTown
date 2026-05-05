using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace FuncyTown.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnsealedErrorCaseCodeFix))]
public sealed class UnsealedErrorCaseCodeFix : CodeFixProvider
{
    private const string DiagnosticId = "FT0006";
    private const string Title = "Add sealed modifier";
    // Stable, locale-invariant equivalence key so fix-all batching keeps working when
    // the user-facing title gets localized.
    private const string EquivalenceKey = "FT0006:AddSealed";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticId);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var declaration = root
            .FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent?
            .FirstAncestorOrSelf<TypeDeclarationSyntax>();

        if (declaration is null)
        {
            return;
        }

        // If the user wrote `[ErrorCase] abstract class Foo` and Foo has descendants,
        // silently swapping `abstract` for `sealed` would break the derived chain.
        // In that case decline to offer the fix; the diagnostic itself still fires so
        // the user is on the hook to either remove `abstract` or move `[ErrorCase]`.
        if (declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)
            && await HasDescendantsAsync(context.Document, declaration, context.CancellationToken).ConfigureAwait(false))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => AddSealedAsync(context.Document, declaration, cancellationToken),
                EquivalenceKey),
            diagnostic);
    }

    private static async Task<Document> AddSealedAsync(
        Document document,
        TypeDeclarationSyntax declaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var updatedDeclaration = declaration
            .WithModifiers(AddOrReplaceSealed(declaration.Modifiers))
            .WithAdditionalAnnotations(Formatter.Annotation);

        return document.WithSyntaxRoot(root.ReplaceNode(declaration, updatedDeclaration));
    }

    private static async Task<bool> HasDescendantsAsync(
        Document document,
        TypeDeclarationSyntax declaration,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return false;
        }

        var target = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
        if (target is null)
        {
            return false;
        }

        return ContainsDescendant(semanticModel.Compilation.GlobalNamespace, target);
    }

    private static bool ContainsDescendant(INamespaceOrTypeSymbol scope, INamedTypeSymbol target)
    {
        foreach (var member in scope.GetMembers())
        {
            if (member is INamespaceSymbol ns)
            {
                if (ContainsDescendant(ns, target))
                {
                    return true;
                }

                continue;
            }

            if (member is INamedTypeSymbol type)
            {
                if (!SymbolEqualityComparer.Default.Equals(type, target) && Inherits(type, target))
                {
                    return true;
                }

                if (ContainsDescendant(type, target))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool Inherits(INamedTypeSymbol type, INamedTypeSymbol target)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, target))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static SyntaxTokenList AddOrReplaceSealed(SyntaxTokenList modifiers)
    {
        for (var i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].IsKind(SyntaxKind.SealedKeyword))
            {
                return modifiers;
            }

            if (modifiers[i].IsKind(SyntaxKind.AbstractKeyword))
            {
                return modifiers.Replace(
                    modifiers[i],
                    SyntaxFactory.Token(SyntaxKind.SealedKeyword).WithTriviaFrom(modifiers[i]));
            }
        }

        return modifiers.Insert(GetSealedInsertionIndex(modifiers), SyntaxFactory.Token(SyntaxKind.SealedKeyword));
    }

    private static int GetSealedInsertionIndex(SyntaxTokenList modifiers)
    {
        var index = 0;
        while (index < modifiers.Count && IsAccessibilityModifier(modifiers[index]))
        {
            index++;
        }

        return index;
    }

    private static bool IsAccessibilityModifier(SyntaxToken modifier)
    {
        return modifier.IsKind(SyntaxKind.PublicKeyword)
            || modifier.IsKind(SyntaxKind.PrivateKeyword)
            || modifier.IsKind(SyntaxKind.ProtectedKeyword)
            || modifier.IsKind(SyntaxKind.InternalKeyword)
            || modifier.IsKind(SyntaxKind.FileKeyword);
    }
}
