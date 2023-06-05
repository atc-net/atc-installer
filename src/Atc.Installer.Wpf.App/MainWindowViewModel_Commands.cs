namespace Atc.Installer.Wpf.App;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "OK - partial class")]
[SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "OK.")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "OK.")]
public partial class MainWindowViewModel
{
    public IRelayCommandAsync OpenConfigurationFileCommand => new RelayCommandAsync(OpenConfigurationFileCommandHandler);

    public IRelayCommandAsync ApplicationAboutCommand => new RelayCommandAsync(ApplicationAboutCommandHandler);

    private async Task OpenConfigurationFileCommandHandler()
    {
        var openFileDialog = new OpenFileDialog
        {
            Multiselect = false,
            Filter = "Configuration Files(.json)|*.json",
        };

        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(openFileDialog.FileName);

            var installationOptions = JsonSerializer.Deserialize<InstallationOption>(
                json,
                Serialization.JsonSerializerOptionsFactory.Create());

            if (installationOptions is null)
            {
                throw new IOException($"Invalid format in {openFileDialog.FileName}");
            }

            Populate(installationOptions);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
        }
    }

    private void Populate(
        InstallationOption installationOptions)
    {
        ProjectName = installationOptions.Name;
        ComponentProviders.Clear();

        foreach (var appInstallationOption in installationOptions.Applications)
        {
            switch (appInstallationOption.ComponentType)
            {
                case ComponentType.Application or ComponentType.WindowsService:
                {
                    var vm = new WindowsApplicationComponentProviderViewModel(appInstallationOption);
                    ComponentProviders.Add(vm);
                    break;
                }

                case ComponentType.InternetInformationService:
                {
                    var vm = new InternetInformationServerComponentProviderViewModel(appInstallationOption);
                    ComponentProviders.Add(vm);
                    break;
                }
            }

            if (ComponentProviders.Count == 1)
            {
                SelectedComponentProvider = ComponentProviders[0];
            }
        }

        foreach (var vm in ComponentProviders)
        {
            vm.PrepareInstallationFiles();
            vm.StartChecking();
        }
    }

    private Task ApplicationAboutCommandHandler()
    {
        // TODO: Imp. this. -> Open about box with assembly versions
        return Task.CompletedTask;
    }
}