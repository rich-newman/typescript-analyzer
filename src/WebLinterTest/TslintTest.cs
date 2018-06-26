// Modifications Copyright Rich Newman 2017
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebLinter;
using System.IO;

namespace WebLinterTest
{
    /// <summary>
    /// Tests linting of individual .ts and .tsx files when use tsconfig.json is false
    /// </summary>
    [TestClass]
    public class TsLintTest
    {
        [TestMethod, TestCategory("TSLint")]
        public async Task StandardTs()
        {
            var result = await LinterFactory.Lint(MockSettings.Instance, "../../artifacts/tslint/a.ts");
            Assert.IsTrue(result.First().HasErrors);
            Assert.IsFalse(string.IsNullOrEmpty(result.First().Errors.First().FileName), "File name is empty");
            Assert.AreEqual(13, result.First().Errors.Count);
            Assert.AreEqual("if statements must be braced", result.First().Errors.First().Message);
        }

        [TestMethod, TestCategory("TSLint")]
        public async Task StandardTsFixErrors()
        {
            try
            {
                File.Copy("../../artifacts/tslint/a.ts", "../../artifacts/tslint/aTest.ts", true);
                var result = await LinterFactory.Lint(MockSettings.Instance, true, false, "../../artifacts/tslint/aTest.ts");
                // Now, bizarrely, we have to fix twice to fix var -> const with the recommended TSLint rules
                // See TSLint issues #2835, #2843, #2625
                result = await LinterFactory.Lint(MockSettings.Instance, true, false, "../../artifacts/tslint/aTest.ts");
                Assert.IsTrue(result.First().HasErrors);
                Assert.IsFalse(string.IsNullOrEmpty(result.First().Errors.First().FileName), "File name is empty");
                // 2017-10-30: tslint 5.8.0 curly has a fixer #3262, reduces 4 -> 2 below
                Assert.AreEqual(2, result.First().Errors.Count);
                Assert.AreEqual("Calls to 'console.log' are not allowed.", result.First().Errors.First().Message);
                string actual = File.ReadAllText("../../artifacts/tslint/aTest.ts");
                string expected = File.ReadAllText("../../artifacts/tslint/aFixed.ts");
                // normalize by removing space indents and using Windows line breaks
                actual = System.Text.RegularExpressions.Regex.Replace(actual, "\r?\n\\s+", "\r\n");
                expected = System.Text.RegularExpressions.Regex.Replace(expected, "\r?\n\\s+", "\r\n");
                Assert.AreEqual(expected, actual);
            }
            finally
            {
                File.Delete("../../artifacts/tslint/aTest.ts");
            }
        }

        [TestMethod, TestCategory("TSLint")]
        public async Task StandardTsNoErrors()
        {
            var result = await LinterFactory.Lint(MockSettings.Instance, "../../artifacts/tslint/e.ts");
            Assert.IsFalse(result.First().HasErrors);
            Assert.AreEqual(0, result.First().Errors.Count);
            Assert.IsFalse(string.IsNullOrEmpty(result.First().FileNames.First()), "File name is empty");
        }

        [TestMethod, TestCategory("TSLint")]
        public async Task StandardTsx()
        {
            var result = await LinterFactory.Lint(MockSettings.Instance, "../../artifacts/tslint/c.tsx");
            Assert.IsTrue(result.First().HasErrors);
            Assert.IsFalse(string.IsNullOrEmpty(result.First().Errors.First().FileName), "File name is empty");
            Assert.AreEqual(6, result.First().Errors.Count);
            Assert.AreEqual("The class method 'sayHello' must be marked either 'private', 'public', or 'protected'", result.First().Errors.First().Message);
        }

        [TestMethod, TestCategory("TSLint")]
        public async Task StandardTsxFixErrors()
        {
            try
            {
                File.Copy("../../artifacts/tslint/c.tsx", "../../artifacts/tslint/cTest.tsx", true);
                var result = await LinterFactory.Lint(MockSettings.Instance, true, false, "../../artifacts/tslint/cTest.tsx");
                Assert.IsFalse(result.First().HasErrors);
                Assert.AreEqual(0, result.First().Errors.Count);
                string actual = File.ReadAllText("../../artifacts/tslint/cTest.tsx");
                string expected = File.ReadAllText("../../artifacts/tslint/cFixed.tsx");
                Assert.AreEqual(expected, actual);
            }
            finally
            {
                File.Delete("../../artifacts/tslint/cTest.tsx");
            }
}

[TestMethod, TestCategory("TSLint")]
        public async Task StandardTsxNoErrors()
        {
            var result = await LinterFactory.Lint(MockSettings.Instance, "../../artifacts/tslint/d.tsx");
            Assert.IsFalse(result.First().HasErrors);
            Assert.AreEqual(0, result.First().Errors.Count);
            Assert.IsFalse(string.IsNullOrEmpty(result.First().FileNames.First()), "File name is empty");
        }

        //[TestMethod, TestCategory("TSLint")]
        //public void Multiple()
        //{
        //    var result = LinterFactory.Lint(Settings.CWD, Settings.Instance, "../../artifacts/tslint/b.ts", "../../artifacts/tslint/a.ts");
        //    var first = result.First();
        //    var firstErrors = first.Errors.ToArray();
        //    Assert.IsTrue(first.HasErrors);
        //    Assert.IsFalse(string.IsNullOrEmpty(firstErrors.First().FileName), "File name is empty");
        //    Assert.AreEqual(14, firstErrors.Length);
        //    Assert.AreEqual("if statements must be braced", firstErrors.First().Message);
        //}

        [TestMethod, TestCategory("TSLint")]
        public async Task TsFileNotExist()
        {
            var result = await LinterFactory.Lint(MockSettings.Instance, "../../artifacts/tslint/doesntexist.ts");
            Assert.IsTrue(result.First().HasErrors);
        }

        [TestMethod, TestCategory("TSLint")]
        public async Task TsxFileNotExist()
        {
            var result = await LinterFactory.Lint(MockSettings.Instance, "../../artifacts/tslint/doesntexist.tsx");
            Assert.IsTrue(result.First().HasErrors);
        }
    }
}
