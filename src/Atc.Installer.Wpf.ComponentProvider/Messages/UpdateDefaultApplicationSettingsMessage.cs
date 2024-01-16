namespace Atc.Installer.Wpf.ComponentProvider.Messages;

public class UpdateDefaultApplicationSettingsMessage(
    TriggerActionType triggerActionType,
    KeyValueTemplateItemViewModel keyValueTemplateItem)
    : MessageBase
{
    public TriggerActionType TriggerActionType { get; } = triggerActionType;

    public KeyValueTemplateItemViewModel KeyValueTemplateItem { get; } = keyValueTemplateItem;

    public override string ToString()
        => $"{nameof(TriggerActionType)}: {TriggerActionType}, {nameof(KeyValueTemplateItem)}: {KeyValueTemplateItem}";
}