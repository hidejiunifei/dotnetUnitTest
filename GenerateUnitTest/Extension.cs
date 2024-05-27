using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateUnitTest
{
    public static class Extension
    {
        public static ClassDeclarationSyntax AddConstructorDeclaration(this ClassDeclarationSyntax syntax, string className, IEnumerable<ParameterSyntax> param)
        {
            return syntax.AddMembers(SyntaxFactory.ConstructorDeclaration($"{className}Tests")
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .WithBody(SyntaxFactory.Block(
            new SyntaxList<StatementSyntax>(
                                    param.Select(x => SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        SyntaxFactory.IdentifierName($"_mock{x.Identifier.Text.Substring(0, 1).ToUpper()}{x.Identifier.Text.Substring(1)}"),
                                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.GenericName(SyntaxFactory.ParseToken("Mock"),
                                            SyntaxFactory.TypeArgumentList(
                                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                                SyntaxFactory.IdentifierName(x.Type.ToString())))), SyntaxFactory.ArgumentList(), null)
                                        ))).ToArray()).
                                        Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        SyntaxFactory.IdentifierName($"_{className}"), SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(className),
                                        SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                param.Select(x => SyntaxFactory.Argument(
                                                    SyntaxFactory.IdentifierName($"_mock{x.Identifier.Text.Substring(0, 1).ToUpper()}{x.Identifier.Text.Substring(1)}.Object")
                                                )).ToArray())
                                            )
                                        , null))))
                                        )
                                    ));
        }

        public static NamespaceDeclarationSyntax AddClassDeclaration(this NamespaceDeclarationSyntax syntax, string className, IEnumerable<ParameterSyntax> param)
        {
            return syntax.AddMembers(
                                SyntaxFactory.ClassDeclaration($"{className}Tests")
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .AddMembers(SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(className)).
                                        AddVariables(SyntaxFactory.VariableDeclarator($"_{className}"))
                                    ).AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                                .AddMembers(
                                    param.Select(x => SyntaxFactory.FieldDeclaration(
                                            SyntaxFactory.VariableDeclaration(SyntaxFactory.GenericName(SyntaxFactory.ParseToken("Mock"),
                                            SyntaxFactory.TypeArgumentList(
                                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                                SyntaxFactory.IdentifierName(x.Type.ToString())))))
                                            .AddVariables(
                                            SyntaxFactory.VariableDeclarator($"_mock{x.Identifier.Text.Substring(0, 1).ToUpper()}{x.Identifier.Text.Substring(1)}"))
                                        ).AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))).ToArray()
                                  ).AddConstructorDeclaration(className, param));
        }
    }
}
