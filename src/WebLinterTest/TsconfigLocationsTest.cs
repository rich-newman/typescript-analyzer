using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

            settings = new MockSettings() { UseTsConfig = true };
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

        [TestMethod, TestCategory("tsconfig Locations")]
        public void BasicEnvironmentTest()
        {

        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindForSingleItem()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(expected, result.FullName);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindForSingleItemSubfolder()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            Assert.AreEqual(expected, result.FullName);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindForSingleItemRoot()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(expected, result.FullName);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindForSingleItemNotsconfig()
        {
            // Note there's a tsconfig.json in the folder, but it's not in the project: it shouldn't be picked up
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file2.ts");
            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName);
            Assert.IsNull(result);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInProject()
        {
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project project = FindProject(projectFullName, solution);
            Tsconfig[] results = TsconfigLocations.FindInProject(project).ToArray();
            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(Contains(results, expected1));
            Assert.IsTrue(Contains(results, expected2));
            Assert.IsTrue(Contains(results, expected3));
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInProjectNotsconfig()
        {
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/tsconfigEmptyTest.csproj");
            Project project = FindProject(projectFullName, solution);
            Tsconfig[] results = TsconfigLocations.FindInProject(project).ToArray();
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Length);
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
            Tsconfig[] results = TsconfigLocations.FindFromSelectedItems(selectedItems).ToArray();

            // Assert
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(1, results.Length);
            Assert.IsTrue(Contains(results, expected));
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

            Tsconfig[] results = TsconfigLocations.FindFromSelectedItems(selectedItems).ToArray();

            string expected = mainProjectTsconfigFullName;
            Assert.AreEqual(1, results.Length);
            Assert.IsTrue(Contains(results, expected));
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

            Tsconfig[] results = TsconfigLocations.FindFromSelectedItems(selectedItems).ToArray();

            Assert.AreEqual(0, results.Length);
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSelectedItemsSolution()
        {
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = solution };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            Tsconfig[] results = TsconfigLocations.FindFromSelectedItems(selectedItems).ToArray();

            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(Contains(results, expected1));
            Assert.IsTrue(Contains(results, expected2));
            Assert.IsTrue(Contains(results, expected3));

        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSelectedItemsProject()
        {
            string mainProjectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project mainProject = FindProject(mainProjectFullName, solution);

            MockUIHierarchyItem mockProjectHierarchyItem = new MockUIHierarchyItem() { Object = mainProject };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockProjectHierarchyItem };

            Tsconfig[] results = TsconfigLocations.FindFromSelectedItems(selectedItems).ToArray();

            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(Contains(results, expected1));
            Assert.IsTrue(Contains(results, expected2));
            Assert.IsTrue(Contains(results, expected3));

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

            Tsconfig[] results = TsconfigLocations.FindFromSelectedItems(selectedItems).ToArray();

            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(1, results.Length);
            Assert.IsTrue(Contains(results, expected));
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSolution()
        {
            Tsconfig[] results = TsconfigLocations.FindInSolution(solution).ToArray();
            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(Contains(results, expected1));
            Assert.IsTrue(Contains(results, expected2));
            Assert.IsTrue(Contains(results, expected3));
        }

        [TestMethod, TestCategory("tsconfig Locations")]
        public void FindInSolutionIgnorePath()
        {
            settings.IgnorePatterns = new string[] { @"\multiple\a\" };
            try
            {
                Tsconfig[] results = TsconfigLocations.FindInSolution(solution).ToArray();
                string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
                string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
                string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
                Assert.AreEqual(2, results.Length);
                Assert.IsTrue(Contains(results, expected1));
                Assert.IsFalse(Contains(results, expected2));  // IsFalse test: the file has been excluded
                Assert.IsTrue(Contains(results, expected3));
            }
            finally
            {
                settings.IgnorePatterns = new string[0];
            }
        }

        public static Project FindProject(string projectFullName, Solution solution)
        {
            var test = solution.Projects.GetEnumerator();
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

        public bool Contains(Tsconfig[] ary, string value)
        {
            foreach (Tsconfig item in ary)
            {
                if (item.FullName == value) return true;
            }
            return false;
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
