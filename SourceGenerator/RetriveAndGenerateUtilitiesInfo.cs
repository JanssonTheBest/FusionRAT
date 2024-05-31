using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SourceGenerator
{
    [Generator]
    internal class RetriveAndGenerateUtilitiesInfo : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            string targetNamespace = "Server.UtilityWindows";

            IEnumerable<SyntaxTree> syntaxTrees = context.Compilation.SyntaxTrees;

            IEnumerable<SyntaxTree> namespaceSyntaxTrees = syntaxTrees
    .Where(tree => tree.GetRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>()
    .Any(namespaceSyntax => namespaceSyntax.Name.ToString() == targetNamespace));

            IEnumerable<string> csFilesInNamespace = namespaceSyntaxTrees
                .Select(tree => tree.FilePath)
                .Where(filePath => filePath.EndsWith(".cs"));

            //context.Compilation.GetTypeByMetadataName();
            //foreach (var item in collection)
            //{

            //}
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            throw new NotImplementedException();
        }
    }
}
