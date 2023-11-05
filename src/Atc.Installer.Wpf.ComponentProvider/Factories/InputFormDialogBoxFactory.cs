namespace Atc.Installer.Wpf.ComponentProvider.Factories;

public static class InputFormDialogBoxFactory
{
    public static InputFormDialogBox CreateForNewApplicationSettings(
        ApplicationSettingsViewModel? defaultSettings)
    {
        var labelControls = LabelControlsFactory.CreateForApplicationSettings(
            defaultSettings,
            updateItem: null);

        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        return new InputFormDialogBox(
            Application.Current.MainWindow!,
            "New key/value",
            labelControlsForm);
    }

    public static InputFormDialogBox CreateForEditApplicationSettings(
        ApplicationSettingsViewModel? defaultSettings,
        KeyValueTemplateItemViewModel updateItem)
    {
        var labelControls = LabelControlsFactory.CreateForApplicationSettings(
            defaultSettings,
            updateItem);

        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        return new InputFormDialogBox(
            Application.Current.MainWindow!,
            "Edit key/value",
            labelControlsForm);
    }

    public static InputFormDialogBox CreateForNewConfigurationSettingsFiles(
        ComponentProviderViewModel? refComponentProvider)
    {
        var labelControls = LabelControlsFactory.CreateForConfigurationSettingsFiles(
            refComponentProvider,
            updateItem: null);

        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        return new InputFormDialogBox(
            Application.Current.MainWindow!,
            "New key/value",
            labelControlsForm);
    }

    public static InputFormDialogBox CreateForEditConfigurationSettingsFiles(
        ComponentProviderViewModel? refComponentProvider,
        KeyValueTemplateItemViewModel? updateItem)
    {
        var labelControls = LabelControlsFactory.CreateForConfigurationSettingsFiles(
            refComponentProvider,
            updateItem);

        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        return new InputFormDialogBox(
            Application.Current.MainWindow!,
            "Edit key/value",
            labelControlsForm);
    }

    public static InputFormDialogBox CreateForNewFirewallRules(
        ComponentProviderViewModel? refComponentProvider)
    {
        var labelControls = LabelControlsFactory.CreateForFirewallRules(
            refComponentProvider,
            updateItem: null);

        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        return new InputFormDialogBox(
            Application.Current.MainWindow!,
            "New firewall rule",
            labelControlsForm);
    }

    public static InputFormDialogBox CreateForEditFirewallRules(
        ComponentProviderViewModel? refComponentProvider,
        FirewallRuleViewModel? updateItem)
    {
        var labelControls = LabelControlsFactory.CreateForFirewallRules(
            refComponentProvider,
            updateItem);

        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        return new InputFormDialogBox(
            Application.Current.MainWindow!,
            "Edit firewall rule",
            labelControlsForm);
    }

    public static InputFormDialogBox CreateForNewFolderPermissions(
        ComponentProviderViewModel refComponentProvider)
    {
        var labelControls = LabelControlsFactory.CreateForFolderPermissions(refComponentProvider);

        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        return new InputFormDialogBox(
            Application.Current.MainWindow!,
            "New folder permission",
            labelControlsForm);
    }
}