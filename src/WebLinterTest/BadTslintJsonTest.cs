using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLinterVsix;

namespace WebLinterTest
{

    [TestClass]
    public class BadTslintJsonTest
    {
        private static EnvDTE80.DTE2 dte = null;
        private static EnvDTE.Solution solution = null;
        private static MockSettings settings = null;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            // This is the official fix for the unused parameter warning IDE0060.  Nice.
            // https://github.com/dotnet/roslyn/issues/35063#issuecomment-484616262
            _ = testContext;
            MessageFilter.Register();
            Type type = Type.GetTypeFromProgID("VisualStudio.DTE.15.0");
            object inst = Activator.CreateInstance(type, true);
            dte = (EnvDTE80.DTE2)inst;
            dte.Solution.Open(Path.GetFullPath(@"../../artifacts/bad-tslint-json/bad-tslint-json.sln"));
            solution = dte.Solution;

            settings = new MockSettings();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            WebLinterPackage.Settings = settings;
            WebLinterPackage.Dte = dte;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            WebLinterPackage.Settings = null;
            WebLinterPackage.Dte = null;
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (solution != null) { solution.Close(); solution = null; }
            if (dte != null) dte.Quit();
            WebLinterPackage.Settings = null;
            WebLinterPackage.Dte = null;
            MessageFilter.Revoke();
        }

        [TestMethod, TestCategory("Lint End to End")]
        public async Task LintBadTslintJsonSolution()
        {
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = solution };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            MockErrorListDataSource mockErrorListDataSource = new MockErrorListDataSource();
            ErrorListDataSource.InjectMockErrorListDataSource(mockErrorListDataSource);

            settings.UseTsConfig = false;
            settings.IgnoreNestedFiles = false;

            try
            {
                bool hasVSErrors = await LintFilesCommandBase.LintLintLint(false, selectedItems);

                Assert.IsTrue(hasVSErrors);
                Assert.IsTrue(mockErrorListDataSource.HasErrors());
                Assert.AreEqual(1, mockErrorListDataSource.Snapshots.Count);

                CollectionAssert.AreEquivalent(new string[] { "TSLint" }, mockErrorListDataSource.Snapshots.Keys.ToArray());

                var actualMsg = mockErrorListDataSource.Snapshots["TSLint"].Select(e => e.Message).First();
                var expectedMsg = "Could not find custom rule directory: ./does-not-exist";
                StringAssert.Contains(actualMsg, expectedMsg);
            }
            finally
            {
                ErrorListDataSource.InjectMockErrorListDataSource(null);
                settings.UseTsConfig = false;
                settings.IgnoreNestedFiles = true;
            }

        }

    }
}
