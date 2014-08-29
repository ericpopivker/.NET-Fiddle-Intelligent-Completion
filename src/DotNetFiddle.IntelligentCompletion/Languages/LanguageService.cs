using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace DotNetFiddle.IntelligentCompletion
{
    /// <summary>
    /// Class with common parsing logic
    /// </summary>
    public abstract class LanguageService
    {
        private readonly LanguageServiceOptions _options;

        protected LanguageService()
            : this(new LanguageServiceOptions())
        {
        }

        protected LanguageService(LanguageServiceOptions options)
        {
            _options = options ?? new LanguageServiceOptions();
        }

        private string _systemXmlFilesDir;

        public abstract Language Language { get; }

        internal abstract string GetUsingKeyword();

        public abstract Type GetSyntaxTreeType();

        public abstract SyntaxTree ParseSyntaxTreeText(string code, string path = "");
        public abstract SyntaxTree ParseSyntaxTreeFile(string filePath);

        public abstract Compilation CreateCompilation(
            string compilatioName,
            SyntaxTree[] syntaxTrees,
            List<MetadataReference> matadataReferences);

        private static Regex _crefRegex = new Regex(@"<see +cref=\""T:([\w.]*)\""[ />]+");

        //Used for AutoComplete items
        protected abstract int GetNewKeywordCode();

        protected abstract int GetUsingDirectiveCode();

        protected abstract int GetArgumentCode();

        protected abstract int GetArgumentListCode();

        protected abstract int GetIdentifierCode();

        protected abstract int GetQualifiedNameCode();

        public abstract string GetUsingNamespaceLinePattern();

        protected virtual void AppendDefaultMetadataReferences(List<MetadataReference> references,
            bool includeDocumentation)
        {
        }

        public Project GetProject(List<string> filePaths)
        {
            var files = new Dictionary<string, SyntaxTree>();
            foreach (var filePath in filePaths)
            {
                var syntaxTree = ParseSyntaxTreeFile(filePath);
                files.Add(filePath, syntaxTree);
            }

            var sources = files.ToDictionary(tree => tree.Key, tree => tree.Value.ToString());

            var compilation = GetCompilationFromSyntaxTree(files.Values.ToList(), sources.Values.ToList());
            return new Project(this, compilation)
            {
                Trees = files,
                Sources = sources
            };
        }

        /// <summary>
        /// Parse all sources and returns project object
        /// </summary>
        /// <param name="sources">Key should be unuque identifier of source like filepath. Key is source code</param>
        /// <returns>Projectfile</returns>
        public Project GetProject(Dictionary<string, string> sources)
        {
            var files = new Dictionary<string, SyntaxTree>();
            foreach (var fileId in sources.Keys)
            {
                var syntaxTree = ParseSyntaxTreeText(sources[fileId], fileId);
                files.Add(fileId, syntaxTree);
            }

            var compilation = GetCompilationFromSyntaxTree(files.Values.ToList(), sources.Values.ToList());
            return new Project(this, compilation)
            {
                Trees = files,
                Sources = sources
            };
        }

        /// <summary>
        /// Get autocomplete suggestions based on the code
        /// </summary>
        /// <param name="code">Source code</param>
        /// <param name="pos">Cursor position. If null then the last symbol will be used</param>
        /// <returns>List of suggested items</returns>
        public List<AutoCompleteItem> GetAutoCompleteItems(string code, int? pos = null)
        {
            var syntaxTree = this.ParseSyntaxTreeText(code);
            var semanticModel = this.GetSemanticModelFromSyntaxTree(syntaxTree, code);
            return GetAutoCompleteItems(code, syntaxTree, semanticModel, pos);
        }

        /// <summary>
        /// Get autocomplete suggestions based on the code
        /// </summary>
        /// <param name="code">Source code</param>
        /// <param name="pos">Cursor position. If null then the last symbol will be used</param>
        /// <returns>List of suggested items</returns>
        public List<AutoCompleteItem> GetAutoCompleteItems(Project project, string fileId, int? pos = null)
        {
            var syntaxTree = project.Trees[fileId];
            var semanticModel = project.Compilation.GetSemanticModel(syntaxTree);
            return GetAutoCompleteItems(project.Sources[fileId], project.Trees[fileId], semanticModel, pos);
        }

        /// <summary>
        /// Get autocomplete suggestions based on the code
        /// </summary>
        /// <param name="code">Source code</param>
        /// <param name="pos">Cursor position. If null then the last symbol will be used</param>
        /// <returns>List of suggested items</returns>
        internal List<AutoCompleteItem> GetAutoCompleteItems(string code, SyntaxTree syntaxTree,
            SemanticModel semanticModel, int? pos = null)
        {
            bool isStaticContext = false;
            bool appendDefaultNamespaces = true;

            var position = GetCursorPosition(code, pos);

            var token = syntaxTree.GetRoot().FindToken(position);

            var synNode = token.Parent;

            var forUsing = GetParentByKind(synNode, GetUsingDirectiveCode());
            var forNew = GetParentByKind(synNode, GetNewKeywordCode());
            var errorType = false;

            if (token.Value.Equals(")") && token.Parent.RawKind == GetArgumentListCode())
            {
                synNode = token.Parent.Parent;

                //handle inline declaration
                forNew = false;
            }

            ITypeSymbol classType = semanticModel.GetTypeInfo(synNode).Type;

            if (classType != null && classType.TypeKind == TypeKind.Error)
            {
                if (!forNew)
                {
                    errorType = true;
                }
                classType = null;
            }
            else if (classType != null)
            {
                var symbol = semanticModel.GetSymbolInfo(synNode).Symbol;
                if (symbol is INamedTypeSymbol)
                {
                    isStaticContext = true;
                    appendDefaultNamespaces = false;
                }

                if (symbol is ILocalSymbol)
                {
                    appendDefaultNamespaces = false;
                }
            }

            var autoCompleteItems = new List<AutoCompleteItem>();

            if (errorType && !forUsing)
            {
                //error type means that token under cursor has unknown type we should not show any items in this case
                //unclude static symbols
                //var symbols = semanticModel.LookupSymbols(position,
                //									container: null);
                //autoCompleteItems.AddRange(GetAutoCompleteItemsFromSymbols(symbols, false, true, this.Language));

                return autoCompleteItems;
            }

            var isNamespace = synNode.Parent.RawKind == GetQualifiedNameCode();

            //check the namespace 
            var namespaceSymbolInfo = semanticModel.GetSymbolInfo(synNode);

            var namespaces = GetNamespaces(appendDefaultNamespaces, namespaceSymbolInfo);

            if (classType == null && (forUsing || isNamespace))
            {
                var usingNamespace = isNamespace ? synNode.Parent.ToFullString().Split(new[] {'\r', '\n'}).First() : "";
                if (namespaces.Count(n => n.StartsWith(usingNamespace)) > 1)
                {
                    foreach (var ns in namespaces)
                    {
                        string innerNameSpace = null;
                        if (namespaceSymbolInfo.Symbol != null)
                        {
                            innerNameSpace =
                                ns.Replace(namespaceSymbolInfo.Symbol.ToDisplayString(), "")
                                    .TrimStart('.')
                                    .Split('.')
                                    .First();
                        }
                        else
                        {
                            innerNameSpace =
                                ns.Replace(synNode.Parent.ToFullString(), "").TrimStart('.').Split('.').First();
                        }

                        if (innerNameSpace.Length > 0)
                        {
                            autoCompleteItems.Add(
                                new AutoCompleteItem()
                                {
                                    IsStatic = false,
                                    Name = innerNameSpace,
                                    ItemType = AutoCompleteItemType.Namespace
                                });
                        }
                    }
                }
            }

            if (!forUsing)
            {
                var symbols = semanticModel.LookupSymbols(
                    position,
                    container: classType ?? (INamespaceOrTypeSymbol) namespaceSymbolInfo.Symbol,
                    includeReducedExtensionMethods: !forNew);

                autoCompleteItems.AddRange(GetAutoCompleteItemsFromSymbols(symbols, forNew, isStaticContext,
                    this.Language));

                if (!forNew && classType == null && !isNamespace && !isStaticContext)
                {
                    //add static items
                    autoCompleteItems.AddRange(GetAutoCompleteItemsFromSymbols(symbols, false, true, this.Language));
                }
            }
            else
            {
                //include static classes in usings/imports

                var symbols =
                    semanticModel.LookupSymbols(position, container: (INamespaceOrTypeSymbol) namespaceSymbolInfo.Symbol)
                        .Where(s => s.Kind != SymbolKind.Namespace && s.Kind != SymbolKind.Method)
                        .ToImmutableArray();
                autoCompleteItems.AddRange(GetAutoCompleteItemsFromSymbols(symbols, false, true, this.Language));
            }

            return autoCompleteItems.Distinct(new AutoCompleteItemEqualityComparer()).OrderBy(i => i.Name).ToList();
        }

        public TokenTypeResult GetTokenType(string code, int? pos = null)
        {
            var result = new TokenTypeResult();

            var syntaxTree = this.ParseSyntaxTreeText(code);
            var semanticModel = this.GetSemanticModelFromSyntaxTree(syntaxTree, code);

            var position = GetCursorPosition(code, pos);

            var token = (SyntaxToken) syntaxTree.GetRoot().FindToken(position);

            var synNode = token.Parent;

            var forUsing = GetParentByKind(synNode, GetUsingDirectiveCode());

            if (!forUsing)
            {
                var argumentList = FindArgumentListRecursive(token.Parent);
                if (argumentList != null)
                {
                    result.IsInsideArgumentList = true;
                    result.PreviousArgumentListTokenTypes =
                        argumentList.ChildNodesAndTokens()
                            .Where(t => t.RawKind == GetArgumentCode())
                            .Select(
                                t =>
                                    semanticModel.GetTypeInfo(
                                        syntaxTree.GetRoot().FindToken(t.AsNode().Span.Start).Parent).Type)
                            .Select(t => t != null ? t.ToDisplayString() : null)
                            .ToArray();
                    result.RawArgumentsList = argumentList.ToString();
                    result.ParentLine = argumentList.GetLocation().GetLineSpan().StartLinePosition.Line;
                    result.ParentChar = argumentList.GetLocation().GetLineSpan().StartLinePosition.Character;
                }

                ITypeSymbol classType = semanticModel.GetTypeInfo(synNode).Type;

                if (classType != null)
                {
                    result.Type = classType.ToDisplayString();
                }
            }

            return result;
        }

        private SyntaxNode FindArgumentListRecursive(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            if (node.RawKind == GetArgumentListCode())
            {
                return node;
            }

            return FindArgumentListRecursive(node.Parent);
        }

        private bool GetParentByKind(SyntaxNode node, int kind)
        {
            if (node.RawKind == kind)
            {
                return true;
            }
            if (node.Parent != null)
            {
                return GetParentByKind(node.Parent, kind);
            }

            return false;
        }

        private List<string> GetNamespaces(bool appendDefaultNamespaces, SymbolInfo namespaceSymbolInfo)
        {
            var namespaces = new List<string>();

            if (appendDefaultNamespaces)
            {
                namespaces.AddRange(NamespaceToDllMap._map.Keys);
            }

            // here we can add custom namespaces

            if (namespaceSymbolInfo.Symbol != null && namespaceSymbolInfo.Symbol.Kind == SymbolKind.Namespace)
            {
                var symbolNameSpaces = new List<string>();

                // TODO: new Roslyn compatibility issue. new roslyn doesn't have Location property. Should be fixed with next Roslyn or reimplemented somehow
                symbolNameSpaces.AddRange(
                    ((INamespaceSymbol) namespaceSymbolInfo.Symbol).ConstituentNamespaces.OfType<INamespaceSymbol>()
                        .SelectMany(n => n.GetNamespaceMembers().Select(s => s.ToDisplayString()))
                        .Distinct());

                symbolNameSpaces.AddRange(namespaces);

                namespaces =
                    symbolNameSpaces.Distinct()
                        .Where(n => n != null && n.StartsWith(namespaceSymbolInfo.Symbol.ToDisplayString()))
                        .ToList();
            }

            return namespaces;
        }

        private int GetCursorPosition(string code, int? pos)
        {
            if (pos == null)
            {
                pos = code.Length;
            }

            var codeFragment = pos < code.Length ? code.Substring(0, pos.Value) : code;

            pos = (pos < code.Length ? pos : code.Length) - 1;

            var dotPos = codeFragment.LastIndexOf('.') - 1;
            var spacePos = codeFragment.LastIndexOf(' ') - 1;
            var openBracketPos = codeFragment.LastIndexOf('(');
            var closeBracketPos = codeFragment.LastIndexOf(')');

            if (openBracketPos > closeBracketPos && openBracketPos > dotPos && openBracketPos > spacePos)
            {
                return pos.Value;
            }

            if (dotPos > spacePos)
            {
                pos = dotPos;
            }

            return pos.Value;
        }

        internal MetadataReference CreateMetadataReference(string assemblyDisplayNameOrPath,
            bool? includeDocumentation = null)
        {
            if (!includeDocumentation.HasValue)
                includeDocumentation = _options.ParseDocumenation;

            string assemblyFullPath = File.Exists(assemblyDisplayNameOrPath)
                ? assemblyDisplayNameOrPath
                : AssemblyHelper.GetAssemblyLocation(assemblyDisplayNameOrPath);

            string xmlFile = Path.Combine(_systemXmlFilesDir, assemblyDisplayNameOrPath + ".xml");

            DocumentationProvider documentationProvider = null;
            if (includeDocumentation.Value && File.Exists(xmlFile))
            {
                documentationProvider = DocumentationProviderFactory.Create(xmlFile);
            }

            return new MetadataFileReference(assemblyFullPath, MetadataReferenceProperties.Assembly,
                documentationProvider);
        }

        private SemanticModel GetSemanticModelFromSyntaxTree(
            SyntaxTree syntaxTree,
            string code)
        {
            var compilation = this.GetCompilationFromSyntaxTree(syntaxTree, code);
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
            return semanticModel;
        }

        public Compilation GetCompilationFromSyntaxTree(SyntaxTree syntaxTree, string code)
        {
            return GetCompilationFromSyntaxTree(
                new List<SyntaxTree>() {syntaxTree},
                new List<string>() {code});
        }

        public Compilation GetCompilationFromSyntaxTree(
            List<SyntaxTree> syntaxTrees,
            List<string> codes)
        {
            var includeDocumentation = _options.ParseDocumenation;
            _systemXmlFilesDir = GetSystemXmlFilesDir();

            MetadataReference mscorlib = CreateMetadataReference("mscorlib", includeDocumentation);
            var metaDllReferences = new List<MetadataReference> {mscorlib};

            AppendDefaultMetadataReferences(metaDllReferences, includeDocumentation);

            List<string> dllNames = new List<string>();
            foreach (var code in codes)
            {
                List<string> gacDlls = GetGacDlls(code);
                foreach (var dllName in gacDlls)
                {
                    string dllNameWithoutExtension = dllName.Substring(0, dllName.Length - 4); //remove .dll

                    if (dllNames.IndexOf(dllNameWithoutExtension) == -1)
                    {
                        dllNames.Add(dllNameWithoutExtension);
                    }
                }
            }

            foreach (string dllName in dllNames)
            {
                MetadataReference metaRef = CreateMetadataReference(dllName, includeDocumentation);
                metaDllReferences.Add(metaRef);
            }

            //http://msdn.microsoft.com/en-us/vstudio/hh500769.aspx#Toc306015688

            Compilation compilation = this.CreateCompilation(
                Guid.NewGuid().ToString(),
                syntaxTrees: syntaxTrees.ToArray(),
                matadataReferences: metaDllReferences);

            return compilation;
        }

        private string GetSystemXmlFilesDir()
        {
			// TODO: this needs to be moved outside and used just for Windows as this method isn't cross platform
            string programFilesDir;
            //From http://stackoverflow.com/questions/194157/c-sharp-how-to-get-program-files-x86-on-windows-vista-64-bit
            if (8 == IntPtr.Size ||
                (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                programFilesDir = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }
            else
            {
                programFilesDir = Environment.GetEnvironmentVariable("ProgramFiles");
            }

            //Ex C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5

            string dir = Path.Combine(programFilesDir, "Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.5");
            return dir;
        }

        private static IEnumerable<AutoCompleteItem> GetAutoCompleteItemsFromSymbols(
            ImmutableArray<ISymbol> symbols,
            bool forNewKeyword,
            bool isStaticContext,
            Language language)
        {
            var autoCompleteItemsQuery = symbols.ToList().AsParallel();

            //if for new keyowrd - get only named types or namespaces
            //if in staticcontext - get static members only
            if (forNewKeyword)
            {
                autoCompleteItemsQuery =
                    autoCompleteItemsQuery.Where(
                        s =>
                            !s.IsStatic && !s.IsVirtual && !s.IsAbstract
                            && (s.Kind == SymbolKind.NamedType || s.Kind == SymbolKind.Namespace));
            }
            else
            {
                if (language == Language.CSharp)
                {
                    autoCompleteItemsQuery = autoCompleteItemsQuery.Where(s => s.IsStatic == isStaticContext);
                }

                else if (language == Language.VbNet)
                {
                    autoCompleteItemsQuery =
                        autoCompleteItemsQuery.Where(
                            s =>
                                (s.Kind != SymbolKind.NamedType && s.IsStatic == isStaticContext)
                                || (s.Kind == SymbolKind.NamedType && isStaticContext
                                    && ((INamedTypeSymbol) s).GetMembers()
                                        .Any(
                                            m =>
                                                m.Kind == SymbolKind.Method && m.IsStatic && m.CanBeReferencedByName
                                                && m.DeclaredAccessibility == Accessibility.Public)));
                }
            }

            var autoCompleteItems = autoCompleteItemsQuery.SelectMany(i => GetAutoCompleteItem(i, !forNewKeyword));

            return autoCompleteItems.Distinct(new AutoCompleteItemEqualityComparer());
        }

        private static IEnumerable<AutoCompleteItem> GetAutoCompleteItem(ISymbol symbol, bool showClassesAsStatic)
        {
            var result = new List<AutoCompleteItem>();

            var item = new AutoCompleteItem {Name = symbol.Name};

            var itemDoc = symbol.GetDocumentationCommentXml(CultureInfo.GetCultureInfo("en-US"));

            DocumentationComment comment = null;
            if (!string.IsNullOrWhiteSpace(itemDoc))
            {
                comment = DocumentationComment.FromXmlFragment(itemDoc);

                item.Description = comment.SummaryText;
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    item.ItemType = AutoCompleteItemType.Method;
                    var methodSymbol = (IMethodSymbol) symbol;
                    item.IsExtension = methodSymbol.IsExtensionMethod;
                    item.IsStatic = methodSymbol.IsStatic;
                    item.Type = methodSymbol.ReturnsVoid ? "void" : methodSymbol.ReturnType.Name;

                    // formatting complicated types name like arrays
                    if (string.IsNullOrWhiteSpace(item.Type))
                        item.Type = GetParameterTypeName(methodSymbol.ReturnType);

                    //args
                    item.Params = GetSymbolParameters(methodSymbol.Parameters, comment);
                    item.IsGeneric = methodSymbol.IsGenericMethod;
                    break;
                case SymbolKind.Local:
                    item.ItemType = AutoCompleteItemType.Variable;
                    var localSymbol = (ILocalSymbol) symbol;
                    item.Type = localSymbol.Type.Name;
                    break;
                case SymbolKind.Field:
                    item.ItemType = AutoCompleteItemType.Variable;
                    var fieldSymbol = (IFieldSymbol) symbol;
                    item.Type = fieldSymbol.Type.Name;
                    break;
                case SymbolKind.Property:
                    item.ItemType = AutoCompleteItemType.Property;
                    var propertySymbol = (IPropertySymbol) symbol;
                    item.Type = propertySymbol.Type.Name;
                    break;
                case SymbolKind.Namespace:
                    item.ItemType = AutoCompleteItemType.Namespace;
                    var namespaceSymbol = (INamespaceSymbol) symbol;
                    item.Name = namespaceSymbol.Name;
                    break;
                case SymbolKind.NamedType:
                    item.ItemType = AutoCompleteItemType.Class;
                    var classSymbol = (INamedTypeSymbol) symbol;
                    item.Name = classSymbol.Name;
                    item.IsStatic = showClassesAsStatic || classSymbol.IsStatic;
                    item.IsGeneric = classSymbol.IsGenericType;

                    if (!showClassesAsStatic)
                    {
                        var constructors = classSymbol.Constructors;
                        foreach (var constructor in constructors)
                        {
                            itemDoc = constructor.GetDocumentationCommentXml(CultureInfo.GetCultureInfo("en-US"));

                            DocumentationComment doc = null;
                            if (!string.IsNullOrWhiteSpace(itemDoc))
                            {
                                doc = DocumentationComment.FromXmlFragment(itemDoc);
                            }

                            var consItem = (AutoCompleteItem) item.Clone();
                            if (doc != null && doc.SummaryText != null)
                            {
                                consItem.Description = GetItemDescription(doc.SummaryText);
                            }
                            consItem.Params = GetSymbolParameters(constructor.Parameters, doc);
                            result.Add(consItem);
                        }
                    }
                    break;
            }

            if (result.Count == 0)
            {
                result.Add(item);
            }

            return result;
        }

        private static string GetItemDescription(string rawDesc)
        {
            var result = rawDesc;
            var match = _crefRegex.Match(result);
            while (match.Success)
            {
                result = result.Replace(match.Value.Trim(), "<b>" + match.Groups[1].Value.Split('.').Last() + "</b>");
                match = _crefRegex.Match(result);
            }

            return result;
        }

        private static AutoCompleteItemParameter[] GetSymbolParameters(
            ImmutableArray<IParameterSymbol> paramsArray,
            DocumentationComment comment,
            bool includeThis = false)
        {
            var result =
                paramsArray.Where(p => !includeThis || !p.IsThis)
                    .Select(
                        p =>
                            new AutoCompleteItemParameter()
                            {
                                Name = p.Name,
                                Type = GetParameterTypeName(p.Type),
                                Description = comment != null ? comment.GetParameterText(p.Name) : null,
                                IsParams = p.IsParams,
                                IsOptional = p.IsOptional
                            })
                    .ToArray();
            return result.Length == 0 ? null : result;
        }

        private static string GetParameterTypeName(ITypeSymbol type)
        {
            var symbol = type as IPointerTypeSymbol;
            if (symbol != null)
            {
                return symbol.PointedAtType.ToDisplayString() + "*";
            }
            var symbol2 = type as IArrayTypeSymbol;
            if (symbol2 != null)
            {
                return symbol2.ElementType.ToDisplayString() + "[]";
            }

            var symbol3 = type as INamedTypeSymbol;
            if (symbol3 != null && symbol3.TypeArguments.Length > 0)
            {
                return symbol3.ConstructedFrom.ToDisplayString() + "<"
                       + String.Join(", ", symbol3.TypeArguments.Select(t => t.ToDisplayString())) + ">";
            }

            return type.ToDisplayString();
        }

        protected virtual List<string> GetGacDlls(string code)
        {
            List<string> referencedDlls = NamespaceToDllMap.GetDlls(this.GetUsedNamespaces(code));

            // expressions require System.Core assemblies, so we can't understand when to add it, so we will add it for every run
            if (!referencedDlls.Contains("System.Core.dll"))
            {
                referencedDlls.Add("System.Core.dll");
            }

            // used by dynamic keyword
            if (!referencedDlls.Contains("Microsoft.CSharp.dll"))
            {
                referencedDlls.Add("Microsoft.CSharp.dll");
            }

            return referencedDlls;
        }

        /// <summary>
        /// Get list of namespaces that used in code block
        /// </summary>
        /// <param name="code">Source code</param>
        /// <returns>List of namespaces</returns>
        public List<string> GetUsedNamespaces(string code)
        {
            string pattern = GetUsingNamespaceLinePattern();

            var regexOpts = RegexOptions.Multiline | RegexOptions.IgnoreCase;
            Regex regex = new Regex(pattern, regexOpts);

            var usingNamespaces = new List<string>();

            if (!String.IsNullOrEmpty(code))
            {
                var matches = regex.Matches(code);
                foreach (Match match in matches)
                {
                    usingNamespaces.Add(match.Groups[1].Value);
                }
            }

            return usingNamespaces;
        }
    }
}