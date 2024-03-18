namespace Atc.Installer.Wpf.App.Options;

public sealed class ApplicationOptions : BasicApplicationOptions
{
    public string Title { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public bool EnableEditingMode { get; set; }

    public bool ShowOnlyBaseSettings { get; set; }

    public override string ToString()
        => $"{base.ToString()}, {nameof(Title)}: {Title}, {nameof(Icon)}: {Icon}, {nameof(EnableEditingMode)}: {EnableEditingMode}, {nameof(ShowOnlyBaseSettings)}: {ShowOnlyBaseSettings}";
}