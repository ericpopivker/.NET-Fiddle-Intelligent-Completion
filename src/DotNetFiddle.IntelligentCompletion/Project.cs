using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DotNetFiddle.IntelligentCompletion
{
    public class Project
    {
        public Project(LanguageService languageService, Compilation compilation)
        {
            LanguageService = languageService;
            Compilation = compilation;
        }

        /// <summary>
        /// Roslyn complication
        /// </summary>
        public Compilation Compilation { get; private set; }

        /// <summary>
        /// Language service that used for project creation
        /// </summary>
        public LanguageService LanguageService { get; private set; }

        /// <summary>
        /// Mapping between File IDs and Roslyn's SyntaxTrees
        /// </summary>
        public Dictionary<string, SyntaxTree> Trees { get; internal set; }

        /// <summary>
        /// Mapping between File IDs and sources. File ID can be path to the file or some unique string that will identify this source
        /// </summary>
        public Dictionary<string, string> Sources { get; internal set; }

        #region References

        /// <summary>
        /// List of already referenced assemblies
        /// </summary>
        public IEnumerable<AssemblyIdentity> ReferencedAssemblies
        {
            get { return Compilation.ReferencedAssemblyNames; }
        }

        /// <summary>
        /// Add reference to the project
        /// </summary>
        /// <param name="displayNameOrPath">Assembly name like System.Xml which can be used for search or full path assembly</param>
        public void AddReference(string displayNameOrPath)
        {
            var reference = LanguageService.CreateMetadataReference(displayNameOrPath);
            Compilation = Compilation.AddReferences(reference);
        }

        /// <summary>
        /// Remove reference from project
        /// </summary>
        /// <param name="displayNameOrPath">Assembly name like System.Xml which can be used for search or full path assembly</param>
        public void RemoveReference(string displayNameOrPath)
        {
            // we need to find metadata reference to get Display property with assembly full path, and then we can remove it
            var newReference = LanguageService.CreateMetadataReference(displayNameOrPath);
            var existingReference = Compilation.References.FirstOrDefault(r => r.Display == newReference.Display);
            Compilation = Compilation.RemoveReferences(existingReference);
        }

        #endregion

        /// <summary>
        /// Replace loaded source file by new one if it's changed
        /// </summary>
        /// <param name="fileId">File ID or path</param>
        /// <param name="source">source code</param>
        public void ReplaceSourceFile(string fileId, string source)
        {
            var oldSourceTree = Trees[fileId];
            var newSourceTree = LanguageService.ParseSyntaxTreeText(source, fileId);

            Trees[fileId] = newSourceTree;
            Sources[fileId] = source;

            // replacing new tree
            Compilation = Compilation.ReplaceSyntaxTree(oldSourceTree, newSourceTree);
        }


        /// <summary>
        /// Reload changed file from file system
        /// </summary>
        /// <param name="fileId">File path</param>
        public void ReloadSourceFile(string fileId)
        {
            var oldSourceTree = Trees[fileId];
            var newSourceTree = LanguageService.ParseSyntaxTreeFile(fileId);

            Trees[fileId] = newSourceTree;
            Sources[fileId] = newSourceTree.ToString();

            // replacing new tree
            Compilation = Compilation.ReplaceSyntaxTree(oldSourceTree, newSourceTree);
        }


    }
}