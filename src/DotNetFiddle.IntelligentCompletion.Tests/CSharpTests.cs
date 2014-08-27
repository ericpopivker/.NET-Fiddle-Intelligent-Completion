using System.Linq;

using NUnit.Framework;

namespace DotNetFiddle.IntelligentCompletion.Tests
{
	[TestFixture]
    public class CSharpTests
	{
	    [Test]
		public void WhenStringBuilder_Works()
		{
			string codeInFile = @"using System; using System.Text;
				class Program
				{
					public void Main()
					{
						var sb= new StringBuilder();
						sb.";

			var service = new CSharpLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
			Assert.IsNotEmpty(autoCompleteItems);
		}

		[Test]
		public void WhenStringBuilder_FullCode_Works()
		{
			string codeInFile = @"using System; using System.Text;

				class Program
				{
					public void Main()
					{
						var sb= new StringBuilder();
						sb.
					}

				}";

			var service = new CSharpLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile, codeInFile.Length - 3);
			Assert.IsNotEmpty(autoCompleteItems);
            Assert.IsNotEmpty(autoCompleteItems.Where(a => a.Name == "Append"));
            Assert.IsNotEmpty(autoCompleteItems.Where(a => a.Name == "AppendLine"));
            Assert.IsNotEmpty(autoCompleteItems.Where(a => a.Name == "Clear"));
        }

		[Test]
		public void WhenExtensionStringBuilder_Works()
		{
			string codeInFile = @"using System; using System.Text;

				static class Extension
				{
					public static void Test(this StringBuilder builder)
					{
					}
				}

				class Program
				{
					public void Main()
					{
						var sb= new StringBuilder();
						sb.
					}

				}";

			var service = new CSharpLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile, codeInFile.Length - 3);
			Assert.IsNotEmpty(autoCompleteItems);
			Assert.IsNotNull(autoCompleteItems.FirstOrDefault(a => a.Name == "Test" && a.Type == "void"));
		}


		[Test]
		public void Constructor_Works()
		{
			string codeInFile = @"using System; 
				class Program
				{

					/// <summary>
					/// Program constructor
					/// </summary>
					/// <param name=""test"">Test parameter</param>
					public Program(string test)
					{
					}

					public void Main()
					{
						var sb= new Program(";

			var service = new CSharpLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
			Assert.IsNotEmpty(autoCompleteItems);

			var programConstructor = autoCompleteItems.FirstOrDefault(a => a.Name == "Program");
			Assert.IsNotNull(programConstructor);
			Assert.AreEqual(programConstructor.Description, "Program constructor");
		}

        [Test]
        public void DisabledDocumentationDoesntDisplay()
        {
            string codeInFile = @"using System; using System.Text;

				class Program
				{
					public void Main()
					{
						var sb= new StringBuilder();
						sb.";


            var service = new CSharpLanguageService(null, new LanguageServiceOptions()
            {
                ParseDocumenation = false
            });
            var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
            Assert.IsNotEmpty(autoCompleteItems);

            var appendMethod = autoCompleteItems.FirstOrDefault(a => a.Name == "Append");
            Assert.IsNotNull(appendMethod);
            Assert.IsNullOrEmpty(appendMethod.Description);
        }

		[Test]
		public void Namespace_Works()
		{
			string codeInFile = @"using System; 
using S";

			var service = new CSharpLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
			Assert.IsNotEmpty(autoCompleteItems);
		}

		[Test]
		public void NamespaceSystem_Works()
		{
			string codeInFile = @"using System; 
using System.";

			var service = new CSharpLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
			Assert.IsNotEmpty(autoCompleteItems);
		}


        [Test]
        public void NewMethod_Works()
        {
            string codeInFile = @"using System; using System.Text;
				class Program
				{
                    public void TestMethod(){}

					public void Main()
					{
						this.";

            var service = new CSharpLanguageService();
            var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
            Assert.IsNotEmpty(autoCompleteItems);

            var method =
                autoCompleteItems.FirstOrDefault(
                    p => p.ItemType == AutoCompleteItemType.Method && p.Name == "TestMethod");

            Assert.IsNotNull(method, "Method TestMethod should be present");
            Assert.AreEqual("void", method.Type);
        }

        [Test]
        public void NewStaticMethod_Works()
        {
            string codeInFile = @"using System; using System.Text;
				class Program
				{
                    public static void TestMethod(){}

					public void Main()
					{
						Program.";

            var service = new CSharpLanguageService();
            var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
            Assert.IsNotEmpty(autoCompleteItems);

            var method =
                autoCompleteItems.FirstOrDefault(
                    p => p.ItemType == AutoCompleteItemType.Method && p.Name == "TestMethod");

            Assert.IsNotNull(method, "Method TestMethod should be present");
            Assert.AreEqual("void", method.Type);
        }

        [Test]
        public void NewStaticMethod_InNonStatic_Ignored()
        {
            string codeInFile = @"using System; using System.Text;
				class Program
				{
                    public void TestMethod(){}

					public static void Main()
					{
						Program.";

            var service = new CSharpLanguageService();
            var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
            Assert.IsNotEmpty(autoCompleteItems);

            var method =
                autoCompleteItems.FirstOrDefault(
                    p => p.ItemType == AutoCompleteItemType.Method && p.Name == "TestMethod");

            Assert.IsNull(method, "Method TestMethod shouldn't be present");
        }

    }
}