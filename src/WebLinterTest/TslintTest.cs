// Modifications Copyright Rich Newman 2017
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using WebLinter;
using System.IO;

namespace WebLinterTest
{
    public class TsLintTest
    {
        [Fact]
        [Trait("Category", "TSLint")]
        public async Task StandardTs()
        {
            await LinterFactory.EnsureEdgeFolderCreated();
            var result = await LinterFactory.Lint(Settings.Instance, "../../artifacts/tslint/a.ts");
            Assert.True(result.First().HasErrors);
            Assert.False(string.IsNullOrEmpty(result.First().Errors.First().FileName), "File name is empty");
            Assert.Equal(13, result.First().Errors.Count);
            Assert.Equal("if statements must be braced", result.First().Errors.First().Message);
        }

        [Fact]
        [Trait("Category", "TSLint")]
        public async Task StandardTsFixErrors()
        {
            await LinterFactory.EnsureEdgeFolderCreated();
            try
            {
                File.Copy("../../artifacts/tslint/a.ts", "../../artifacts/tslint/aTest.ts", true);
                var result = await LinterFactory.Lint(Settings.Instance, true, false, "../../artifacts/tslint/aTest.ts");
                // Now, bizarrely, we have to fix twice to fix var -> const with the recommended TSLint rules
                // See TSLint issues #2835, #2843, #2625
                result = await LinterFactory.Lint(Settings.Instance, true, false, "../../artifacts/tslint/aTest.ts");
                Assert.True(result.First().HasErrors);
                Assert.False(string.IsNullOrEmpty(result.First().Errors.First().FileName), "File name is empty");
                Assert.Equal(4, result.First().Errors.Count);
                Assert.Equal("if statements must be braced", result.First().Errors.First().Message);
                string actual = File.ReadAllText("../../artifacts/tslint/aTest.ts");
                string expected = File.ReadAllText("../../artifacts/tslint/aFixed.ts");
                Assert.Equal(expected, actual);
            }
            finally
            {
                File.Delete("../../artifacts/tslint/aTest.ts");
            }
        }

        [Fact]
        [Trait("Category", "TSLint")]
        public async Task StandardTsNoErrors()
        {
            await LinterFactory.EnsureEdgeFolderCreated();
            var result = await LinterFactory.Lint(Settings.Instance, "../../artifacts/tslint/e.ts");
            Assert.False(result.First().HasErrors);
            Assert.Equal(0, result.First().Errors.Count);
            Assert.False(string.IsNullOrEmpty(result.First().FileNames.First()), "File name is empty");
        }

        [Fact]
        [Trait("Category", "TSLint")]
        public async Task StandardTsx()
        {
            await LinterFactory.EnsureEdgeFolderCreated();
            var result = await LinterFactory.Lint(Settings.Instance, "../../artifacts/tslint/c.tsx");
            Assert.True(result.First().HasErrors);
            Assert.False(string.IsNullOrEmpty(result.First().Errors.First().FileName), "File name is empty");
            Assert.Equal(6, result.First().Errors.Count);
            Assert.Equal("The class method 'sayHello' must be marked either 'private', 'public', or 'protected'", result.First().Errors.First().Message);
        }

        [Fact]
        [Trait("Category", "TSLint")]
        public async Task StandardTsxFixErrors()
        {
            await LinterFactory.EnsureEdgeFolderCreated();
            try
            {
                File.Copy("../../artifacts/tslint/c.tsx", "../../artifacts/tslint/cTest.tsx", true);
                var result = await LinterFactory.Lint(Settings.Instance, true, false, "../../artifacts/tslint/cTest.tsx");
                Assert.True(result.First().HasErrors);
                Assert.False(string.IsNullOrEmpty(result.First().Errors.First().FileName), "File name is empty");
                Assert.Equal(1, result.First().Errors.Count);
                Assert.Equal("The class method 'sayHello' must be marked either 'private', 'public', or 'protected'", result.First().Errors.First().Message);
                string actual = File.ReadAllText("../../artifacts/tslint/cTest.tsx");
                string expected = File.ReadAllText("../../artifacts/tslint/cFixed.tsx");
                Assert.Equal(expected, actual);
            }
            finally
            {
                File.Delete("../../artifacts/tslint/cTest.tsx");
            }
        }

        [Fact]
        [Trait("Category", "TSLint")]
        public async Task StandardTsxNoErrors()
        {
            await LinterFactory.EnsureEdgeFolderCreated();
            var result = await LinterFactory.Lint(Settings.Instance, "../../artifacts/tslint/d.tsx");
            Assert.False(result.First().HasErrors);
            Assert.Equal(0, result.First().Errors.Count);
            Assert.False(string.IsNullOrEmpty(result.First().FileNames.First()), "File name is empty");
        }

        //[TestMethod, TestCategory("TSLint")]
        //public void Multiple()
        //{
        //    var result = LinterFactory.Lint(Settings.CWD, Settings.Instance, "../../artifacts/tslint/b.ts", "../../artifacts/tslint/a.ts");
        //    var first = result.First();
        //    var firstErrors = first.Errors.ToArray();
        //    Assert.True(first.HasErrors);
        //    Assert.False(string.IsNullOrEmpty(firstErrors.First().FileName), "File name is empty");
        //    Assert.Equal(14, firstErrors.Length);
        //    Assert.Equal("if statements must be braced", firstErrors.First().Message);
        //}

        [Fact]
        [Trait("Category", "TSLint")]
        public async Task TsFileNotExist()
        {
            await LinterFactory.EnsureEdgeFolderCreated();
            var result = await LinterFactory.Lint(Settings.Instance, "../../artifacts/tslint/doesntexist.ts");
            Assert.True(result.First().HasErrors);
        }

        [Fact]
        [Trait("Category", "TSLint")]
        public async Task TsxFileNotExist()
        {
            await LinterFactory.EnsureEdgeFolderCreated();
            var result = await LinterFactory.Lint(Settings.Instance, "../../artifacts/tslint/doesntexist.tsx");
            Assert.True(result.First().HasErrors);
        }
    }
}
