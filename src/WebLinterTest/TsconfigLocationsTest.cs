using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebLinterVsix;
using WebLinterVsix.Helpers;

namespace WebLinterTest
{
    /// <summary>
    /// Tests how files are found for linting when the use tsconfig.json option is set to true
    /// Tests file discovery both from file paths and from selected items in Solution Explorer
    /// </summary>
    [TestClass]
    public class TsconfigLocationsTest
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

            settings = new MockSettings() { UseTsConfig = true, IgnoreNestedFiles = false };
        }

        [TestInitialize]
        public void TestInitialize()
        {
            WebLinterVsix.WebLinterPackage.Settings = settings;
            WebLinterVsix.WebLinterPackage.Dte = dte;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            WebLinterVsix.WebLinterPackage.Settings = null;
            WebLinterVsix.WebLinterPackage.Dte = null;
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

        [TestMethod, TestCategory("tsconfig Locations")]
        public void BasicEnvironmentTest()
        {

        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindForSingleItem()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            string result = TsconfigLocations.FindParentTsconfig(projectItemFullName);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(expected, result);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindForSingleItemSubfolder()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
            string result = TsconfigLocations.FindParentTsconfig(projectItemFullName);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            Assert.AreEqual(expected, result);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindForSingleItemRoot()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
            string result = TsconfigLocations.FindParentTsconfig(projectItemFullName);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(expected, result);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindForSingleItemNotsconfig()
        {
            // Note there's a tsconfig.json in the folder, but it's not in the project: it shouldn't be picked up
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file2.ts");
            string result = TsconfigLocations.FindParentTsconfig(projectItemFullName);
            Assert.IsNull(result);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInProject()
        {
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project project = FindProject(projectFullName, solution);
            HashSet<string> results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> fileToProjectMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            TsconfigLocations.FindTsconfigsInProject(project, results, fileToProjectMap);
            string expectedConfig1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expectedConfig2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expectedConfig3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.Contains(expectedConfig1));
            Assert.IsTrue(results.Contains(expectedConfig2));
            Assert.IsTrue(results.Contains(expectedConfig3));

            // The fileToProjectMap contains all 9 files in tsconfigTest.csproj
            Assert.AreEqual(9, fileToProjectMap.Keys.Count);
            TestMapContainsAlltsconfigTestFiles(fileToProjectMap);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInProjectNotsconfig()
        {
            // Folder b contains a tsconfig on disk that's not included in the VS project
            // Folder c contains a tsconfig in the VS project (tsconfigEmptyTest.csproj) that doesn't exist on disk
            // In both cases we don't lint with these
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/tsconfigEmptyTest.csproj");
            Project project = FindProject(projectFullName, solution);
            HashSet<string> results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> fileToProjectMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            TsconfigLocations.FindTsconfigsInProject(project, results, fileToProjectMap);
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
            // We iterate the project looking for tsconfigs, and create a map of all files -> project names as we go
            // So the fileToProjectMap correctly contains the mapping for file5.ts, which is in tsconfigEmptyTest, even though we find
            // no tsconfigs
            Assert.AreEqual(1, fileToProjectMap.Keys.Count);
            Assert.AreEqual("tsconfigEmptyTest", fileToProjectMap[Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts")]);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSelectedItemsSingleFile()
        {
            // Arrange
            string mainProjectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project mainProject = FindProject(mainProjectFullName, solution);
            string mainFile4FullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
            ProjectItem file4 = FindProjectItemInProject(mainFile4FullName, mainProject);
            MockUIHierarchyItem mockFile4HierarchyItem = new MockUIHierarchyItem() { Object = file4 };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockFile4HierarchyItem };

            // Act
            string[] results = TsconfigLocations.FindPathsFromSelectedItems(selectedItems, out Dictionary<string, string> fileToProjectMap);

            // Assert
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(1, results.Length);
            Assert.IsTrue(results.Contains(expected));
            Assert.AreEqual(1, fileToProjectMap.Keys.Count);
            Assert.AreEqual("tsconfigTest", fileToProjectMap[mainFile4FullName]);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSelectedItemsTsconfig()
        {
            string mainProjectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project mainProject = FindProject(mainProjectFullName, solution);
            string mainProjectTsconfigFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            ProjectItem tsconfig = FindProjectItemInProject(mainProjectTsconfigFullName, mainProject);
            MockUIHierarchyItem mockTsconfigHierarchyItem = new MockUIHierarchyItem() { Object = tsconfig };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockTsconfigHierarchyItem };

            string[] results = TsconfigLocations.FindPathsFromSelectedItems(selectedItems, out Dictionary<string, string> fileToProjectMap);

            string expected = mainProjectTsconfigFullName;
            Assert.AreEqual(1, results.Length);
            Assert.IsTrue(results.Contains(expected));
            // If we lint having selected a tsconfig we don't search the project structure pre-lint, so can't construct the map
            Assert.AreEqual(0, fileToProjectMap.Keys.Count);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSelectedItemsNoTsconfig()
        {
            string emptyProjectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/tsconfigEmptyTest.csproj");
            Project emptyProject = FindProject(emptyProjectFullName, solution);
            string emptyFile2FullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file2.ts");
            ProjectItem file2 = FindProjectItemInProject(emptyFile2FullName, emptyProject);
            MockUIHierarchyItem mockFile2HierarchyItem = new MockUIHierarchyItem() { Object = file2 };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockFile2HierarchyItem };

            Dictionary<string, string> fileToProjectMap;
            string[] results = TsconfigLocations.FindPathsFromSelectedItems(selectedItems, out fileToProjectMap);

            Assert.AreEqual(0, results.Length);
            Assert.AreEqual(0, fileToProjectMap.Keys.Count);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSelectedItemsSolution()
        {
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = solution };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            string[] results = TsconfigLocations.FindPathsFromSelectedItems(selectedItems, out Dictionary<string, string> fileToProjectMap);

            string expectedConfig1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expectedConfig2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expectedConfig3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(results.Contains(expectedConfig1));
            Assert.IsTrue(results.Contains(expectedConfig2));
            Assert.IsTrue(results.Contains(expectedConfig3));

            TestMapContainsAlltsconfigTestAndtsconfigEmptyTestFiles(fileToProjectMap);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSelectedItemsProject()
        {
            string mainProjectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project mainProject = FindProject(mainProjectFullName, solution);

            MockUIHierarchyItem mockProjectHierarchyItem = new MockUIHierarchyItem() { Object = mainProject };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockProjectHierarchyItem };

            string[] results = TsconfigLocations.FindPathsFromSelectedItems(selectedItems, out Dictionary<string, string> fileToProjectMap);

            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(results.Contains(expected1));
            Assert.IsTrue(results.Contains(expected2));
            Assert.IsTrue(results.Contains(expected3));
            Assert.AreEqual(9, fileToProjectMap.Keys.Count);
            TestMapContainsAlltsconfigTestFiles(fileToProjectMap);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSelectedItemsMultipleFiles()
        {
            // Includes two files with the same tsconfig.json and one with none
            string mainProjectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project mainProject = FindProject(mainProjectFullName, solution);
            string emptyProjectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/tsconfigEmptyTest.csproj");
            Project emptyProject = FindProject(emptyProjectFullName, solution);

            string mainFile4FullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
            ProjectItem file4 = FindProjectItemInProject(mainFile4FullName, mainProject);
            MockUIHierarchyItem mockFile4HierarchyItem = new MockUIHierarchyItem() { Object = file4 };

            string emptyFile2FullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file2.ts");
            ProjectItem file2 = FindProjectItemInProject(emptyFile2FullName, emptyProject);
            MockUIHierarchyItem mockFile2HierarchyItem = new MockUIHierarchyItem() { Object = file2 };

            string mainFile1FullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            ProjectItem file1 = FindProjectItemInProject(mainFile1FullName, mainProject);
            MockUIHierarchyItem mockFile1HierarchyItem = new MockUIHierarchyItem() { Object = file1 };


            UIHierarchyItem[] selectedItems = new UIHierarchyItem[]
                                              {
                                                  mockFile1HierarchyItem, mockFile2HierarchyItem, mockFile4HierarchyItem
                                              };

            string[] results = TsconfigLocations.FindPathsFromSelectedItems(selectedItems, out Dictionary<string, string> fileToProjectMap);

            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(1, results.Length);
            Assert.IsTrue(results.Contains(expected));
            Assert.AreEqual(2, fileToProjectMap.Keys.Count);
            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
            Assert.AreEqual("tsconfigTest", fileToProjectMap[expected1]);
            Assert.AreEqual("tsconfigTest", fileToProjectMap[expected2]);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSolution()
        {
            HashSet<string> results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> fileToProjectMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            TsconfigLocations.FindTsconfigsInSolution(solution, results, fileToProjectMap);
            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.Contains(expected1));
            Assert.IsTrue(results.Contains(expected2));
            Assert.IsTrue(results.Contains(expected3));
            TestMapContainsAlltsconfigTestAndtsconfigEmptyTestFiles(fileToProjectMap);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSolutionIgnorePath()
        {
            settings.IgnorePatterns = new string[] { @"\multiple\a\" };
            try
            {
                HashSet<string> results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> fileToProjectMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                TsconfigLocations.FindTsconfigsInSolution(solution, results, fileToProjectMap);
                string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
                string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
                string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
                Assert.AreEqual(2, results.Count);
                Assert.IsTrue(results.Contains(expected1));
                Assert.IsFalse(results.Contains(expected2));  // IsFalse test: the file has been excluded
                Assert.IsTrue(results.Contains(expected3));
            }
            finally
            {
                settings.IgnorePatterns = new string[0];
            }
        }

        //[TestMethod, TestCategory("tsconfig Locations")]
        //public void FindInSolutionExcludeNested()
        //{
        //    settings.IgnoreNestedFiles = true;
        //    try
        //    {
        //        HashSet<string> results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //        Dictionary<string, string> fileToProjectMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        //        TsconfigLocations.FindTsconfigsInSolution(solution, results, fileToProjectMap);
        //        string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
        //        string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
        //        string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
        //        // Nested tsconfig in tsconfigEmptyTest
        //        string expected4 = Path.GetFullPath(@"../../artifacts/tsconfig/none/tsconfig.json");
        //        // C:\Source\typescript-analyzer2\src\WebLinterTest\artifacts\tsconfig\none\tsconfig.json
        //        Assert.AreEqual(4, results.Length);
        //        Assert.IsTrue(Contains(results, expected1));
        //        Assert.IsTrue(Contains(results, expected2));
        //        Assert.IsTrue(Contains(results, expected3));
        //        Assert.IsTrue(Contains(results, expected4));
        //    }
        //    finally
        //    {
        //        settings.IgnoreNestedFiles = false;
        //    }

        //}

        private static void TestMapContainsAlltsconfigTestAndtsconfigEmptyTestFiles(Dictionary<string, string> fileToProjectMap)
        {
            Assert.AreEqual(10, fileToProjectMap.Keys.Count); // 9 in testconfigTest, 1 in tsconfigEmptyTest
            TestMapContainsAlltsconfigTestFiles(fileToProjectMap);
            string expected10 = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts"); // In tsconfigEmptyTest, not tsconfigTest
            Assert.AreEqual("tsconfigEmptyTest", fileToProjectMap[expected10]);
        }

        private static void TestMapContainsAlltsconfigTestFiles(Dictionary<string, string> fileToProjectMap)
        {
            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react-dom.d.ts");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react.d.ts");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
            string expected4 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            string expected5 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
            string expected6 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/file3.ts");
            string expected7 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file6.tsx");
            string expected8 = Path.GetFullPath(@"../../artifacts/tsconfig/file9.ts"); // Linked file in Tsconfig.sln/tsconfigTest.csproj
            string expected9 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/file7.ts"); // Nested file under HtmlPage1.html
            //string expected10 = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts"); // In tsconfigEmptyTest, not tsconfigTest

            Assert.AreEqual("tsconfigTest", fileToProjectMap[expected1]);
            Assert.AreEqual("tsconfigTest", fileToProjectMap[expected2]);
            Assert.AreEqual("tsconfigTest", fileToProjectMap[expected3]);
            Assert.AreEqual("tsconfigTest", fileToProjectMap[expected4]);
            Assert.AreEqual("tsconfigTest", fileToProjectMap[expected5]);
            Assert.AreEqual("tsconfigTest", fileToProjectMap[expected6]);
            Assert.AreEqual("tsconfigTest", fileToProjectMap[expected7]);
            Assert.AreEqual("tsconfigTest", fileToProjectMap[expected8]);
            Assert.AreEqual("tsconfigTest", fileToProjectMap[expected9]);
        }

        public static Project FindProject(string projectFullName, Solution solution)
        {
            foreach (Project project in solution.Projects)
                if (project.FullName == projectFullName) return project;
            return null;
        }

        public static ProjectItem FindProjectItemInProject(string projectItemName, Project project)
        {
            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                ProjectItem result = FindProjectItemInProjectItem(projectItemName, projectItem);
                if (result != null) return result;
            }
            return null;
        }

        private static ProjectItem FindProjectItemInProjectItem(string projectItemName, ProjectItem rootProjectItem)
        {
            string fileName = rootProjectItem.GetFullPath();
            if (fileName == projectItemName) return rootProjectItem;
            if (rootProjectItem == null || rootProjectItem.ProjectItems == null) return null;
            foreach (ProjectItem subProjectItem in rootProjectItem.ProjectItems)
            {
                ProjectItem result = FindProjectItemInProjectItem(projectItemName, subProjectItem);
                if (result != null) return result;
            }
            return null;
        }
    }

    public class MockUIHierarchyItem : UIHierarchyItem
    {
        public void Select(vsUISelectionType How)
        {
            throw new NotImplementedException();
        }

        public DTE DTE => throw new NotImplementedException();

        public UIHierarchyItems Collection => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public UIHierarchyItems UIHierarchyItems => throw new NotImplementedException();

        public object Object { get; set; }

        public bool IsSelected => throw new NotImplementedException();
    }
}
