using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using WebLinter;
using WebLinterVsix.Helpers;
using System.Collections.Generic;
using EnvDTE;

namespace WebLinterTest
{
    [TestClass]
    public class TsconfigTest
    {
        private static EnvDTE80.DTE2 dte = null;
        private static EnvDTE.Solution solution = null;

        // Clunky, but better than nothing
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Type type = System.Type.GetTypeFromProgID("VisualStudio.DTE.15.0");
            object inst = System.Activator.CreateInstance(type, true);
            dte = (EnvDTE80.DTE2)inst;
            dte.Solution.Open(Path.GetFullPath(@"../../artifacts/tsconfig/Tsconfig.sln"));
            solution = dte.Solution;

            Settings settings = new Settings() { TSLintUseTSConfig = true };
            WebLinterVsix.WebLinterPackage.Settings = settings;

            MessageFilter.Register();
            //System.Threading.Thread.Sleep(1000);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (solution != null) { solution.Close(); solution = null; }
            if (dte != null) dte.Quit();
            WebLinterVsix.WebLinterPackage.Settings = null;
            MessageFilter.Revoke();
        }

        [TestMethod, TestCategory("tsconfig")]
        public void BasicEnvironmentTest()
        {

        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindForSingleItem()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName, solution);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(expected, result.FullName);
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindForSingleItemSubfolder()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName, solution);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            Assert.AreEqual(expected, result.FullName);
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindForSingleItemRoot()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName, solution);
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(expected, result.FullName);
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindForSingleItemNotsconfig()
        {
            // Note there's a tsconfig.json in the folder, but it's not in the project: it shouldn't be picked up
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file2.ts");
            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName, solution);
            Assert.IsNull(result);
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindInProject()
        {
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project project = FindProject(projectFullName, solution);
            Tsconfig[] results = TsconfigLocations.FindInProject(project, solution).ToArray();
            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(Contains(results, expected1));
            Assert.IsTrue(Contains(results, expected2));
            Assert.IsTrue(Contains(results, expected3));
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindInProjectNotsconfig()
        {
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/tsconfigEmptyTest.csproj");
            Project project = FindProject(projectFullName, solution);
            Tsconfig[] results = TsconfigLocations.FindInProject(project, solution).ToArray();
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Length);
        }

        [TestMethod, TestCategory("tsconfig")]
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
            Tsconfig[] results = TsconfigLocations.FindFromSelectedItems(selectedItems, solution).ToArray();

            // Assert
            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(1, results.Length);
            Assert.IsTrue(Contains(results, expected));
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindInSelectedItemsNoTsconfig()
        {
            string emptyProjectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/tsconfigEmptyTest.csproj");
            Project emptyProject = FindProject(emptyProjectFullName, solution);
            string emptyFile2FullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file2.ts");
            ProjectItem file2 = FindProjectItemInProject(emptyFile2FullName, emptyProject);
            MockUIHierarchyItem mockFile2HierarchyItem = new MockUIHierarchyItem() { Object = file2 };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockFile2HierarchyItem };

            Tsconfig[] results = TsconfigLocations.FindFromSelectedItems(selectedItems, solution).ToArray();

            Assert.AreEqual(0, results.Length);
        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindInSelectedItemsSolution()
        {
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = solution };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            Tsconfig[] results = TsconfigLocations.FindFromSelectedItems(selectedItems, solution).ToArray();

            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(Contains(results, expected1));
            Assert.IsTrue(Contains(results, expected2));
            Assert.IsTrue(Contains(results, expected3));

        }

        [TestMethod, TestCategory("tsconfig")]
        public void FindInSelectedItemsProject()
        {
            string mainProjectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project mainProject = FindProject(mainProjectFullName, solution);

            MockUIHierarchyItem mockProjectHierarchyItem = new MockUIHierarchyItem() { Object = mainProject };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockProjectHierarchyItem };

            Tsconfig[] results = TsconfigLocations.FindFromSelectedItems(selectedItems, solution).ToArray();

            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(Contains(results, expected1));
            Assert.IsTrue(Contains(results, expected2));
            Assert.IsTrue(Contains(results, expected3));

        }

        [TestMethod, TestCategory("tsconfig")]
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

            Tsconfig[] results = TsconfigLocations.FindFromSelectedItems(selectedItems, solution).ToArray();

            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            Assert.AreEqual(1, results.Length);
            Assert.IsTrue(Contains(results, expected));
        }

        [TestMethod, TestCategory("tsconfig")]
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

        private static Project FindProject(string projectFullName, Solution solution)
        {
            var test = solution.Projects.GetEnumerator();
            foreach (Project project in solution.Projects)
                if (project.FullName == projectFullName) return project;
            return null;
        }

        private static ProjectItem FindProjectItemInProject(string projectItemName, Project project)
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
            string fileName = rootProjectItem.Properties.Item("FullPath")?.Value?.ToString();
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

    public class MockUIHierarchyItem: UIHierarchyItem
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
