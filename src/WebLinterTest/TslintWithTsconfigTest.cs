using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLinter;

namespace WebLinterTest
{
    [TestClass]
    public class TslintWithTsconfigTest
    {
        [TestMethod, TestCategory("TSLint with tsconfig")]
        public async Task BasicLint()
        {
            // tslint.json is the usual file but has had completed-docs added to allow us to test the type checking

            // Arrange
            string mainProjectTsconfig = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string[] fileNames = new string[] { mainProjectTsconfig };

            // Act
            var result = await LinterFactory.Lint(new Settings() { UseTsConfig = true }, false, false, fileNames);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(15, result[0].Errors.Count);

            // file3.ts is excluded from this tsconfig.json, in spite of being in the VS project.
            // It has errors but they shouldn't appear here
            IList<LintingError> file3Errors = GetErrorsForFile("file3.ts", result[0].Errors);
            Assert.IsTrue(file3Errors.Count == 0);

            // file2.ts is the reverse of the above: it's included in the tsconfig.json file, but is not in the VS project
            // It should have 4 errors, one of them our completed-docs
            IList<LintingError> file2Errors = GetErrorsForFile("file2.ts", result[0].Errors);
            Assert.IsTrue(file2Errors.Count == 4);
            LintingError completedDocsError = file2Errors.First(le => le.ErrorCode == "completed-docs");
            Assert.IsNotNull(completedDocsError);
            Assert.AreEqual(2, completedDocsError.LineNumber);
            Assert.AreEqual(0, completedDocsError.ColumnNumber);
        }

        [TestMethod, TestCategory("TSLint with tsconfig")]
        public async Task LintWithDuplicateErrors()
        {
            // Arrange
            string topTsconfig = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string folderbTsconfig = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            string[] fileNames = new string[] { topTsconfig, folderbTsconfig };

            // Act
            var result = await LinterFactory.Lint(new Settings() { UseTsConfig = true }, false, false, fileNames);

            // Assert
            // file2 is in both tsconfigs.  It has 4 errors.  With the old code we got 8 in the Error List, and here.
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(18, result[0].Errors.Count);

            IList<LintingError> file2Errors = GetErrorsForFile("file2.ts", result[0].Errors);
            Assert.IsTrue(file2Errors.Count == 4);
            LintingError completedDocsError = file2Errors.First(le => le.ErrorCode == "completed-docs");
            Assert.IsNotNull(completedDocsError);
            Assert.AreEqual(2, completedDocsError.LineNumber);
            Assert.AreEqual(0, completedDocsError.ColumnNumber);
        }

        [TestMethod, TestCategory("TSLint with tsconfig")]
        public async Task LintAll()
        {
            // Arrange
            string topTsconfig = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string folderaTsconfig = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string folderbTsconfig = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
            // The tsconfig.json below isn't in a VS project, so couldn't be linted from the UI - can be here
            string folderbTsconfigEmptyProject = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/tsconfig.json");
            string[] fileNames = new string[] { topTsconfig, folderaTsconfig, folderbTsconfig, folderbTsconfigEmptyProject };

            // Act
            var result = await LinterFactory.Lint(new Settings() { UseTsConfig = true }, false, false, fileNames);

            // Assert
            // file2 is in both tsconfigs.  It has 4 errors.  With the old code we got 8 in the Error List, and here.
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(22, result[0].Errors.Count);

            IList<LintingError> file1Errors = GetErrorsForFile("file1.ts", result[0].Errors);
            Assert.IsTrue(file1Errors.Count == 3);

            IList<LintingError> file2Errors = GetErrorsForFile("file2.ts", result[0].Errors);
            Assert.IsTrue(file2Errors.Count == 4);
            LintingError completedDocsError = file2Errors.First(le => le.ErrorCode == "completed-docs");
            Assert.IsNotNull(completedDocsError);
            Assert.AreEqual(2, completedDocsError.LineNumber);
            Assert.AreEqual(0, completedDocsError.ColumnNumber);

            IList<LintingError> file3Errors = GetErrorsForFile("file3.ts", result[0].Errors);
            Assert.IsTrue(file3Errors.Count == 3);

            IList<LintingError> file4Errors = GetErrorsForFile("file4.ts", result[0].Errors);
            Assert.IsTrue(file4Errors.Count == 3);

            IList<LintingError> file5Errors = GetErrorsForFile("file5.ts", result[0].Errors);
            Assert.IsTrue(file5Errors.Count == 4);

            IList<LintingError> testErrors = GetErrorsForFile("test.ts", result[0].Errors);
            Assert.IsTrue(testErrors.Count == 5);

        }

        private IList<LintingError> GetErrorsForFile(string fileName, IEnumerable<LintingError> allErrors)
        {
            List<LintingError> result = new List<LintingError>();
            foreach (LintingError lintingError in allErrors)
            {
                if (lintingError.FileName.EndsWith(fileName)) result.Add(lintingError);
            }
            return result;
        }
    }
}
