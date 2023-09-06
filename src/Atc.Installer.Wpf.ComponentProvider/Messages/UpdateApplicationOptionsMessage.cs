namespace Atc.Installer.Wpf.ComponentProvider.Messages;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "OK - used for MVVM message.")]
public class UpdateApplicationOptionsMessage : MessageBase
{
    public UpdateApplicationOptionsMessage(
        bool enableEditingMode,
        bool showOnlyBaseSettings)
    {
        EnableEditingMode = enableEditingMode;
        ShowOnlyBaseSettings = showOnlyBaseSettings;
    }

    public bool EnableEditingMode { get; }

    public bool ShowOnlyBaseSettings { get; }

    public override string ToString()
        => $"{nameof(EnableEditingMode)}: {EnableEditingMode}, {nameof(ShowOnlyBaseSettings)}: {ShowOnlyBaseSettings}";
}