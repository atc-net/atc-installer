// ReSharper disable NotAccessedField.Local
namespace Atc.Installer.Wpf.App;

/// <summary>
/// Interaction logic for App.
/// </summary>
public partial class App
{
    private readonly IHost host;
    private IConfiguration? configuration;

    public App()
    {
        host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(
                configurationBuilder =>
                {
                    configuration = configurationBuilder
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile("appsettings.custom.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();
                })
            .ConfigureServices((_, services) => ConfigureServices(services))
            .Build();

        ServiceProvider = host.Services;
    }

    public IServiceProvider ServiceProvider { get; }

    private void ConfigureServices(
        IServiceCollection services)
    {
        services
            .AddOptions<ApplicationOptions>()
            .Bind(configuration!.GetRequiredSection(ApplicationOptions.SectionName));

        services.AddSingleton<INetworkShellService, NetworkShellService>();
        services.AddSingleton<IInstalledAppsInstallerService, InstalledAppsInstallerService>();

        services.AddSingleton<IElasticSearchServerInstallerService, ElasticSearchServerInstallerService>();
        services.AddSingleton<IInternetInformationServerInstallerService, InternetInformationServerInstallerService>();
        services.AddSingleton<IPostgreSqlServerInstallerService, PostgreSqlServerInstallerService>();
        services.AddSingleton<IWindowsApplicationInstallerService, WindowsApplicationInstallerService>();

        services.AddSingleton<IApplicationSettingsDialogViewModel, ApplicationSettingsDialogViewModel>();
        services.AddSingleton<IMainWindowViewModelBase, MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }

    private async void ApplicationStartup(
        object sender,
        StartupEventArgs e)
    {
        await host
            .StartAsync()
            .ConfigureAwait(false);

        var applicationOptions = new ApplicationOptions();
        configuration!
            .GetRequiredSection(ApplicationOptions.SectionName)
            .Bind(applicationOptions);

        Thread.CurrentThread.CurrentUICulture = GlobalizationConstants.EnglishCultureInfo;

        var theme = string.IsNullOrEmpty(applicationOptions.Theme)
            ? "Light.Steel"
            : applicationOptions.Theme;

        ThemeManager.Current.ChangeTheme(Current, theme);

        var mainWindow = host
            .Services
            .GetService<MainWindow>()!;

        mainWindow.Show();
    }

    private async void ApplicationExit(
        object sender,
        ExitEventArgs e)
    {
        await host
            .StopAsync()
            .ConfigureAwait(false);

        host.Dispose();
    }
}