using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;
using EnvDTE80;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Generic;

namespace GenerateUnitTest
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GenerateUnitTests
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("7d642241-da32-480e-9fbd-a9bfb78aca52");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateUnitTests"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private GenerateUnitTests(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GenerateUnitTests Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in GenerateHandlerUnitTests's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new GenerateUnitTests(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            // Get DTE Object
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            UIHierarchy uih = dte.ToolWindows.SolutionExplorer;

            Array selectedItems = (Array)uih.SelectedItems;
            if (null != selectedItems)
            {
                foreach (UIHierarchyItem selItem in selectedItems)
                {
                    Extension.LimparListaUsings();
                    ProjectItem prjItem = selItem.Object as ProjectItem;
                    string filePath = prjItem.Properties.Item("FullPath").Value.ToString();

                    string projectPath = prjItem.ContainingProject.FullName;

                    var workspace = MSBuildWorkspace.Create();
                    var project = await workspace.OpenProjectAsync(projectPath);
                    var compilation = await project.GetCompilationAsync();
                    var syntaxTrees = compilation.SyntaxTrees;

                    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
                    var nameSpace = tree.GetRoot().ChildNodes().Single(x => x.IsKind(SyntaxKind.NamespaceDeclaration));

                    var classDeclaration = ((ClassDeclarationSyntax)nameSpace.ChildNodes()
                        .Single(x => x.IsKind(SyntaxKind.ClassDeclaration)))
                        .WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));

                    var constructor = ((ConstructorDeclarationSyntax)classDeclaration.ChildNodes()
                        .Single(z => z.IsKind(SyntaxKind.ConstructorDeclaration)))
                        .WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));

                    var comp = SyntaxFactory.CompilationUnit()
                        .AddUsings(SyntaxFactory.UsingDirective(((NamespaceDeclarationSyntax)nameSpace).Name))
                        .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Moq")))
                        .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Xunit")))
                        .AddUsings(Extension.GenerateUsingsByParameters(constructor.ParameterList.ChildNodes().Cast<ParameterSyntax>(), syntaxTrees))
                        .AddMembers(
                        SyntaxFactory.NamespaceDeclaration(((NamespaceDeclarationSyntax)nameSpace).Name)
                            .AddClassDeclaration(classDeclaration, constructor.ParameterList.ChildNodes().Cast<ParameterSyntax>(), syntaxTrees)
                        )
                        .AddUsings(Extension.UsingList)
                        .NormalizeWhitespace().ToFullString();
                    File.WriteAllText(filePath.Replace(".cs", "Tests.cs"), comp);
                }
            }
        }

        
    }
}
