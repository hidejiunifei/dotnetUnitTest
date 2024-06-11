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

        private static SyntaxList<StatementSyntax> AddConditionally(this SyntaxList<StatementSyntax> list, ExpressionStatementSyntax syntax, bool tests) {

            if (tests)
                list.Add(syntax);
            return list;
        }

        private static ClassDeclarationSyntax AddConstructorDeclaration(this ClassDeclarationSyntax syntax, string className, IEnumerable<ParameterSyntax> param, bool tests)
        {
            return syntax.AddMembers(SyntaxFactory.ConstructorDeclaration($"{className}{(tests ? "Tests" : string.Empty)}")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(SyntaxFactory.Block(
                    new SyntaxList<StatementSyntax>(
                        param.Select(x => (tests ? SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName($"_mock{x.Identifier.Text.Substring(0, 1).ToUpper()}{x.Identifier.Text.Substring(1)}"),
                            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.GenericName(SyntaxFactory.ParseToken("Mock"),
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.IdentifierName(x.Type.ToString())))), SyntaxFactory.ArgumentList(), null)))
                            :
                            SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName($"_{x.Identifier.Text.Substring(0, 1).ToUpper()}{x.Identifier.Text.Substring(1)}"),
                            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(x.Type.ToString()), SyntaxFactory.ArgumentList(), null))))
                            ).ToArray())
                            .AddConditionally(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName($"_{className.Substring(0, 1).ToLower()}{className.Substring(1)}"), 
                                SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(className),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                    param.Select(x => SyntaxFactory.Argument(
                                        SyntaxFactory.IdentifierName($"_mock{x.Identifier.Text.Substring(0, 1).ToUpper()}{x.Identifier.Text.Substring(1)}.Object")
                                    )).ToArray())
                                )
                            , null))), tests)
                            )
                        ));
        }

        private static ClassDeclarationSyntax AddMembers(this ClassDeclarationSyntax syntax, string identifier, bool tests)
        {
            if (tests)
                syntax.AddMembers(SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(identifier)).
                    AddVariables(SyntaxFactory.VariableDeclarator(
                            $"_{identifier.Substring(0, 1).ToLower()}{identifier.Substring(1)}"))
                    ).AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));

            return syntax;
        }

        private static ClassDeclarationSyntax AddBaseListTypes(this ClassDeclarationSyntax syntax, IEnumerable<ParameterSyntax> param, bool tests)
        {
            if (tests)
                return syntax;

            return syntax.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("Notifiable")))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(
                    $"IRequestHandler<{((IdentifierNameSyntax)param.First().Type).Identifier.Text}, object>")));
        }

        private static ClassDeclarationSyntax AddMembers(this ClassDeclarationSyntax syntax, IEnumerable<MemberDeclarationSyntax> members, bool tests)
        {
            if (tests)
                return syntax;

            return syntax.AddMembers(members.ToArray());
        }

        public static NamespaceDeclarationSyntax AddClassDeclaration(this NamespaceDeclarationSyntax syntax, ClassDeclarationSyntax classDeclaration, IEnumerable<ParameterSyntax> param, IEnumerable<SyntaxTree> syntaxTrees, IEnumerable<MemberDeclarationSyntax> members, bool tests)
        {
            return syntax.AddMembers(
                SyntaxFactory.ClassDeclaration($"{classDeclaration.Identifier.Text}{(tests ? "Tests": string.Empty )}")
                .AddBaseListTypes(classDeclaration.ParameterList.Parameters, tests)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddMembers(classDeclaration.Identifier.Text, tests)
                .AddMembers(
                    param.Select(x => (tests ? SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(SyntaxFactory.GenericName(SyntaxFactory.ParseToken("Mock"),
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName(x.Type.ToString())))))
                            .AddVariables(
                            SyntaxFactory.VariableDeclarator(
                                $"_mock{x.Identifier.Text.Substring(0, 1).ToUpper()}{x.Identifier.Text.Substring(1)}"))
                            ) : SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(x.Type.ToString()))
                                .AddVariables(SyntaxFactory.VariableDeclarator($"{x.Identifier.Text.Substring(0, 1).ToUpper()}{x.Identifier.Text.Substring(1)}"))
                        )).AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))).ToArray()
                    )
                .AddMembers(members, tests)
                .AddConstructorDeclaration(classDeclaration.Identifier.Text, param, tests)
                .AddMethodsDeclaration(classDeclaration, syntaxTrees, tests));
        }

        private static ClassDeclarationSyntax AddMethodsDeclaration(this ClassDeclarationSyntax syntax, ClassDeclarationSyntax classDeclaration, IEnumerable<SyntaxTree> syntaxTrees, bool tests)
        {
            if (!tests)
                return syntax;

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
                                    .AddVariables(SyntaxFactory.VariableDeclarator(c.Type is PredefinedTypeSyntax ? c.Identifier.Text : GetVariableName(c.Type))
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
                                                SyntaxFactory.IdentifierName(c.Type is PredefinedTypeSyntax ? c.Identifier.Text : GetVariableName(c.Type))
                                    )).ToArray())
                        ), null)))))
                    )).ToArray());
        }

        private static string GetVariableName(TypeSyntax syntax) {
            if (syntax is GenericNameSyntax)
            {
                _usingList.Add(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Collections.Generic")));
                return $"{((GenericNameSyntax)syntax).TypeArgumentList.Arguments.First().GetText().ToString().Substring(0,1).ToLower()}{((GenericNameSyntax)syntax).TypeArgumentList.Arguments.First().GetText().ToString().Substring(1)}s";
            }
            else
            {
                return $"{syntax.GetText().ToString().Substring(0, 1).ToLower()}{syntax.GetText().ToString().Trim().Substring(1)}";
            }
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
                var classNamespaces = new List<NamespaceDeclarationSyntax>();
                var interfaceNamespaces = new List<NamespaceDeclarationSyntax>();
                NamespaceDeclarationSyntax classNamespace;
                NamespaceDeclarationSyntax interfaceNamespace;

                if (item.Type is GenericNameSyntax)
                {
                    var argumentTypes = ((GenericNameSyntax)item.Type).TypeArgumentList.Arguments.Select(y => ((IdentifierNameSyntax)y).Identifier.Text);

                    classNamespace = syntaxTrees.SelectMany(x => x.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                    .FirstOrDefault(y => argumentTypes.Contains(y.Identifier.Text) && y.Parent is NamespaceDeclarationSyntax)?.Parent as NamespaceDeclarationSyntax;

                    if (classNamespace != null)
                        classNamespaces.Add(classNamespace);

                    interfaceNamespace = syntaxTrees.SelectMany(x => x.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>())
                    .FirstOrDefault(y => argumentTypes.Contains(y.Identifier.Text) && y.Parent is NamespaceDeclarationSyntax)?.Parent as NamespaceDeclarationSyntax;

                    if (interfaceNamespace != null)
                        interfaceNamespaces.Add(interfaceNamespace);
                }
                else 
                {
                    if (!(item.Type is PredefinedTypeSyntax))
                    {
                        classNamespace = syntaxTrees.SelectMany(x => x.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                        .FirstOrDefault(y => y.Identifier.Text == ((IdentifierNameSyntax)item.Type).Identifier.Text
                        && y.Parent is NamespaceDeclarationSyntax)?.Parent as NamespaceDeclarationSyntax;

                        if (classNamespace != null)
                            classNamespaces.Add(classNamespace);

                        interfaceNamespace = syntaxTrees.SelectMany(x => x.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>())
                        .FirstOrDefault(y => y.Identifier.Text == ((IdentifierNameSyntax)item.Type).Identifier.Text
                        && y.Parent is NamespaceDeclarationSyntax)?.Parent as NamespaceDeclarationSyntax;

                        if (interfaceNamespace != null)
                            interfaceNamespaces.Add(interfaceNamespace);
                    }
                }
                if (classNamespaces.Any())
                    list.AddRange(classNamespaces.Select(x => SyntaxFactory.UsingDirective(x.Name)));

                if (interfaceNamespaces.Any())
                    list.AddRange(interfaceNamespaces.Select(x => SyntaxFactory.UsingDirective(x.Name)));
            }

            return list.ToArray();
        }
    }
}
