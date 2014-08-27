using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DotNetFiddle.IntelligentCompletion.Tests.Project
{
    [TestFixture]
    public class CSharpProjectTextTests
    {
        [Test]
        public void Project_Works()
        {
            var source = @"using System;

public class Dog
{
	/// <summary>
	/// Dog name
	/// </summary>
    public string Name {get;set;}
}";

            var sourceMain = @"using System;

public class Program
{
    public static void Main()
    {
        var dog = new Dog();
        dog.
    }
}";

            var service = new CSharpLanguageService();

            var files = new Dictionary<string, string>()
            {
                {"dog.cs", source},
                {"main.cs", sourceMain}
            };

            var project = service.GetProject(files);
            Assert.IsNotNull(project);

            int dotIndex = sourceMain.IndexOf(".") + 1;
            var autoCompleteItems = service.GetAutoCompleteItems(project, "main.cs", dotIndex);
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
            var source = @"using System;

public class Dog
{
	/// <summary>
	/// Dog name
	/// </summary>
    public string Name {get;set;}
}";

            var sourceMain = @"using System;

public class Program
{
    public static void Main()
    {
        var dog = new Dog();
        dog.
    }
}";

            var service = new CSharpLanguageService();

            var files = new Dictionary<string, string>()
            {
                {"dog.cs", source},
                {"main.cs", sourceMain}
            };

            var project = service.GetProject(files);
            Assert.IsNotNull(project);

            int dotIndex = sourceMain.IndexOf(".") + 1;
            var autoCompleteItems = service.GetAutoCompleteItems(project, "main.cs", dotIndex);
            Assert.IsTrue(autoCompleteItems.Any());

            var property =
                autoCompleteItems.FirstOrDefault(p => p.ItemType == AutoCompleteItemType.Property && p.Name == "Name");

            Assert.IsNotNull(property, "property should be returned from language service");

            source = @"using System;

public class Dog
{
	/// <summary>
	/// Dog name
	/// </summary>
    public string Name {get;set;}

    public DateTime Birthdate {get;set;}
}";

            project.ReplaceSourceFile("dog.cs", source);

            autoCompleteItems = service.GetAutoCompleteItems(project, "main.cs", dotIndex);
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