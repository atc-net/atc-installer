namespace Atc.Installer.Wpf.App;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "OK - partial class")]
[SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "OK.")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "OK.")]
public partial class MainWindowViewModel
{
    public IRelayCommandAsync OpenConfigurationCommand => new RelayCommandAsync(OpenConfigurationCommandHandler);

    public IRelayCommandAsync ApplicationAboutCommand => new RelayCommandAsync(ApplicationAboutCommandHandler);

    private Task OpenConfigurationCommandHandler()
    {
        var openFileDialog = new OpenFileDialog
        {
            Multiselect = false,
            Filter = "Configuration Files(.json)|*.json",
        };

        if (openFileDialog.ShowDialog() != true)
        {
            return Task.CompletedTask;
        }

        try
        {
            var json = File.ReadAllText(openFileDialog.FileName);

            var installationOptions = JsonSerializer.Deserialize<InstallationOption>(
                json,
                Serialization.JsonSerializerOptionsFactory.Create());

            if (installationOptions is null)
            {
                throw new IOException($"Invalid format in {openFileDialog.FileName}");
            }

            // TODO: Imp. this.
            ////foreach (var appInstallationOption in installationOptions.Applications)
            ////{
            ////}
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
        }

        return Task.CompletedTask;
    }

    private Task ApplicationAboutCommandHandler()
    {
        // TODO: Imp. this. -> Open about box with assembly versions
        return Task.CompletedTask;
    }
}