using System.Collections.Immutable;
using FuncyTown.Analyzers.CodeFixes.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace FuncyTown.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DiscardedResultCodeFix))]
public sealed class DiscardedResultCodeFix : CodeFixProvider
{
    private const string DiagnosticId = "FT0001";
    private const string DiscardTitle = "Discard explicitly";
    private const string AwaitDiscardTitle = "Await and discard explicitly";
    // Stable, locale-invariant equivalence keys so fix-all batching keeps working when
    // the user-facing titles get localized.
    private const string DiscardEquivalenceKey = "FT0001:Discard";
    private const string AwaitDiscardEquivalenceKey = "FT0001:AwaitDiscard";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticId);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();
        var expressionStatement = root?
            .FindNode(diagnostic.Location.SourceSpan)
            .FirstAncestorOrSelf<ExpressionStatementSyntax>();

        if (expressionStatement is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                DiscardTitle,
                cancellationToken => DiscardExplicitlyAsync(context.Document, expressionStatement, cancellationToken),
                DiscardEquivalenceKey),
            diagnostic);

        if (await CanOfferAwaitFixAsync(context.Document, expressionStatement, context.CancellationToken).ConfigureAwait(false))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    AwaitDiscardTitle,
                    cancellationToken => AwaitAndDiscardExplicitlyAsync(context.Document, expressionStatement, cancellationToken),
                    AwaitDiscardEquivalenceKey),
                diagnostic);
        }
    }

    private static async Task<bool> CanOfferAwaitFixAsync(
        Document document,
        ExpressionStatementSyntax expressionStatement,
        CancellationToken cancellationToken)
    {
        if (!CanLegallyAwait(expressionStatement))
        {
            return false;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return false;
        }

        var operation = semanticModel.GetOperation(expressionStatement.Expression, cancellationToken);
        if (operation is null)
        {
            return false;
        }

        var knownTypes = new KnownTypes(semanticModel.Compilation);
        return knownTypes.TryGetTaskResultTypes(operation.Type, out _, out _, out _);
    }

    private static async Task<Document> DiscardExplicitlyAsync(
        Document document,
        ExpressionStatementSyntax expressionStatement,
        CancellationToken cancellationToken)
    {
        return await ReplaceStatementAsync(
            document,
            expressionStatement,
            CreateDiscardStatement(expressionStatement.Expression),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> AwaitAndDiscardExplicitlyAsync(
        Document document,
        ExpressionStatementSyntax expressionStatement,
        CancellationToken cancellationToken)
    {
        var awaitExpression = SyntaxFactory.AwaitExpression(expressionStatement.Expression.WithoutLeadingTrivia());
        return await ReplaceStatementAsync(
            document,
            expressionStatement,
            CreateDiscardStatement(awaitExpression),
            cancellationToken).ConfigureAwait(false);
    }

    private static ExpressionStatementSyntax CreateDiscardStatement(ExpressionSyntax expression)
    {
        return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName("_"),
                    expression.WithoutLeadingTrivia()))
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static async Task<Document> ReplaceStatementAsync(
        Document document,
        ExpressionStatementSyntax oldStatement,
        ExpressionStatementSyntax newStatement,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return root is null
            ? document
            : document.WithSyntaxRoot(root.ReplaceNode(oldStatement, newStatement.WithTriviaFrom(oldStatement)));
    }

    private static bool CanLegallyAwait(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            switch (ancestor)
            {
                case MethodDeclarationSyntax method:
                    return method.Modifiers.Any(SyntaxKind.AsyncKeyword);
                case LocalFunctionStatementSyntax localFunction:
                    return localFunction.Modifiers.Any(SyntaxKind.AsyncKeyword);
                case AnonymousMethodExpressionSyntax anonymousMethod:
                    return anonymousMethod.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
                case ParenthesizedLambdaExpressionSyntax lambda:
                    return lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
                case SimpleLambdaExpressionSyntax lambda:
                    return lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
            }
        }

        return false;
    }
}
