// Modifications Copyright Rich Newman 2017
using System.Threading.Tasks;
using Xunit;
using WebLinter;

namespace WebLinterTest
{
    public class EslintTest
    {
        [Fact]
        [Trait("Category", "ESLint")]
        public async Task Standard()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/eslint/a.js");
            Assert.True(result.Length == 0);
        }

        [Fact]
        [Trait("Category", "ESLint")]
        public async Task MultipleInput()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/eslint/a.js", "../../artifacts/eslint/b.js");
            Assert.True(result.Length == 0);
        }

        [Fact]
        [Trait("Category", "ESLint")]
        public async Task JSX()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/eslint/a.jsx");
            Assert.True(result.Length == 0);
        }

        [Fact]
        [Trait("Category", "ESLint")]
        public async Task FileNotExist()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/eslint/doesntexist.js");
            Assert.True(result.Length == 0);
        }
    }
}
