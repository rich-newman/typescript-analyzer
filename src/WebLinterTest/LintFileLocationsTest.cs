using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebLinterVsix.Helpers;

namespace WebLinterTest
{
    /// <summary>
    /// Tests how files are found for linting from various selected items in Solution Explorer
    /// This is the normal case of linting all files in a VS project: use tsconfig.json option is set to false
    /// </summary>
    [TestClass]
    public class LintFileLocationsTest
    {
        private static EnvDTE80.DTE2 dte = null;
        private static EnvDTE.Solution solution = null;
        private static MockSettings settings = null;

        // Clunky, but better than nothing
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            MessageFilter.Register();
            Type type = System.Type.GetTypeFromProgID("VisualStudio.DTE.15.0");
            object inst = System.Activator.CreateInstance(type, true);
            dte = (EnvDTE80.DTE2)inst;
            dte.Solution.Open(Path.GetFullPath(@"../../artifacts/tsconfig/Tsconfig.sln"));
            solution = dte.Solution;

            settings = new MockSettings() { UseTsConfig = false };
            WebLinterVsix.WebLinterPackage.Settings = settings;
            WebLinterVsix.WebLinterPackage.Dte = dte;
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (solution != null) { solution.Close(); solution = null; }
            if (dte != null) dte.Quit();
            WebLinterVsix.WebLinterPackage.Settings = null;
            WebLinterVsix.WebLinterPackage.Dte = null;
            MessageFilter.Revoke();
        }

        [TestMethod, TestCategory("Lint File Locations")]
        public void GetLintFilesForSolution()
        {
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = solution };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };
            List<string> results = LintFileLocations.FindPathsFromSelectedItems(selectedItems).ToList();

            Assert.AreEqual(8, results.Count);

            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react-dom.d.ts");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react.d.ts");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
            string expected4 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            string expected5 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
            string expected6 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/file3.ts");
            string expected7 = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts");
            string expected8 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file6.tsx");

            Assert.IsTrue(results.Contains(expected1));
            Assert.IsTrue(results.Contains(expected2));
            Assert.IsTrue(results.Contains(expected3));
            Assert.IsTrue(results.Contains(expected4));
            Assert.IsTrue(results.Contains(expected5));
            Assert.IsTrue(results.Contains(expected6));
            Assert.IsTrue(results.Contains(expected7));
            Assert.IsTrue(results.Contains(expected8));
        }

        [TestMethod, TestCategory("Lint File Locations")]
        public void GetLintFilesForProject()
        {
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/tsconfigEmptyTest.csproj");
            Project project = TsconfigLocationsTest.FindProject(projectFullName, solution);
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = project };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            List<string> results = LintFileLocations.FindPathsFromSelectedItems(selectedItems).ToList();

            Assert.AreEqual(1, results.Count);
            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts");
            Assert.IsTrue(results.Contains(expected1));
        }

        [TestMethod, TestCategory("Lint File Locations")]
        public void GetLintFilesForSingleItem()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project project = TsconfigLocationsTest.FindProject(projectFullName, solution);
            ProjectItem projectItem = TsconfigLocationsTest.FindProjectItemInProject(projectItemFullName, project);

            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = projectItem };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            List<string> results = LintFileLocations.FindPathsFromSelectedItems(selectedItems).ToList();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(projectItemFullName, results[0]);
            //string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts");
            //Assert.IsTrue(results.Contains(expected1));
        }

        [TestMethod, TestCategory("Lint File Locations")]
        public void GetLintFilesForSolutionIncludeNested()
        {
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = solution };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            settings.IgnoreNestedFiles = false;

            try
            {
                List<string> results = LintFileLocations.FindPathsFromSelectedItems(selectedItems).ToList();

                Assert.AreEqual(9, results.Count);

                string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react-dom.d.ts");
                string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react.d.ts");
                string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
                string expected4 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
                string expected5 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
                string expected6 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/file3.ts");
                string expected7 = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts");
                string expected8 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file6.tsx");
                string expected9 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/file7.ts");

                Assert.IsTrue(results.Contains(expected1));
                Assert.IsTrue(results.Contains(expected2));
                Assert.IsTrue(results.Contains(expected3));
                Assert.IsTrue(results.Contains(expected4));
                Assert.IsTrue(results.Contains(expected5));
                Assert.IsTrue(results.Contains(expected6));
                Assert.IsTrue(results.Contains(expected7));
                Assert.IsTrue(results.Contains(expected8));
                Assert.IsTrue(results.Contains(expected9));
            }
            finally
            {
                settings.IgnoreNestedFiles = true;
            }
        }

        [TestMethod, TestCategory("Lint File Locations")]
        public void GetLintFilesForSolutionSetIgnorePath()
        {
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = solution };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            settings.IgnorePatterns = new string[] { @"\multiple\a\", @"random\stuff" };

            try
            {
                List<string> results = LintFileLocations.FindPathsFromSelectedItems(selectedItems).ToList();

                Assert.AreEqual(5, results.Count);

                string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react-dom.d.ts");
                string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react.d.ts");
                string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
                //string expected4 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
                //string expected5 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
                string expected6 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/file3.ts");
                string expected7 = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts");
                //string expected8 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file6.tsx");

                Assert.IsTrue(results.Contains(expected1));
                Assert.IsTrue(results.Contains(expected2));
                Assert.IsTrue(results.Contains(expected3));
                //Assert.IsTrue(results.Contains(expected4));
                //Assert.IsTrue(results.Contains(expected5));
                Assert.IsTrue(results.Contains(expected6));
                Assert.IsTrue(results.Contains(expected7));
                //Assert.IsTrue(results.Contains(expected8));
            }
            finally
            {
                settings.IgnorePatterns = new string[0];
            }
        }
    }
}
