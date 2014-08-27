using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace DotNetFiddle.IntelligentCompletion.Tests.Project
{
    [TestFixture]
    public class VBNetProjectFileTests
    {
        [Test]
        public void Project_Works()
        {
            var service = new VBNetLanguageService();

            var currentPath = AssemblyHelper.GetAssemblyDirectory(typeof (CSharpProjectFileTests).Assembly);
            currentPath = Path.Combine(currentPath, "Samples", "VBNet");

            string dogMainPath = Path.Combine(currentPath, "DogMain.vb");
            string dogPath = Path.Combine(currentPath, "DogOneProperty.vb");

            var files = new List<string>()
            {
                dogMainPath, dogPath
            };

            var project = service.GetProject(files);
            Assert.IsNotNull(project);

            var sourceMain = project.Sources[dogMainPath];
            int dotIndex = sourceMain.IndexOf(".") + 1;
            var autoCompleteItems = service.GetAutoCompleteItems(project, dogMainPath, dotIndex);
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
            var service = new VBNetLanguageService();

            var currentPath = AssemblyHelper.GetAssemblyDirectory(typeof (CSharpProjectFileTests).Assembly);
            currentPath = Path.Combine(currentPath, "Samples", "VBNet");

            string dogMainPath = Path.Combine(currentPath, "DogMain.vb");
            string dogPath = Path.Combine(currentPath, "DogOneProperty.vb");
            string dogNewPath = Path.Combine(currentPath, "DogTwoProperties.vb");

            var files = new List<string>()
            {
                dogMainPath,
                dogPath
            };

            var project = service.GetProject(files);
            Assert.IsNotNull(project);

            var sourceMain = project.Sources[dogMainPath];
            int dotIndex = sourceMain.IndexOf(".") + 1;
            var autoCompleteItems = service.GetAutoCompleteItems(project, dogMainPath, dotIndex);
            Assert.IsTrue(autoCompleteItems.Any());

            var property =
                autoCompleteItems.FirstOrDefault(p => p.ItemType == AutoCompleteItemType.Property && p.Name == "Name");
            Assert.IsNotNull(property, "property Name should be returned from language service");

            var newDogContent = File.ReadAllText(dogNewPath);
            project.ReplaceSourceFile(dogPath, newDogContent);

            autoCompleteItems = service.GetAutoCompleteItems(project, dogMainPath, dotIndex);
            property =
                autoCompleteItems.FirstOrDefault(p => p.ItemType == AutoCompleteItemType.Property && p.Name == "Name");

            Assert.IsNotNull(property, "property Name should be returned from language service");

            property =
                autoCompleteItems.FirstOrDefault(
                    p => p.ItemType == AutoCompleteItemType.Property && p.Name == "Birthdate");

            Assert.IsNotNull(property, "property Birthdate should be returned from language service");
        }

    }
}