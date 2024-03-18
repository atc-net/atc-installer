// ReSharper disable NotAccessedField.Local
namespace Atc.Installer.Wpf.App;

/// <summary>
/// Interaction logic for App.
/// </summary>
public partial class App
{
    private readonly IHost host;
    private IConfiguration? configuration;

    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "OK.")]
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
    public static readonly BitmapImage DefaultIcon = new(new Uri("pack://application:,,,/Resources/AppIcon.ico", UriKind.Absolute));

    public static DirectoryInfo InstallerTempDirectory => new(Path.Combine(Path.GetTempPath(), "atc-installer"));

    public static DirectoryInfo InstallerProgramDataDirectory => new(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ATC"), "atc-installer"));

    public static DirectoryInfo InstallerProgramDataLogsDirectory => new(Path.Combine(InstallerProgramDataDirectory.FullName, "Logs"));

    public static DirectoryInfo InstallerProgramDataProjectsDirectory => new(Path.Combine(InstallerProgramDataDirectory.FullName, "Projects"));

    public static JsonSerializerOptions JsonSerializerOptions
    {
        get
        {
            var jsonSerializerOptions = JsonSerializerOptionsFactory.Create();
            jsonSerializerOptions.PropertyNamingPolicy = null;
            return jsonSerializerOptions;
        }
    }

    public App()
    {
        EnsureInstallerDirectoriesIsCreated();

        RestoreInstallerCustomAppSettingsIfNeeded();

        TaskHelper.RunSync(UpdateProjectsInstallerFilesIfNeeded);

        host = Host.CreateDefaultBuilder()
            .ConfigureLogging((_, logging) =>
                {
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddDebug();
                    logging.AddSerilog(CreateLoggerConfigurationForSerilogFileLog());
                })
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
            .Bind(configuration!.GetRequiredSection(BasicApplicationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IGitHubReleaseService, GitHubReleaseService>();
        services.AddSingleton<INetworkShellService, NetworkShellService>();
        services.AddSingleton<IWindowsFirewallService, WindowsFirewallService>();
        services.AddSingleton<IInstalledAppsInstallerService, InstalledAppsInstallerService>();

        services.AddSingleton<IElasticSearchServerInstallerService, ElasticSearchServerInstallerService>();
        services.AddSingleton<IInternetInformationServerInstallerService, InternetInformationServerInstallerService>();
        services.AddSingleton<IPostgreSqlServerInstallerService, PostgreSqlServerInstallerService>();
        services.AddSingleton<IWindowsApplicationInstallerService, WindowsApplicationInstallerService>();
        services.AddSingleton<IAzureStorageAccountInstallerService, AzureStorageAccountInstallerService>();

        services.AddSingleton<ICheckForUpdatesBoxDialogViewModel, CheckForUpdatesBoxDialogViewModel>();
        services.AddSingleton<IMainWindowViewModel, MainWindowViewModel>();
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
            .GetRequiredSection(BasicApplicationOptions.SectionName)
            .Bind(applicationOptions);

        CultureManager.SetCultures(applicationOptions.Language);

        ThemeManagerHelper.SetThemeAndAccent(Current, applicationOptions.Theme);

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

    private static void EnsureInstallerDirectoriesIsCreated()
    {
        if (!InstallerTempDirectory.Exists)
        {
            Directory.CreateDirectory(InstallerTempDirectory.FullName);
        }

        if (!InstallerProgramDataDirectory.Exists)
        {
            Directory.CreateDirectory(InstallerProgramDataDirectory.FullName);
        }

        if (!InstallerProgramDataProjectsDirectory.Exists)
        {
            Directory.CreateDirectory(InstallerProgramDataProjectsDirectory.FullName);
        }
    }

    private static void RestoreInstallerCustomAppSettingsIfNeeded()
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

    private static async Task UpdateProjectsInstallerFilesIfNeeded()
    {
        foreach (var projectDirectory in Directory.GetDirectories(InstallerProgramDataProjectsDirectory.FullName))
        {
            await ConfigurationFileHelper
                .UpdateInstallationSettingsFromCustomAndTemplateSettingsIfNeeded(new DirectoryInfo(projectDirectory))
                .ConfigureAwait(true);
        }
    }

    private static Serilog.Core.Logger CreateLoggerConfigurationForSerilogFileLog()
    {
        var systemName = AssemblyHelper.GetSystemNameAsKebabCasing();
        var loggerConfig = new LoggerConfiguration();
        return loggerConfig
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("System", systemName)
            .Enrich.WithExceptionDetails()
            .WriteTo.File(
                path: Path.Combine(InstallerProgramDataLogsDirectory.FullName, $"{systemName}-.log"),
                rollingInterval: RollingInterval.Day,
                formatProvider: GlobalizationConstants.EnglishCultureInfo)
            .CreateLogger();
    }
}