// Modifications Copyright Rich Newman 2017
namespace WebLinter
{
    public interface ISettings
    {
        bool TSLintEnable { get; }

        bool TSLintWarningsAsErrors { get; }
    }
}
