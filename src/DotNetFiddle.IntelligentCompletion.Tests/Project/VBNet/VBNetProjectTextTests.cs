using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DotNetFiddle.IntelligentCompletion.Tests.Project
{
    [TestFixture]
    public class VBNetProjectTextTests
    {
        [Test]
        public void Project_Works()
        {
            var source = @"Imports System

Public Class Dog

''' <summary>
	''' Dog name
	''' </summary>
    Public property Name As String

End Class";

            var sourceMain = @"Imports System

Public Class Program

    Public Shared Sub Main()
    {
        Dim dog as Dog = New Dog()
        dog.
    End Sub
End Class";

            var service = new VBNetLanguageService();

            var files = new Dictionary<string, string>()
            {
                {"dog.vb", source},
                {"main.vb", sourceMain}
            };

            var project = service.GetProject(files);
            Assert.IsNotNull(project);

            int dotIndex = sourceMain.IndexOf(".") + 1;
            var autoCompleteItems = service.GetAutoCompleteItems(project, "main.vb", dotIndex);
            Assert.IsTrue(autoCompleteItems.Any());

            var propertyName =
                autoCompleteItems.FirstOrDefault(p => p.ItemType == AutoCompleteItemType.Property && p.Name == "Name");

            Assert.IsNotNull(propertyName, "property should be returned from language service");
            Assert.AreEqual("Name", propertyName.Name);
            Assert.AreEqual("String", propertyName.Type);
            Assert.IsFalse(propertyName.IsStatic);
            Assert.IsFalse(propertyName.IsGeneric);
            Assert.IsFalse(propertyName.IsExtension);
            Assert.AreEqual(AutoCompleteItemType.Property, propertyName.ItemType);
            Assert.AreEqual("Dog name", propertyName.Description);
        }

        [Test]
        public void Project_Changed_Works()
        {
            var source = @"Imports System

Public Class Dog

''' <summary>
	''' Dog name
	''' </summary>
    Public property Name As String

End Class";

            var sourceMain = @"Imports System

Public Class Program

    Public Shared Sub Main()
    {
        Dim dog as Dog = New Dog()
        dog.
    End Sub
End Class";


            var service = new VBNetLanguageService();

            var files = new Dictionary<string, string>()
            {
                {"dog.vb", source},
                {"main.vb", sourceMain}
            };

            var project = service.GetProject(files);
            Assert.IsNotNull(project);

            int dotIndex = sourceMain.IndexOf(".") + 1;
            var autoCompleteItems = service.GetAutoCompleteItems(project, "main.vb", dotIndex);
            Assert.IsTrue(autoCompleteItems.Any());

            var property =
                autoCompleteItems.FirstOrDefault(p => p.ItemType == AutoCompleteItemType.Property && p.Name == "Name");

            Assert.IsNotNull(property, "property should be returned from language service");

            source = @"Imports System

Public Class Dog

	''' <summary>
	''' Dog name
	''' </summary>
    Public Property Name as String

    Public Property Birthdate As DateTime 
End Class";

            project.ReplaceSourceFile("dog.vb", source);

            autoCompleteItems = service.GetAutoCompleteItems(project, "main.vb", dotIndex);
            Assert.IsTrue(autoCompleteItems.Any());

            property =
                autoCompleteItems.FirstOrDefault(p => p.ItemType == AutoCompleteItemType.Property && p.Name == "Name");

            Assert.IsNotNull(property, "property Name should be returned from language service");

            property =
    autoCompleteItems.FirstOrDefault(p => p.ItemType == AutoCompleteItemType.Property && p.Name == "Birthdate");

            Assert.IsNotNull(property, "property Birthdate should be returned from language service");

        }

    }
}