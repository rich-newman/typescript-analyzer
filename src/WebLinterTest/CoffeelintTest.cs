// Modifications Copyright Rich Newman 2017
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebLinter;
using System.Collections;

namespace WebLinterTest
{
    [TestClass]
    public class CoffeelintTest
    {
        [TestMethod, TestCategory("CoffeeLint")]
        public async Task Standard()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/coffeelint/a.coffee");
            Assert.IsTrue(result.Length == 0);
        }
        [TestMethod, TestCategory("CoffeeLint")]
        public async Task Multiple()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/coffeelint/a.coffee", "../../artifacts/coffeelint/b.coffee");
            Assert.IsTrue(result.Length == 0);
        }


        [TestMethod, TestCategory("CoffeeLint")]
        public async Task FileDontExist()
        {
            var result = await LinterFactory.LintAsync(Settings.Instance, "../../artifacts/coffeelint/doesntexist.coffee");
            Assert.IsTrue(result.Length == 0);
        }
    }
}
