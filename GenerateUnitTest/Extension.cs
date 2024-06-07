using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace GenerateUnitTest
{
    public static class Extension
    {
        private static List<UsingDirectiveSyntax> _usingList = new List<UsingDirectiveSyntax>();

        public static UsingDirectiveSyntax[] UsingList { get => _usingList.ToArray(); }

        public static void LimparListaUsings() {
            _usingList.Clear();
        }

        private static ClassDeclarationSyntax AddConstructorDeclaration(this ClassDeclarationSyntax syntax, string className, IEnumerable<ParameterSyntax> param)
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
                            ))).ToArray())
                            .Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName($"_{className.Substring(0, 1).ToLower()}{className.Substring(1)}"), 
                                SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(className),
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

        public static NamespaceDeclarationSyntax AddClassDeclaration(this NamespaceDeclarationSyntax syntax, ClassDeclarationSyntax classDeclaration, IEnumerable<ParameterSyntax> param, IEnumerable<SyntaxTree> syntaxTrees)
        {
            return syntax.AddMembers(
                SyntaxFactory.ClassDeclaration($"{classDeclaration.Identifier.Text}Tests")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddMembers(SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(classDeclaration.Identifier.Text)).
                    AddVariables(SyntaxFactory.VariableDeclarator(
                            $"_{classDeclaration.Identifier.Text.Substring(0,1).ToLower()}{classDeclaration.Identifier.Text.Substring(1)}"))
                    ).AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                .AddMembers(
                    param.Select(x => SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(SyntaxFactory.GenericName(SyntaxFactory.ParseToken("Mock"),
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName(x.Type.ToString())))))
                            .AddVariables(
                            SyntaxFactory.VariableDeclarator(
                                $"_mock{x.Identifier.Text.Substring(0, 1).ToUpper()}{x.Identifier.Text.Substring(1)}"))
                        ).AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))).ToArray()
                    ).AddConstructorDeclaration(classDeclaration.Identifier.Text, param)
                    .AddMethodsDeclaration(classDeclaration, syntaxTrees));
        }

        private static ClassDeclarationSyntax AddMethodsDeclaration(this ClassDeclarationSyntax syntax, ClassDeclarationSyntax classDeclaration, IEnumerable<SyntaxTree> syntaxTrees)
        {
            return syntax.AddMembers(classDeclaration.Members.Where(x => x.Kind() == SyntaxKind.MethodDeclaration &&
                x.Modifiers.Any(z => z.IsKind(SyntaxKind.PublicKeyword)) && 
                ((MethodDeclarationSyntax)x).Identifier.Text == "Handle")
                    .Select(y => SyntaxFactory.MethodDeclaration((TypeSyntax)SyntaxFactory.IdentifierName("void"),
                    ((IdentifierNameSyntax)((MethodDeclarationSyntax)y).ParameterList.Parameters.First().Type).Identifier.Text
                    ).AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.ParseName("Fact")
                    ))))
                    .GenerateUsingsByParameters(
                        syntaxTrees.FirstOrDefault(a => a.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                            .FirstOrDefault(b => b.Identifier.Text == ((IdentifierNameSyntax)((MethodDeclarationSyntax)y).ParameterList
                            .Parameters.First().Type).Identifier.Text) != null)?.GetRoot()
                            .DescendantNodes().OfType<ConstructorDeclarationSyntax>().First().ParameterList.Parameters, syntaxTrees
                    )
                    .GenerateUsingsByParameters(
                        new List<ParameterSyntax>() { ((MethodDeclarationSyntax)y).ParameterList.Parameters.First() }, syntaxTrees
                    )
                    .AddBodyStatements(
                        syntaxTrees.FirstOrDefault(a => a.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                            .FirstOrDefault(b => b.Identifier.Text == ((IdentifierNameSyntax)((MethodDeclarationSyntax)y).ParameterList
                            .Parameters.First().Type).Identifier.Text) != null)?.GetRoot()
                            .DescendantNodes().OfType<ConstructorDeclarationSyntax>().First().ParameterList.Parameters
                            .Select(c => SyntaxFactory.LocalDeclarationStatement(
                                SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("var"))
                                    .AddVariables(SyntaxFactory.VariableDeclarator($"{c.Type.GetText().ToString().Substring(0, 1).ToLower()}{c.Type.GetText().ToString().Trim().Substring(1)}")
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.ObjectCreationExpression(c.Type, SyntaxFactory.ArgumentList(), null)
                    ))))).ToArray()
                    )
                    .AddBodyStatements(
                        SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("var"))
                        .AddVariables(SyntaxFactory.VariableDeclarator(
                            $"{((MethodDeclarationSyntax)y).ParameterList.Parameters.First().Type.GetText().ToString().Substring(0, 1).ToLower()}{((MethodDeclarationSyntax)y).ParameterList.Parameters.First().Type.GetText().ToString().Substring(1)}")
                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ObjectCreationExpression(((MethodDeclarationSyntax)y).ParameterList.Parameters.First().Type,
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                    syntaxTrees.FirstOrDefault(a => a.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                                        .FirstOrDefault(b => b.Identifier.Text == ((IdentifierNameSyntax)((MethodDeclarationSyntax)y).ParameterList
                                        .Parameters.First().Type).Identifier.Text) != null)?.GetRoot()
                                        .DescendantNodes().OfType<ConstructorDeclarationSyntax>().First().ParameterList.Parameters
                                            .Select(c => SyntaxFactory.Argument(
                                                SyntaxFactory.IdentifierName($"{c.Type.GetText().ToString().Substring(0, 1).ToLower()}{c.Type.GetText().ToString().Trim().Substring(1)}")
                                    )).ToArray())
                        ), null)))))
                    )).ToArray());
        }

        private static MethodDeclarationSyntax GenerateUsingsByParameters(this MethodDeclarationSyntax syntax, IEnumerable<ParameterSyntax> param, IEnumerable<SyntaxTree> syntaxTrees)
        {
            _usingList.AddRange(GenerateUsingsByParameters(param, syntaxTrees));
            return syntax;
        }

        public static UsingDirectiveSyntax[] GenerateUsingsByParameters(IEnumerable<ParameterSyntax> param, IEnumerable<SyntaxTree> syntaxTrees)
        {
            var list = new List<UsingDirectiveSyntax>();

            foreach (var item in param)
            {
                var classNamespace = syntaxTrees.SelectMany(x => x.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                .FirstOrDefault(y => y.Identifier.Text == ((IdentifierNameSyntax)item.Type).Identifier.Text && y.Parent is NamespaceDeclarationSyntax)?.Parent as NamespaceDeclarationSyntax;

                var interfaceNamespace = syntaxTrees.SelectMany(x => x.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>())
                .FirstOrDefault(y => y.Identifier.Text == ((IdentifierNameSyntax)item.Type).Identifier.Text && y.Parent is NamespaceDeclarationSyntax)?.Parent as NamespaceDeclarationSyntax;

                if (classNamespace != null)
                    list.Add(SyntaxFactory.UsingDirective(classNamespace.Name));

                if (interfaceNamespace != null)
                    list.Add(SyntaxFactory.UsingDirective(interfaceNamespace.Name));
            }

            return list.ToArray();
        }
    }
}
