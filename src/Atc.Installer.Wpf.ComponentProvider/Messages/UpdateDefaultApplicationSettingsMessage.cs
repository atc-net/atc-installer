namespace Atc.Installer.Wpf.ComponentProvider.Messages;

public class UpdateDefaultApplicationSettingsMessage : MessageBase
{
    public UpdateDefaultApplicationSettingsMessage(
        TriggerActionType triggerActionType,
        KeyValueTemplateItemViewModel keyValueTemplateItem)
    {
        TriggerActionType = triggerActionType;
        KeyValueTemplateItem = keyValueTemplateItem;
    }

    public TriggerActionType TriggerActionType { get; }

    public KeyValueTemplateItemViewModel KeyValueTemplateItem { get; }

    public override string ToString()
        => $"{nameof(TriggerActionType)}: {TriggerActionType}, {nameof(KeyValueTemplateItem)}: {KeyValueTemplateItem}";
}