// Modifications Copyright Rich Newman 2017
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebLinter;

namespace WebLinterTest
{
    [TestClass]
    public class EslintTest
    {
        [TestMethod, TestCategory("ESLint")]
        public async Task Standard()
        {
            var result = await LinterFactory.Lint(MockSettings.Instance, "../../artifacts/eslint/a.js");
            Assert.IsTrue(result.Length == 0);
        }

        [TestMethod, TestCategory("ESLint")]
        public async Task MultipleInput()
        {
            var result = await LinterFactory.Lint(MockSettings.Instance, "../../artifacts/eslint/a.js", "../../artifacts/eslint/b.js");
            Assert.IsTrue(result.Length == 0);
        }

        [TestMethod, TestCategory("ESLint")]
        public async Task JSX()
        {
            var result = await LinterFactory.Lint(MockSettings.Instance, "../../artifacts/eslint/a.jsx");
            Assert.IsTrue(result.Length == 0);
        }

        [TestMethod, TestCategory("ESLint")]
        public async Task FileNotExist()
        {
            var result = await LinterFactory.Lint(MockSettings.Instance, "../../artifacts/eslint/doesntexist.js");
            Assert.IsTrue(result.Length == 0);
        }
    }
}
