namespace Atc.Installer.Wpf.App.Options;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public string Title { get; set; } = string.Empty;

    public override string ToString()
        => $"{nameof(Title)}: {Title}";
}