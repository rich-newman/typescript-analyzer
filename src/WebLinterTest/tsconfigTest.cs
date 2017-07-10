using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using WebLinter.Helpers;

namespace WebLinterTest
{
    public class TsconfigTest
    {
        [TestMethod, TestCategory("tsconfig")]
        public void FindInProject()
        {
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Tsconfig[] results = Tsconfig.FindInProject(projectFullName);
            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(Contains(results, expected1));
            Assert.IsTrue(Contains(results, expected2));
            Assert.IsTrue(Contains(results, expected3));
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindForSingleItem()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            Tsconfig result = Tsconfig.FindFromProjectItem(projectItemFullName);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(expected, result);
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindForSingleItemSubfolder()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
            Tsconfig result = Tsconfig.FindFromProjectItem(projectItemFullName);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            Assert.AreEqual(expected, result);
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindForSingleItemRoot()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
            Tsconfig result = Tsconfig.FindFromProjectItem(projectItemFullName);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(expected, result);
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindForSingleItemNotsconfig()
        {
            // Note there's a tsconfig.json in the folder, but it's not in the project: it shouldn't be picked up
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file2.ts");
            Tsconfig result = Tsconfig.FindFromProjectItem(projectItemFullName);
            Assert.IsNull(result);
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindInProjectNotsconfig()
        {
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/tsconfigEmptyTest.csproj");
            Tsconfig[] results = Tsconfig.FindInProject(projectFullName);
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Length);
        }

        public bool Contains(Tsconfig[] ary, string value)
        {
            foreach (Tsconfig item in ary)
            {
                if (item.FullName == value) return true;
            }
            return false;
        }
    }
}
