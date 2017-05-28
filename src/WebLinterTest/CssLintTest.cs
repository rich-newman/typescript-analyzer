// Modifications Copyright Rich Newman 2017
using System.Threading.Tasks;
using Xunit;
using WebLinter;

namespace WebLinterTest
{
    public class CsslintTest
    {
        [Fact]
        [Trait("Category", "CssLint")]
        public async Task Standard()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/csslint/a.css");
            Assert.True(result.Length == 0);
        }

        [Fact]
        [Trait("Category", "CssLint")]
        public async Task Multiple()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/csslint/a.css", "../../artifacts/csslint/b.css");
            Assert.True(result.Length == 0);
        }

        [Fact]
        [Trait("Category", "CssLint")]
        public async Task FileNotExist()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/csslint/doesntexist.css");
            // Running on css file should have same result as any other non-TS file
            Assert.True(result.Length == 0);

        }
    }
}
