namespace Atc.Installer.Wpf.ComponentProvider.Messages;

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "OK - used for MVVM message.")]
public class UpdateEditingModeMessage : MessageBase
{
    public UpdateEditingModeMessage(
        bool enableEditingMode)
        => EnableEditingMode = enableEditingMode;

    public bool EnableEditingMode { get; }
}