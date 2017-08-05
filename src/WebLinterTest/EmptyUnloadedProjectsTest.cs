using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using WebLinterVsix;
using Microsoft.VisualStudio.Shell.Interop;
using System.Linq;
using Microsoft.VisualStudio;

namespace WebLinterTest
{
    public class EmptyUnloadedProjectsTestBase
    {
        protected EnvDTE80.DTE2 dte;
        protected Solution solution;
        protected MockSettings settings;
        protected UIHierarchyItem[] selectedItems;
        protected MockErrorsTableDataSource mockErrorsTableDataSource;

        protected void Initialize(string solutionPath)
        {
            MessageFilter.Register();
            Type type = Type.GetTypeFromProgID("VisualStudio.DTE.15.0");
            object inst = Activator.CreateInstance(type, true);
            dte = (EnvDTE80.DTE2)inst;
            var test = (EnvDTE.DTE)inst;
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
            mockErrorsTableDataSource = new MockErrorsTableDataSource();
            TableDataSource.InjectMockErrorsTableDataSource(mockErrorsTableDataSource);
            return selectedItems;
        }

        protected void Cleanup()
        {
            if (solution != null) { solution.Close(); solution = null; }
            if (dte != null) dte.Quit();
            WebLinterPackage.Settings = null;
            WebLinterPackage.Dte = null;
            MessageFilter.Revoke();
        }
    }

    [TestClass]
    public class EmptyUnloadedProjectsTest : EmptyUnloadedProjectsTestBase
    {
        [TestMethod, TestCategory("Empty/Unloaded Projects")]
        public async Task LintEmptySolution()
        {
            try
            {
                Arrange(@"../../artifacts/empty/noprojects.sln");

                bool hasVSErrors = await LintFilesCommandBase.LintLintLint(false, selectedItems);

                Assert.IsFalse(hasVSErrors);
                Assert.IsFalse(mockErrorsTableDataSource.HasErrors());
                Assert.AreEqual(0, mockErrorsTableDataSource.Snapshots.Count);
            }
            finally
            {
                TableDataSource.InjectMockErrorsTableDataSource(null);
                Cleanup();
            }
        }

    }
}
