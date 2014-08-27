using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DotNetFiddle.IntelligentCompletion.Tests.Project
{
    [TestFixture]
    public class VBNetTests
    {
        [Test]
        public void AddReferenceWorks()
        {
            var sourceMain = @"Imports System
Imports System.Configuration

Public Class Program

    Public Shared Sub()
        ConfigurationManager.AppSettings.
    End Sub
End Class";

            var service = new VBNetLanguageService();

            var files = new Dictionary<string, string>()
            {
                {"main.vb", sourceMain}
            };

            var project = service.GetProject(files);
            Assert.IsNotNull(project);
            var configIdentity = project.ReferencedAssemblies.FirstOrDefault(i => i.Name == "System.Configuration");

            Assert.IsNull(configIdentity);

            project.AddReference("System.Configuration");

            configIdentity = project.ReferencedAssemblies.FirstOrDefault(i => i.Name == "System.Configuration");
            Assert.IsNotNull(configIdentity);

            int dotIndex = sourceMain.LastIndexOf(".") + 1;
            var autoCompleteItems = service.GetAutoCompleteItems(project, "main.vb", dotIndex);
            Assert.IsTrue(autoCompleteItems.Any());

            project.RemoveReference("System.Configuration");
            configIdentity = project.ReferencedAssemblies.FirstOrDefault(i => i.Name == "System.Configuration");

            Assert.IsNull(configIdentity);
        }
    }
}