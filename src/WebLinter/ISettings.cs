namespace WebLinter
{
    public interface ISettings
    {
        bool TSLintEnable { get; }

        bool TSLintWarningsAsErrors { get; }
    }
}
