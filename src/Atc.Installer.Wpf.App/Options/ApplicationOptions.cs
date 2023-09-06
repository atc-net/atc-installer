namespace Atc.Installer.Wpf.App.Options;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public string Title { get; set; } = string.Empty;

    public string Theme { get; set; } = string.Empty;

    public bool EnableEditingMode { get; set; }

    public bool ShowOnlyBaseSettings { get; set; }

    public override string ToString()
        => $"{nameof(Title)}: {Title}, {nameof(Theme)}: {Theme}, {nameof(EnableEditingMode)}: {EnableEditingMode}, {nameof(ShowOnlyBaseSettings)}: {ShowOnlyBaseSettings}";
}