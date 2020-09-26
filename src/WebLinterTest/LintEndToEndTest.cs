using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLinter;
using WebLinterVsix;
using WebLinterVsix.Helpers;

namespace WebLinterTest
{
    public class MockErrorsTableDataSource : IErrorsTableDataSource
    {
        private Dictionary<string, IGrouping<string, LintingError>> _snapshots = new Dictionary<string, IGrouping<string, LintingError>>();
        public Dictionary<string, IGrouping<string, LintingError>> Snapshots => _snapshots;

        public event EventHandler ErrorListChanged;

        public void AddErrors(IEnumerable<LintingError> errors)
        {
            if (errors == null || !errors.Any()) return;
            IEnumerable<LintingError> cleanErrors = errors.Where(e => e != null && !string.IsNullOrEmpty(e.FileName));
            foreach (IGrouping<string, LintingError> error in cleanErrors.GroupBy(t => t.FileName))
            {
                _snapshots[error.Key] = error;
            }
        }

        public void BringToFront() { }
        public void CleanAllErrors() => _snapshots.Clear();

        public void CleanErrors(IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                if (_snapshots.ContainsKey(file)) _snapshots.Remove(file);
            }
        }

        public bool HasErrors() => _snapshots.Count > 0;
        public bool HasErrors(string fileName) => _snapshots.ContainsKey(fileName);
        public void RaiseErrorListChanged() { }
    }

    /// <summary>
    /// Tests the whole process of linting end-to-end from selected items in Solution Explorer
    /// to the snapshots parsed into the error list, both with and without use tsconfig.json set
    /// </summary>
    [TestClass]
    public class LintEndToEndTest
    {
        private static EnvDTE80.DTE2 dte = null;
        private static EnvDTE.Solution solution = null;
        private static MockSettings settings = null;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            MessageFilter.Register();
            Type type = System.Type.GetTypeFromProgID("VisualStudio.DTE.15.0");
            object inst = System.Activator.CreateInstance(type, true);
            dte = (EnvDTE80.DTE2)inst;
            dte.Solution.Open(Path.GetFullPath(@"../../artifacts/tsconfig/Tsconfig.sln"));
            solution = dte.Solution;

            settings = new MockSettings();
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

        [TestMethod, TestCategory("Lint End to End")]
        public async Task LintSolution()
        {
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = solution };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            // TODO Don't like that singleton much, or my workaround to test: 
            // any reason it can't be instantiated at startup and cached on the package?
            MockErrorsTableDataSource mockErrorsTableDataSource = new MockErrorsTableDataSource();
            TableDataSource.InjectMockErrorsTableDataSource(mockErrorsTableDataSource);

            settings.UseTsConfig = false;
            settings.IgnoreNestedFiles = false;

            try
            {
                bool hasVSErrors = await LintFilesCommandBase.LintLintLint(false, selectedItems);

                Assert.IsFalse(hasVSErrors);
                Assert.IsTrue(mockErrorsTableDataSource.HasErrors());
                Assert.AreEqual(10, mockErrorsTableDataSource.Snapshots.Count);

                // See LintFileLocationsTest.GetLintFilesForSolutionIncludeNested
                string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react-dom.d.ts");
                string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react.d.ts");
                string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
                string expected4 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
                string expected5 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
                string expected6 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/file3.ts");
                string expected7 = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts");
                string expected8 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file6.tsx");
                string expected9 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/file7.ts");
                string expected10 = Path.GetFullPath(@"../../artifacts/tsconfig/file9.ts"); // Linked file

                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected1));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected2));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected3));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected4));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected5));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected6));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected7));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected8));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected9));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected10));

                // May be too painful when we upgrade tslint: I think since the tslint.json is fixed it should be OK though
                // 2017-08-30: tslint 5.7.0 changed no-namespace rule to 'ignore global augmentation', reduced 293->292 below
                // 2017-10-30: tslint 5.8.0 no-empty-interface now disregards interfaces with generic types #3260, reduced 292 -> 283 below
                // 2019-03-02: tslint 15.13.0 enabled the completed-docs rules if NOT using tsconfig.json, as here, previously only ran if
                //             a tsconfig.json was specified.  That is it stopped being 'typed' = 'uses tslint' = 'uses type checker'.  
                //             The rule was in tslint.json for this test, so extra test errors appeared. tslint issue #3557.
                // 2019-05-31: tslint 15.17.0 treats IntrinsicAttributes, IntrinsicClassAttributes and IntrinsicElements interface names
                //             as failing the 'interface name must start with a capitalized I' test, version 5.13.1 didn't, +3 errors in
                //             expected2 (react.d.ts).  interface-name rule.
                // 2020-01-07: tslint 5.20.1 has stopped reporting line 2164 in react.d.ts in WebLinterTest/artifacts/
                //             tsconfig/multiple as being over 120 characters, in spite of the fact it clearly is, was reported in 
                //             earlier versions, and other lines over 120 characters in that file are being correctly reported.  Reduces
                //             errors in expected2 from 1297 to 1296. Edit: this is because of the check-strings flag for max-line-length 
                //             introduced in 5.19.0 and set to false by default.  If it is set to true the error reappears.  Whether it's
                //             really meant to allow multiple string options on a line or this is a bug I'm not sure.
                Assert.AreEqual(22, mockErrorsTableDataSource.Snapshots[expected1].Count());
                Assert.AreEqual(1296, mockErrorsTableDataSource.Snapshots[expected2].Count());
                Assert.AreEqual(5, mockErrorsTableDataSource.Snapshots[expected3].Count());
                Assert.AreEqual(3, mockErrorsTableDataSource.Snapshots[expected4].Count());
                Assert.AreEqual(3, mockErrorsTableDataSource.Snapshots[expected5].Count());
                Assert.AreEqual(3, mockErrorsTableDataSource.Snapshots[expected6].Count());
                Assert.AreEqual(4, mockErrorsTableDataSource.Snapshots[expected7].Count());
                Assert.AreEqual(11, mockErrorsTableDataSource.Snapshots[expected8].Count());
                Assert.AreEqual(4, mockErrorsTableDataSource.Snapshots[expected9].Count());
                Assert.AreEqual(3, mockErrorsTableDataSource.Snapshots[expected10].Count());

                // Pick an error and check we are generating all details - expected4 is file1.ts
                LintingError lintingError = mockErrorsTableDataSource.Snapshots[expected4].First(le => le.ErrorCode == "no-empty");
                Assert.AreEqual(expected4, lintingError.FileName);
                Assert.AreEqual("block is empty", lintingError.Message);
                Assert.AreEqual(2, lintingError.LineNumber);
                Assert.AreEqual(17, lintingError.ColumnNumber);
                Assert.AreEqual("https://palantir.github.io/tslint/rules/no-empty", lintingError.HelpLink);
                Assert.IsFalse(lintingError.IsError);
            }
            finally
            {
                TableDataSource.InjectMockErrorsTableDataSource(null);
                settings.UseTsConfig = false;
                settings.IgnoreNestedFiles = true;
            }

        }

        [TestMethod, TestCategory("Lint End to End")]
        public async Task LintSolutionUseTsconfigs()
        {
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = solution };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            MockErrorsTableDataSource mockErrorsTableDataSource = new MockErrorsTableDataSource();
            TableDataSource.InjectMockErrorsTableDataSource(mockErrorsTableDataSource);

            settings.UseTsConfig = true;

            try
            {
                bool hasVSErrors = await LintFilesCommandBase.LintLintLint(false, selectedItems);

                Assert.IsFalse(hasVSErrors);
                Assert.IsTrue(mockErrorsTableDataSource.HasErrors());
                Assert.AreEqual(7, mockErrorsTableDataSource.Snapshots.Count);

                // Note file5 is referenced by a tsconfig that isn't in the project, so doesn't get included
                string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
                string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/file2.ts");
                string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/file3.ts");
                string expected4 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
                //string expected5 = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts");
                string expected6 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file6.tsx");
                string expected7 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/file7.ts");
                string expected8 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");

                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected1));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected2));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected3));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected4));
                //Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected5));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected6));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected7));
                Assert.IsTrue(mockErrorsTableDataSource.Snapshots.Keys.Contains(expected8));

                // Similar to TslintWithTsconfigTest.LintAll, again this level of detail may be too much
                Assert.AreEqual(3, mockErrorsTableDataSource.Snapshots[expected1].Count());
                Assert.AreEqual(4, mockErrorsTableDataSource.Snapshots[expected2].Count());
                Assert.AreEqual(3, mockErrorsTableDataSource.Snapshots[expected3].Count());
                Assert.AreEqual(3, mockErrorsTableDataSource.Snapshots[expected4].Count());
                //Assert.AreEqual(4, mockErrorsTableDataSource.Snapshots[expected5].Count());
                Assert.AreEqual(11, mockErrorsTableDataSource.Snapshots[expected6].Count());
                Assert.AreEqual(4, mockErrorsTableDataSource.Snapshots[expected7].Count());
                Assert.AreEqual(5, mockErrorsTableDataSource.Snapshots[expected8].Count());
            }
            finally
            {
                TableDataSource.InjectMockErrorsTableDataSource(null);
                settings.UseTsConfig = false;
            }

        }
    }
}
