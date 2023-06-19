namespace Atc.Installer.Wpf.App.Dialogs;

public class ApplicationSettingsDialogViewModel : ViewModelBase, IApplicationSettingsDialogViewModel
{
    public IRelayCommand<NiceWindow> OkCommand => new RelayCommand<NiceWindow>(OkCommandCommandHandler);

    public ApplicationSettingsDialogViewModel(
        ApplicationOptionsViewModel applicationOptionsViewModel)
    {
        ArgumentNullException.ThrowIfNull(applicationOptionsViewModel);

        this.ApplicationOptions = applicationOptionsViewModel;

        ThemeManager.Current.ThemeChanged += OnThemeChanged;
    }

    public ApplicationOptionsViewModel ApplicationOptions { get; set; }

    private void OnThemeChanged(
        object? sender,
        ThemeChangedEventArgs e)
    {
        IsDirty = true;
    }

    private void OkCommandCommandHandler(
        NiceWindow window)
    {
        if (IsDirty)
        {
            ApplicationOptions.Theme = ThemeManager.Current.DetectTheme(Application.Current)!.Name;

            var file = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.custom.json"));
            if (file.Exists)
            {
                var dynamicJson = new DynamicJson(file);
                dynamicJson.SetValue("Application.Title", ApplicationOptions.Title);
                dynamicJson.SetValue("Application.Theme", ApplicationOptions.Theme);
                File.WriteAllText(file.FullName, dynamicJson.ToJson());

                window.DialogResult = true;
            }
        }

        ThemeManager.Current.ThemeChanged -= OnThemeChanged;

        window.Close();
    }
}