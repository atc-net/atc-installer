// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ReplaceSubstringWithRangeIndexer
namespace Atc.Installer.Integration.WindowsApplication;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SupportedOSPlatform("windows")]
public sealed class WindowsApplicationInstallerService : IWindowsApplicationInstallerService
{
    private readonly IInstalledAppsInstallerService iaInstallerService;

    public WindowsApplicationInstallerService(
        IInstalledAppsInstallerService installedAppsInstallerService)
    {
        this.iaInstallerService = installedAppsInstallerService ?? throw new ArgumentNullException(nameof(installedAppsInstallerService));
    }

    public bool IsMicrosoftDonNetFramework48()
        => iaInstallerService.IsMicrosoftDonNetFramework48();

    public bool IsMicrosoftDonNet7()
        => iaInstallerService.IsMicrosoftDonNet7();

    public ComponentRunningState GetServiceState(
        string serviceName)
    {
        try
        {
            var services = ServiceController.GetServices();
            var service = services.FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            if (service is null)
            {
                return ComponentRunningState.NotAvailable;
            }

            return service.Status switch
            {
                ServiceControllerStatus.ContinuePending => ComponentRunningState.Checking,
                ServiceControllerStatus.Paused => ComponentRunningState.Stopped,
                ServiceControllerStatus.PausePending => ComponentRunningState.Checking,
                ServiceControllerStatus.Running => ComponentRunningState.Running,
                ServiceControllerStatus.StartPending => ComponentRunningState.Checking,
                ServiceControllerStatus.Stopped => ComponentRunningState.Stopped,
                ServiceControllerStatus.StopPending => ComponentRunningState.Checking,
                _ => throw new SwitchExpressionException(service.Status),
            };
        }
        catch
        {
            return ComponentRunningState.Unknown;
        }
    }

    public async Task<bool> StopService(
        string serviceName,
        ushort timeoutInSeconds = 60)
    {
        try
        {
            var services = ServiceController.GetServices();
            var service =
                services.FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            if (service is null ||
                service.Status != ServiceControllerStatus.Running)
            {
                return false;
            }

            await Task
                .Run(() =>
                {
                    service.Stop();
                    service.WaitForStatus(
                        ServiceControllerStatus.Stopped,
                        TimeSpan.FromSeconds(timeoutInSeconds));
                })
                .ConfigureAwait(false);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StartService(
        string serviceName,
        ushort timeoutInSeconds = 60)
    {
        try
        {
            var services = ServiceController.GetServices();
            var service =
                services.FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            if (service is null ||
                service.Status != ServiceControllerStatus.Stopped)
            {
                return false;
            }

            await Task
                .Run(() =>
                {
                    service.Start();
                    service.WaitForStatus(
                        ServiceControllerStatus.Running,
                        TimeSpan.FromSeconds(timeoutInSeconds));
                })
                .ConfigureAwait(false);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public ComponentRunningState GetApplicationState(
        string applicationName)
    {
        ArgumentException.ThrowIfNullOrEmpty(applicationName);

        try
        {
            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName.Equals(applicationName, StringComparison.OrdinalIgnoreCase))
                {
                    return ComponentRunningState.Running;
                }
            }

            return ComponentRunningState.NotAvailable;
        }
        catch
        {
            return ComponentRunningState.Unknown;
        }
    }

    public ComponentRunningState GetApplicationState(
        FileInfo applicationFile)
    {
        ArgumentNullException.ThrowIfNull(applicationFile);

        var applicationName = applicationFile.Name.Substring(
            startIndex: 0,
            applicationFile.Name.Length - applicationFile.Extension.Length);
        return GetApplicationState(applicationName);
    }

    public bool StopApplication(
        string applicationName,
        ushort timeoutInSeconds = 60)
    {
        ArgumentException.ThrowIfNullOrEmpty(applicationName);

        try
        {
            var result = false;
            foreach (var process in Process.GetProcesses())
            {
                if (!process.ProcessName.Equals(applicationName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit(TimeSpan.FromSeconds(timeoutInSeconds));
                process.Dispose();
                result = true;
            }

            return result;
        }
        catch
        {
            return false;
        }
    }

    public bool StopApplication(
        FileInfo applicationFile,
        ushort timeoutInSeconds = 60)
    {
        ArgumentNullException.ThrowIfNull(applicationFile);

        var applicationName = applicationFile.Name.Substring(
            startIndex: 0,
            applicationFile.Name.Length - applicationFile.Extension.Length);
        return StopApplication(applicationName, timeoutInSeconds);
    }

    public bool StartApplication(
        string applicationName,
        ushort timeoutInSeconds = 60)
    {
        ArgumentException.ThrowIfNullOrEmpty(applicationName);

        try
        {
            Process.Start(applicationName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool StartApplication(
        FileInfo applicationFile,
        ushort timeoutInSeconds = 60)
    {
        ArgumentNullException.ThrowIfNull(applicationFile);

        try
        {
            Process.Start(applicationFile.FullName);
            return true;
        }
        catch
        {
            return false;
        }
    }
}