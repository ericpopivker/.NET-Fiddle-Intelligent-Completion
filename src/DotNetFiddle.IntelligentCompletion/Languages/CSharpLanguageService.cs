using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotNetFiddle.IntelligentCompletion
{
    /// <summary>
    /// CSharp related parsing logic
    /// </summary>
    public class CSharpLanguageService : LanguageService
    {
        private readonly CSharpParseOptions _options;

        private static readonly CSharpParseOptions DefaultOptions =
            new CSharpParseOptions(LanguageVersion.CSharp6).WithDocumentationMode(DocumentationMode.Parse);

        public CSharpLanguageService()
        {
            _options = DefaultOptions;
        }

        public CSharpLanguageService(CSharpParseOptions options)
            : this(options, null)
        {
        }

        public CSharpLanguageService(CSharpParseOptions options, LanguageServiceOptions serviceOptions)
            : base(serviceOptions)
        {
            _options = options ?? DefaultOptions;
        }

        public override SyntaxTree ParseSyntaxTreeText(string code, string path = "")
        {
            var tree = ParseSyntaxTree(code, _options, path);
            return tree;
        }

        public override Language Language
        {
            get { return Language.CSharp; }
        }

        internal override string GetUsingKeyword()
        {
            return "using";
        }

        protected override int GetNewKeywordCode()
        {
            return (int) SyntaxKind.ObjectCreationExpression;
        }

        protected override int GetUsingDirectiveCode()
        {
            return (int) SyntaxKind.UsingDirective;
        }

        protected override int GetArgumentCode()
        {
            return (int) SyntaxKind.Argument;
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

        public override Type GetSyntaxTreeType()
        {
            return typeof (SyntaxTree);
        }

        public override string GetUsingNamespaceLinePattern()
        {
            return "^\\s*using\\s*(\\S*)\\s*;";
        }

        protected SyntaxTree ParseSyntaxTree(string code, CSharpParseOptions parseOptions, string path = "")
        {
            var tree = CSharpSyntaxTree.ParseText(code, parseOptions, path);
            return tree;
        }

        public override Compilation CreateCompilation(string compilatioName, SyntaxTree[] syntaxTrees,
            List<MetadataReference> metadataReferences)
        {
            Compilation compilation = CSharpCompilation.Create(compilatioName, syntaxTrees.ToArray(), metadataReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation;
        }
    }
}