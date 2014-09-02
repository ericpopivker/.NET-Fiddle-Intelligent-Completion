using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace DotNetFiddle.IntelligentCompletion
{
    /// <summary>
    /// VBNet related parsing logic
    /// </summary>
    public class VBNetLanguageService : LanguageService
    {
        private readonly VisualBasicParseOptions _options;

        private static readonly VisualBasicParseOptions DefaultOptions =
            new VisualBasicParseOptions().WithDocumentationMode(DocumentationMode.Parse);

        public VBNetLanguageService()
        {
            _options = DefaultOptions;
        }

        public VBNetLanguageService(VisualBasicParseOptions options)
            : this(options, null)
        {
        }

        public VBNetLanguageService(VisualBasicParseOptions options, LanguageServiceOptions serviceOptions)
            : base(serviceOptions)
        {
            _options = options ?? DefaultOptions;
        }

        public override Language Language
        {
            get { return Language.VbNet; }
        }

        public override Type GetSyntaxTreeType()
        {
            return typeof (SyntaxTree);
        }

        internal override string GetUsingKeyword()
        {
            return "imports";
        }


        public override string GetUsingNamespaceLinePattern()
        {
            return "^\\s*imports\\s*(\\S*)\\s*$";
        }

        protected override void AppendDefaultMetadataReferences(List<MetadataReference> references,
            bool includeDocumentation)
        {
            //Need to add vb or getting error
            //Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor' is not defined.
            MetadataReference vb = CreateMetadataReference("Microsoft.VisualBasic", includeDocumentation);
            references.Add(vb);
        }


        // http://stackoverflow.com/questions/13601412/compilation-errors-when-dealing-with-c-sharp-script-using-roslyn
        protected SyntaxTree ParseSyntaxTree(string code, VisualBasicParseOptions parseOptions, string path = "")
        {
			var tree = VisualBasicSyntaxTree.ParseText(code, parseOptions, path);
            return tree;
        }

        public override SyntaxTree ParseSyntaxTreeText(string code, string path = "")
        {
            var tree = ParseSyntaxTree(code, _options, path);
            return tree;
        }

        public override Compilation CreateCompilation(string compilatioName, SyntaxTree[] syntaxTrees,
            List<MetadataReference> metadataReferences)
        {
            var vbSyntaxTrees = new List<SyntaxTree>();
            foreach (var syntaxTree in syntaxTrees)
                vbSyntaxTrees.Add((SyntaxTree) syntaxTree);

            var vbOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            Compilation compilation = VisualBasicCompilation.Create(
                compilatioName,
                syntaxTrees: vbSyntaxTrees.ToArray(),
                references: metadataReferences,
                options: vbOptions);

            return compilation;
        }

        protected override int GetNewKeywordCode()
        {
            return (int) SyntaxKind.ObjectCreationExpression;
        }

        protected override int GetUsingDirectiveCode()
        {
            return (int) SyntaxKind.ImportsStatement;
        }

        protected override int GetArgumentCode()
        {
            return (int) SyntaxKind.SimpleArgument;
        }

        protected override int GetArgumentListCode()
        {
            return (int) SyntaxKind.ArgumentList;
        }

        protected override int GetIdentifierCode()
        {
            return (int) SyntaxKind.IdentifierToken;
        }

        protected override int GetQualifiedNameCode()
        {
            return (int) SyntaxKind.QualifiedName;
        }
    }
}