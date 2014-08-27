using System.Linq;

using NUnit.Framework;

namespace DotNetFiddle.IntelligentCompletion.Tests
{
	[TestFixture]
    public class VBNetTests
	{
        // TODO: remove copypaste and make common inheritor for the same tests

	    [Test]
		public void WhenStringBuilder_Works()
		{
            string codeInFile = @"Imports System
Imports System.Text

Public Module Module1
	Public Sub Main()
		Dim sb As StringBuilder = New StringBuilder
        sb.";

			var service = new VBNetLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
			Assert.IsNotEmpty(autoCompleteItems);
		}

		[Test]
		public void WhenStringBuilder_FullCode_Works()
		{
            string codeInFile = @"Imports System
Imports System.Text

Public Module Module1
	Public Sub Main()
		Dim sb As StringBuilder = New StringBuilder
        sb.
    End Sub
End Module";

			var service = new VBNetLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile, codeInFile.LastIndexOf('.') + 1);
			Assert.IsNotEmpty(autoCompleteItems);
            Assert.IsNotEmpty(autoCompleteItems.Where(a => a.Name == "Append"));
            Assert.IsNotEmpty(autoCompleteItems.Where(a => a.Name == "AppendLine"));
            Assert.IsNotEmpty(autoCompleteItems.Where(a => a.Name == "Clear"));
        }

		[Test]
		public void WhenExtensionStringBuilder_Works()
		{
			string codeInFile = @"Imports System
Imports System.Text
Imports System.Runtime.CompilerServices

Module StringBuilderExtensions
    
    <Extension()> 
    Public Sub Test(ByVal builder as StringBuilder)
    End Sub
End Module

Public Module Module1
	Public Sub Main()
		Dim sb As StringBuilder = New StringBuilder
        sb.
    End Sub
End Module";

			var service = new VBNetLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile, codeInFile.LastIndexOf('.') + 1);
			Assert.IsNotEmpty(autoCompleteItems);
			Assert.IsNotNull(autoCompleteItems.FirstOrDefault(a => a.Name == "Test" && a.Type == "void"));
		}


		[Test]
		public void Constructor_Works()
		{
			string codeInFile = @"Imports System 

Public Class Program

	''' <summary>
	''' Program constructor
	''' </summary>
	''' <param name=""test"">Test parameter</param>
	Public Sub New(ByVal test as String)
	
	End Sub

	Public Sub Main()
		Dim sb as Program = New Program(";

			var service = new VBNetLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
			Assert.IsNotEmpty(autoCompleteItems);

			var programConstructor = autoCompleteItems.FirstOrDefault(a => a.Name == "Program");
			Assert.IsNotNull(programConstructor);
			Assert.AreEqual(programConstructor.Description, "Program constructor");
		}

        [Test]
        public void DisabledDocumentationDoesntDisplay()
        {
            string codeInFile = @"Imports System
Imports System.Text

Public Module Module1
	Public Sub Main()
		Dim sb As StringBuilder = New StringBuilder
        sb.";

            var service = new VBNetLanguageService(null, new LanguageServiceOptions()
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
			string codeInFile = @"Imports System
Imports S";

			var service = new VBNetLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
			Assert.IsNotEmpty(autoCompleteItems);
		}

		[Test]
		public void NamespaceSystem_Works()
		{
			string codeInFile = @"Imports System
Imports System.";

			var service = new VBNetLanguageService();
			var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
			Assert.IsNotEmpty(autoCompleteItems);
		}


        [Test]
        public void NewMethod_Works()
        {
            string codeInFile = @"Imports System

Public Class Program

    Public Sub TestMethod()
    End Sub

    Public Sub Main()
	    Me.";

            var service = new VBNetLanguageService();
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
            string codeInFile = @"Imports System

Public Class Program

    Public Shared Sub TestMethod()
    End Sub

    Public Sub Main()
	    Program.";

            var service = new VBNetLanguageService();
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
            string codeInFile = @"Imports System

Public Class Program

    Public Sub TestMethod()
    End Sub

    Public Shared Sub Main()
	    Program.";

            var service = new VBNetLanguageService();
            var autoCompleteItems = service.GetAutoCompleteItems(codeInFile);
            Assert.IsNotEmpty(autoCompleteItems);

            var method =
                autoCompleteItems.FirstOrDefault(
                    p => p.ItemType == AutoCompleteItemType.Method && p.Name == "TestMethod");

            Assert.IsNull(method, "Method TestMethod shouldn't be present");
        }

    }
}