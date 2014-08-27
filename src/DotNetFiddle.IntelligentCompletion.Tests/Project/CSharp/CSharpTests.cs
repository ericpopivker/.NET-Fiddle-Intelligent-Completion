using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DotNetFiddle.IntelligentCompletion.Tests.Project
{
    [TestFixture]
    public class CSharpTests
    {
        [Test]
        public void AddReferenceWorks()
        {
            var sourceMain = @"using System;
using System.Configuration;

public class Program
{
    public static void Main()
    {
        ConfigurationManager.AppSettings.
    }
}";

            var service = new CSharpLanguageService();

            var files = new Dictionary<string, string>()
            {
                {"main.cs", sourceMain}
            };

            var project = service.GetProject(files);
            Assert.IsNotNull(project);
            var configIdentity = project.ReferencedAssemblies.FirstOrDefault(i => i.Name == "System.Configuration");

            Assert.IsNull(configIdentity);

            project.AddReference("System.Configuration");

            configIdentity = project.ReferencedAssemblies.FirstOrDefault(i => i.Name == "System.Configuration");
            Assert.IsNotNull(configIdentity);

            int dotIndex = sourceMain.LastIndexOf(".") + 1;
            var autoCompleteItems = service.GetAutoCompleteItems(project, "main.cs", dotIndex);
            Assert.IsTrue(autoCompleteItems.Any());

            project.RemoveReference("System.Configuration");
            configIdentity = project.ReferencedAssemblies.FirstOrDefault(i => i.Name == "System.Configuration");

            Assert.IsNull(configIdentity);
        }
    }
}