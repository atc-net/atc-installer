// ReSharper disable NotAccessedField.Local
namespace Atc.Installer.Wpf.App;

/// <summary>
/// Interaction logic for App.
/// </summary>
public partial class App
{
    private readonly IHost host;
    private IConfiguration? configuration;

    public static DirectoryInfo InstallerTempDirectory => new(Path.Combine(Path.GetTempPath(), "atc-installer"));

    public static DirectoryInfo InstallerProgramDataDirectory => new(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ATC"), "atc-installer"));

    public App()
    {
        if (!InstallerProgramDataDirectory.Exists)
        {
            Directory.CreateDirectory(InstallerProgramDataDirectory.FullName);
        }

        FixLegacyFileLocations();

        RestoreCustomAppSettingsIfNeeded();

        host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(
                configurationBuilder =>
                {
                    configuration = configurationBuilder
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile(Constants.CustomAppSettingsFileName, optional: true, reloadOnChange: true)
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
        services.AddSingleton<IAzureStorageAccountInstallerService, AzureStorageAccountInstallerService>();

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

    private static void FixLegacyFileLocations()
    {
        var customAppSettings = new FileInfo(Path.Combine(InstallerTempDirectory.FullName, Constants.CustomAppSettingsFileName));
        if (customAppSettings.Exists)
        {
            File.Move(customAppSettings.FullName, Path.Combine(InstallerProgramDataDirectory.FullName, Constants.CustomAppSettingsFileName));
        }

        var recentOpenFilesFile = new FileInfo(Path.Combine(InstallerTempDirectory.FullName, Constants.RecentOpenFilesFileName));
        if (recentOpenFilesFile.Exists)
        {
            File.Move(recentOpenFilesFile.FullName, Path.Combine(InstallerProgramDataDirectory.FullName, Constants.RecentOpenFilesFileName));
        }
    }

    private static void RestoreCustomAppSettingsIfNeeded()
    {
        var currentFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.CustomAppSettingsFileName));
        var backupFile = new FileInfo(Path.Combine(InstallerProgramDataDirectory.FullName, Constants.CustomAppSettingsFileName));
        if (!currentFile.Exists ||
            !backupFile.Exists ||
            currentFile.LastWriteTime == backupFile.LastWriteTime)
        {
            return;
        }

        File.Copy(backupFile.FullName, currentFile.FullName, overwrite: true);
    }
}