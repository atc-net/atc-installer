namespace Atc.Installer.Wpf.App.Dialogs;

public class ApplicationSettingsDialogViewModel : ViewModelBase, IApplicationSettingsDialogViewModel
{
    public IRelayCommand<NiceDialogBox> OkCommand
        => new RelayCommand<NiceDialogBox>(
            OkCommandHandler);

    public IRelayCommand<NiceDialogBox> CancelCommand
        => new RelayCommand<NiceDialogBox>(
            CancelCommandHandler);

    public ApplicationSettingsDialogViewModel(
        ApplicationOptionsViewModel applicationOptionsViewModel)
    {
        ArgumentNullException.ThrowIfNull(applicationOptionsViewModel);

        this.ApplicationOptions = applicationOptionsViewModel.Clone();
        this.ApplicationOptionsBackup = applicationOptionsViewModel.Clone();

        ThemeManager.Current.ThemeChanged += OnThemeChanged;
    }

    public ApplicationOptionsViewModel ApplicationOptions { get; set; }

    public ApplicationOptionsViewModel ApplicationOptionsBackup { get; set; }

    private void OnThemeChanged(
        object? sender,
        ThemeChangedEventArgs e)
    {
        ApplicationOptions.IsDirty = true;
    }

    private void OkCommandHandler(
        NiceDialogBox dialogBox)
    {
        ThemeManager.Current.ThemeChanged -= OnThemeChanged;

        if (ApplicationOptions.IsDirty)
        {
            ApplicationOptions.Theme = ThemeManager.Current.DetectTheme(Application.Current)!.Name;

            var file = new FileInfo(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    Constants.CustomAppSettingsFileName));
            if (file.Exists)
            {
                var dynamicJson = new DynamicJson(file);
                dynamicJson.SetValue($"Application.{nameof(ApplicationOptions.Title)}", ApplicationOptions.Title);
                dynamicJson.SetValue($"Application.{nameof(ApplicationOptions.Theme)}", ApplicationOptions.Theme);
                dynamicJson.SetValue($"Application.{nameof(ApplicationOptions.OpenRecentFileOnStartup)}", ApplicationOptions.OpenRecentFileOnStartup);
                dynamicJson.SetValue($"Application.{nameof(ApplicationOptions.EnableEditingMode)}", ApplicationOptions.EnableEditingMode);
                dynamicJson.SetValue($"Application.{nameof(ApplicationOptions.ShowOnlyBaseSettings)}", ApplicationOptions.ShowOnlyBaseSettings);
                File.WriteAllText(file.FullName, dynamicJson.ToJson());

                File.Copy(
                    file.FullName,
                    Path.Combine(
                        App.InstallerProgramDataDirectory.FullName,
                        Constants.CustomAppSettingsFileName),
                    overwrite: true);

                dialogBox.DialogResult = true;
            }

            ApplicationOptions.IsDirty = false;

            Messenger.Default.Send(
                new UpdateApplicationOptionsMessage(
                    ApplicationOptions.EnableEditingMode,
                    ApplicationOptions.ShowOnlyBaseSettings));
        }

        dialogBox.Close();
    }

    private void CancelCommandHandler(
        NiceDialogBox dialogBox)
    {
        ThemeManager.Current.ThemeChanged -= OnThemeChanged;

        if (ApplicationOptions.IsDirty)
        {
            if (!ApplicationOptions.Theme.Equals(ThemeManager.Current.DetectTheme(Application.Current)!.Name, StringComparison.Ordinal))
            {
                var sa = ApplicationOptions.Theme.Split('.');
                ThemeManager.Current.ChangeTheme(Application.Current, sa[0], sa[1]);
            }

            ApplicationOptions = ApplicationOptionsBackup.Clone();
        }

        dialogBox.Close();
    }
}