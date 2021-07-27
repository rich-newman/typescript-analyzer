using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using WebLinterVsix;
namespace WebLinterTest
{
    public class ProjectsTestBase
    {
        protected static EnvDTE80.DTE2 dte;
        protected static Solution solution;
        protected MockSettings settings;
        protected UIHierarchyItem[] selectedItems;
        protected MockErrorListDataSource mockErrorListDataSource;

        protected void Initialize(string solutionPath)
        {
            MessageFilter.Register();
            Type type = Type.GetTypeFromProgID("VisualStudio.DTE.15.0");
            object inst = Activator.CreateInstance(type, true);
            dte = (EnvDTE80.DTE2)inst;
            var test = (DTE)inst;
            //test.
            dte.Solution.Open(Path.GetFullPath(solutionPath));

            solution = dte.Solution;
            settings = new MockSettings() { UseTsConfig = false };
            WebLinterPackage.Settings = settings;
            WebLinterPackage.Dte = dte;
        }

        protected UIHierarchyItem[] Arrange(string solutionPath)
        {
            Initialize(solutionPath);
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = solution };
            selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };
            mockErrorListDataSource = new MockErrorListDataSource();
            ErrorListDataSource.InjectMockErrorListDataSource(mockErrorListDataSource);
            return selectedItems;
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            if (solution != null) { solution.Close(); solution = null; }
            if (dte != null) dte.Quit();
            WebLinterPackage.Settings = null;
            WebLinterPackage.Dte = null;
            MessageFilter.Revoke();
        }
    }

    [TestClass]
    public class EmptyUnloadedProjectsTest : ProjectsTestBase
    {
        [TestMethod, TestCategory("Empty/Unloaded Projects")]
        public async Task LintEmptySolution()
        {
            try
            {
                Arrange(@"../../artifacts/empty/noprojects.sln");

                bool hasVSErrors = await LintFilesCommandBase.LintSelectedItems(false, selectedItems);

                Assert.IsFalse(hasVSErrors);
                Assert.IsFalse(mockErrorListDataSource.HasErrors());
                Assert.AreEqual(0, mockErrorListDataSource.Snapshots.Count);
            }
            finally
            {
                ErrorListDataSource.InjectMockErrorListDataSource(null);
            }
        }

    }
}
