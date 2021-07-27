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
    /// Tests we construct the correct Solution Explorer items for linting when we are building
    /// </summary>
    /// <remarks>
    /// The rules are slightly different from regular file discovery: if a single item or items is
    /// selected in Solution Explorer we need to figure out which VS projects are being built
    /// </remarks>
    [TestClass]
    public class BuildSelectedItemsTest
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
            dte.Solution.Open(Path.GetFullPath(@"../../artifacts/localinstall/multiple/multiple.sln"));
            solution = dte.Solution;

            settings = new MockSettings() { UseTsConfig = false };
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

        [TestMethod, TestCategory("Build Selected Items")]
        public void SelectedItemsWhenBuidingSolutionTest()
        {
            UIHierarchyItem[] results = BuildSelectedItems.Get(isBuildingSolution: true);
            Assert.AreEqual(1, results.Length);
            Solution solutionObject = results[0].Object as Solution;
            Assert.IsNotNull(solutionObject);
            Assert.IsTrue(solutionObject.FullName
                .EndsWith("\\src\\WebLinterTest\\artifacts\\localinstall\\multiple\\multiple.sln"));
        }

        [TestMethod, TestCategory("Build Selected Items")]
        public void MapToProjectsSingleProjectItemTest()
        {
            // Simulate fileA.ts being selected in Solution Explorer in multiple.sln.
            // Ensure a.csproj (which contains fileA.ts) is the item we calculate as building
            string fileAFullPath = Path.GetFullPath(@"../../artifacts/localinstall/multiple/a/fileA.ts");
            Solution solution = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.UIHierarchyItems.Item(1).Object as Solution;
            ProjectItem projectItem = solution.FindProjectItem(fileAFullPath);
            MockUIHierarchyItem mockProjectItemHierarchyItem = new MockUIHierarchyItem { Object = projectItem };

            UIHierarchyItem[] results = BuildSelectedItems.MapToProjects(new UIHierarchyItem[] { mockProjectItemHierarchyItem }).ToArray();

            Assert.AreEqual(1, results.Length);
            Project projectObject = results[0].Object as Project;
            Assert.IsNotNull(projectObject);
            Assert.IsTrue(projectObject.FullName
                .EndsWith("\\src\\WebLinterTest\\artifacts\\localinstall\\multiple\\a\\a.csproj"));
        }

        [TestMethod, TestCategory("Build Selected Items")]
        public void MapToProjectsTwoProjectItemsInSameProjectTest()
        {
            // Simulate fileA.ts and fileAA.ts being selected in Solution Explorer in multiple.sln.
            string fileAFullPath = Path.GetFullPath(@"../../artifacts/localinstall/multiple/a/fileA.ts");
            string fileAAFullPath = Path.GetFullPath(@"../../artifacts/localinstall/multiple/a/fileAA.ts");
            Solution solution = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.UIHierarchyItems.Item(1).Object as Solution;
            ProjectItem projectItemA = solution.FindProjectItem(fileAFullPath);
            ProjectItem projectItemAA = solution.FindProjectItem(fileAAFullPath);
            MockUIHierarchyItem mockProjectItemHierarchyItemA = new MockUIHierarchyItem { Object = projectItemA };
            MockUIHierarchyItem mockProjectItemHierarchyItemAA = new MockUIHierarchyItem { Object = projectItemAA };

            UIHierarchyItem[] results = BuildSelectedItems.MapToProjects(
                new UIHierarchyItem[] { mockProjectItemHierarchyItemA, mockProjectItemHierarchyItemAA }).ToArray();

            Assert.AreEqual(1, results.Length);
            Project projectObject = results[0].Object as Project;
            Assert.IsNotNull(projectObject);
            Assert.IsTrue(projectObject.FullName
                .EndsWith("\\src\\WebLinterTest\\artifacts\\localinstall\\multiple\\a\\a.csproj"));
        }

        [TestMethod, TestCategory("Build Selected Items")]
        public void MapToProjectsTwoProjectItemsInDifferentProjectsTest()
        {
            // Simulate fileA.ts and fileB.ts being selected in Solution Explorer in multiple.sln.
            string fileAFullPath = Path.GetFullPath(@"../../artifacts/localinstall/multiple/a/fileA.ts");
            string fileBFullPath = Path.GetFullPath(@"../../artifacts/localinstall/multiple/b/fileB.ts");
            Solution solution = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.UIHierarchyItems.Item(1).Object as Solution;
            ProjectItem projectItemA = solution.FindProjectItem(fileAFullPath);
            ProjectItem projectItemB = solution.FindProjectItem(fileBFullPath);
            MockUIHierarchyItem mockProjectItemHierarchyItemA = new MockUIHierarchyItem { Object = projectItemA };
            MockUIHierarchyItem mockProjectItemHierarchyItemB = new MockUIHierarchyItem { Object = projectItemB };

            UIHierarchyItem[] results = BuildSelectedItems.MapToProjects(
                new UIHierarchyItem[] { mockProjectItemHierarchyItemA, mockProjectItemHierarchyItemB }).ToArray();

            Assert.AreEqual(2, results.Length);
            Project projectObject = results[0].Object as Project;
            Assert.IsNotNull(projectObject);
            Assert.IsTrue(projectObject.FullName
                .EndsWith("\\src\\WebLinterTest\\artifacts\\localinstall\\multiple\\a\\a.csproj"));
            Project projectObject2 = results[1].Object as Project;
            Assert.IsNotNull(projectObject2);
            Assert.IsTrue(projectObject2.FullName
                .EndsWith("\\src\\WebLinterTest\\artifacts\\localinstall\\multiple\\b\\b.csproj"));
        }


        [TestMethod, TestCategory("Build Selected Items")]
        public void MapToProjectsItemNotInProjectTest()
        {
            // package.json in b isn't in a project or solution
            string fileFullPath = Path.GetFullPath(@"../../artifacts/localinstall/multiple/b/package.json");
            Assert.IsTrue(File.Exists(fileFullPath));
            Solution solution = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.UIHierarchyItems.Item(1).Object as Solution;
            ProjectItem projectItem = solution.FindProjectItem(fileFullPath);
            MockUIHierarchyItem mockProjectItemHierarchyItem = new MockUIHierarchyItem { Object = projectItem };

            UIHierarchyItem[] results = BuildSelectedItems.MapToProjects(new UIHierarchyItem[] { mockProjectItemHierarchyItem }).ToArray();

            Assert.AreEqual(0, results.Length);
        }
    }
}
