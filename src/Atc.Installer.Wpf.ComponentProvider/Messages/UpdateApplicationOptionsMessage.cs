namespace Atc.Installer.Wpf.ComponentProvider.Messages;

public class UpdateApplicationOptionsMessage(
    bool enableEditingMode,
    bool showOnlyBaseSettings) : MessageBase
{
    public bool EnableEditingMode { get; } = enableEditingMode;

    public bool ShowOnlyBaseSettings { get; } = showOnlyBaseSettings;

    public override string ToString()
        => $"{nameof(EnableEditingMode)}: {EnableEditingMode}, {nameof(ShowOnlyBaseSettings)}: {ShowOnlyBaseSettings}";
}