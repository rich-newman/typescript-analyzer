using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebLinterTest
{
    [TestClass]
    public class zzGenerateCsprojEntries
    {
        private string result;
        private int pathLocation;
        [TestMethod]
        public void GenerateEntriesTest()
        {
            // Results are in typescript-analyzer\src\WebLinterVsix\TypeScriptAnalyzerNode\temp.txt
            // and need to be used to update WebLinterVsix.csproj
            result = "";
            string assemblyDirectory = WebLinter.LinterFactory.ExecutionPath;
            assemblyDirectory = assemblyDirectory.Replace("WebLinterTest\\bin\\Debug", "WebLinterVsix");
            Assert.IsTrue(Directory.Exists(assemblyDirectory), $"Source folder for node files ({assemblyDirectory}) doesn't exist");
            pathLocation = assemblyDirectory.LastIndexOf("\\TypeScriptAnalyzerNode") + 1;
            ProcessDirectory(assemblyDirectory);
            File.WriteAllText(assemblyDirectory + "\\temp.txt", result);
        }

        private void ProcessDirectory(string targetDirectory)
        {
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName);
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        private readonly string template = @"    <Content Include=""REPLACE"">
      <IncludeInVSIX>true</IncludeInVSIX>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
";
        private void ProcessFile(string path)
        {
            string pathForCsproj = path.Substring(pathLocation, path.Length - pathLocation);
            string entry = template.Replace("REPLACE", pathForCsproj);
            result += entry;
        }
    }
}
