// Modifications Copyright Rich Newman 2017
using System.Threading.Tasks;
using Xunit;
using WebLinter;

namespace WebLinterTest
{
    public class CoffeelintTest
    {
        [Fact]
        [Trait("Category", "CoffeeLint")]
        public async Task Standard()
        {
            var result = await LinterFactory.Lint(Settings.Instance, "../../artifacts/coffeelint/a.coffee");
            Assert.True(result.Length == 0);
        }

        [Fact]
        [Trait("Category", "CoffeeLint")]
        public async Task Multiple()
        {
            var result = await LinterFactory.Lint(Settings.Instance, "../../artifacts/coffeelint/a.coffee", "../../artifacts/coffeelint/b.coffee");
            Assert.True(result.Length == 0);
        }

        [Fact]
        [Trait("Category", "CoffeeLint")]
        public async Task FileDontExist()
        {
            var result = await LinterFactory.Lint(Settings.Instance, "../../artifacts/coffeelint/doesntexist.coffee");
            Assert.True(result.Length == 0);
        }
    }
}
