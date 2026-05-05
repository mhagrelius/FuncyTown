using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FuncyTown.Generators.Internal;

internal static class TypeProjectionHelpers
{
    public static string GetAccessibility(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Private => "private",
        Accessibility.Protected => "protected",
        Accessibility.Internal => "internal",
        Accessibility.ProtectedAndInternal => "private protected",
        Accessibility.ProtectedOrInternal => "protected internal",
        _ => "internal",
    };

    public static string GetContainingTypeDeclarations(TypeDeclarationSyntax declaration)
    {
        var containingTypes = declaration
            .Ancestors()
            .OfType<TypeDeclarationSyntax>()
            .Reverse()
            .Select(static containingType => BuildContainingTypeDeclaration(containingType))
            .ToArray();

        return string.Join("\n", containingTypes);
    }

    private static string BuildContainingTypeDeclaration(TypeDeclarationSyntax declaration)
    {
        var builder = new StringBuilder();
        if (declaration.Modifiers.Count > 0)
        {
            builder.Append(declaration.Modifiers);
            builder.Append(' ');
        }

        switch (declaration)
        {
            case ClassDeclarationSyntax:
                builder.Append("class ");
                break;
            case StructDeclarationSyntax:
                builder.Append("struct ");
                break;
            case InterfaceDeclarationSyntax:
                builder.Append("interface ");
                break;
            case RecordDeclarationSyntax recordDeclaration:
                builder.Append("record");
                if (!recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.None))
                {
                    builder.Append(' ');
                    builder.Append(recordDeclaration.ClassOrStructKeyword.ValueText);
                }

                builder.Append(' ');
                break;
        }

        builder.Append(declaration.Identifier.ValueText);
        if (declaration.TypeParameterList is not null)
        {
            builder.Append(declaration.TypeParameterList);
        }

        return builder.ToString();
    }
}
