.NET Fiddle Intelligent Completion
===================


This is part of code that used in [DotNetFiddle](https://dotnetfiddle.net) for intellisense purposes.
It based on [Roslyn](https://roslyn.codeplex.com/) and at the moment supports two languages:
- C#
- VBNet

----------


Samples
-------------
### One file example

Lets assume that you have such user code:

	using System; 
	using System.Text;
	
	class Program
	{
		public void Main()
		{
			var sb= new StringBuilder();
			sb.
	
Then using LanguageService classes you can get list of suggested items in the following way by passing this user code to `GetAutoCompleteItems` method:

	var service = new CSharpLanguageService();
	List<AutoCompleteItem> autoCompleteItems = service.GetAutoCompleteItems(SourceCodeString);

Method `GetAutoCompleteItems` also has `int` parameter that can be used to specify where is user cursor are. If parameter is omitted, then it will use the latest symbol in the source code.

### Project examples
For use cases when there are a lot of files and external references Intelligent Completion supports simple Project system. It has `Project` class with following signature:

    public class Project
    {
        public Project(LanguageService languageService, Compilation compilation){}
        
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
        public void AddReference(string displayNameOrPath){}
        /// <summary>
        /// Remove reference from project
        /// </summary>
        /// <param name="displayNameOrPath">Assembly name like System.Xml which can be used for search or full path assembly</param>
        public void RemoveReference(string displayNameOrPath){}
        #endregion

        /// <summary>
        /// Replace loaded source file by new one if it's changed
        /// </summary>
        /// <param name="fileId">File ID or path</param>
        /// <param name="source">source code</param>
        public void ReplaceSourceFile(string fileId, string source){}


        /// <summary>
        /// Reload changed file from file system
        /// </summary>
        /// <param name="fileId">File path</param>
        public void ReloadSourceFile(string fileId){}

LanguageService can return `Project` instance for list of files passed to it, so this project and be used by adding\removing assembly reference and updating changed file. Main purpose of project is to cache already parsed source files, so we don't need to parse all files each time and we can parse just changed files by using `ReloadSourceFile`,`ReplaceSourceFile` methods. It should increase performance for typical use cases.

Usage sample:

Lets assume that we have two files:

	using System;
	
	public class Dog
	{
	    public string Name {get;set;}
	}
And

	using System;
	
	public class Program
	{
	    public static void Main()
	    {
	        var dog = new Dog();
	        dog.
	    }
	}

Then we can use Intelligent Completionin such way:

	var service = new CSharpLanguageService();
	
	var files = new Dictionary<string, string>()
	{
		{"dog.cs", dogSource},
		{"main.cs", mainSource}
	};
	
	var project = service.GetProject(files);
	
	// here we need to know where user cursor is. But for sample we just search for the last . symbol
	int dotIndex = sourceMain.IndexOf(".") + 1;
	var autoCompleteItems = service.GetAutoCompleteItems(project, "main.cs", dotIndex);
	// autoCompleteItems will contain Name property

In case if Dog class is changed, then we can reload it with new source in following way:

	project.ReplaceSourceFile("dog.cs", newSource);

`"dog.cs"` is unique identifier of that source file that will be used during auto completion. We can also specify full path to files on file system, and then identifier will be full path to the file.


There are a couple of nUnit tests that can be also checked for use cases.