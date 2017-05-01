// Modifications Copyright Rich Newman 2017
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebLinter;

namespace WebLinterTest
{
    [TestClass]
    public class TshintTest
    {
        [TestMethod, TestCategory("TSLint")]
        public async Task StandardTs()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/tslint/a.ts");
            Assert.IsTrue(result.First().HasErrors);
            Assert.IsFalse(string.IsNullOrEmpty(result.First().Errors.First().FileName), "File name is empty");
            Assert.AreEqual(7, result.First().Errors.Count);
            Assert.AreEqual("if statements must be braced", result.First().Errors.First().Message);
        }

        [TestMethod, TestCategory("TSLint")]
        public async Task StandardTsNoErrors()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/tslint/e.ts");
            Assert.IsFalse(result.First().HasErrors);
            Assert.AreEqual(0, result.First().Errors.Count);
            Assert.IsFalse(string.IsNullOrEmpty(result.First().FileNames.First()), "File name is empty");
        }

        [TestMethod, TestCategory("TSLint")]
        public async Task StandardTsx()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/tslint/c.tsx");
            Assert.IsTrue(result.First().HasErrors);
            Assert.IsFalse(string.IsNullOrEmpty(result.First().Errors.First().FileName), "File name is empty");
            Assert.AreEqual(5, result.First().Errors.Count);
            Assert.AreEqual("comment must start with lowercase letter", result.First().Errors.First().Message);
        }

        [TestMethod, TestCategory("TSLint")]
        public async Task StandardTsxNoErrors()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/tslint/d.tsx");
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
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/tslint/doesntexist.ts");
            Assert.IsTrue(result.First().HasErrors);
        }

        [TestMethod, TestCategory("TSLint")]
        public async Task TsxFileNotExist()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/tslint/doesntexist.tsx");
            Assert.IsTrue(result.First().HasErrors);
        }
    }
}
