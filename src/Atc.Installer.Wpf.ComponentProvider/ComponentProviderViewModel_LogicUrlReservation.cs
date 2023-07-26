namespace Atc.Installer.Wpf.ComponentProvider;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
public partial class ComponentProviderViewModel
{
    protected async Task EnsureUrlReservationEntryIfNeeded(
        string webProtocol,
        ushort port)
    {
        ArgumentException.ThrowIfNullOrEmpty(webProtocol);

        var useWildcard = false;
        var hostName = string.Empty;
        if (TryGetStringFromApplicationSettings("UrlReservationForWebProtocol", out var urlReservationForWebProtocol))
        {
            if (string.IsNullOrEmpty(urlReservationForWebProtocol) ||
                urlReservationForWebProtocol.Equals("Skip", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (urlReservationForWebProtocol.Equals("Remove", StringComparison.OrdinalIgnoreCase))
            {
                await EnsureUrlReservationEntryIsRemoved(
                    webProtocol,
                    port).ConfigureAwait(true);
            }
            else
            {
                if (urlReservationForWebProtocol.Equals("+", StringComparison.Ordinal) ||
                    urlReservationForWebProtocol.Equals("*", StringComparison.Ordinal))
                {
                    useWildcard = true;
                }
                else
                {
                    hostName = ResolveTemplateIfNeededByApplicationSettingsLookup(urlReservationForWebProtocol);
                }

                await EnsureUrlReservationEntryIsAdded(
                    webProtocol,
                    port,
                    useWildcard,
                    hostName).ConfigureAwait(true);
            }
        }
    }

    private async Task EnsureUrlReservationEntryIsAdded(
        string webProtocol,
        ushort port,
        bool useWildcard,
        string hostName)
    {
        var isHttps = webProtocol.Equals("https", StringComparison.OrdinalIgnoreCase);

        if (isHttps)
        {
            if (useWildcard)
            {
                var (isSucceeded, errorMessage) = await networkShellService
                    .AddUrlReservationEntryWithHttpsPortForEveryone(port)
                    .ConfigureAwait(false);
                LogUrlReservationEntryResultForAdded(isHttps, port, useWildcard, hostName, isSucceeded, errorMessage);
            }
            else
            {
                var (isSucceeded, errorMessage) = await networkShellService
                    .AddUrlReservationEntryWithHttpsPortForEveryone(hostName, port)
                    .ConfigureAwait(false);
                LogUrlReservationEntryResultForAdded(isHttps, port, useWildcard, hostName, isSucceeded, errorMessage);
            }
        }
        else
        {
            if (useWildcard)
            {
                var (isSucceeded, errorMessage) = await networkShellService
                    .AddUrlReservationEntryWithHttpPortForEveryone(port)
                    .ConfigureAwait(false);
                LogUrlReservationEntryResultForAdded(isHttps, port, useWildcard, hostName, isSucceeded, errorMessage);
            }
            else
            {
                var (isSucceeded, errorMessage) = await networkShellService
                    .AddUrlReservationEntryWithHttpPortForEveryone(hostName, port)
                    .ConfigureAwait(false);
                LogUrlReservationEntryResultForAdded(isHttps, port, useWildcard, hostName, isSucceeded, errorMessage);
            }
        }
    }

    private async Task EnsureUrlReservationEntryIsRemoved(
        string webProtocol,
        ushort port)
    {
        var isHttps = webProtocol.Equals("https", StringComparison.OrdinalIgnoreCase);

        if (isHttps)
        {
            var (isSucceeded, errorMessage) = await networkShellService
                .RemoveUrlReservationEntryByHttpsPort(port)
                .ConfigureAwait(false);
            LogUrlReservationEntryResultForRemoved(isHttps, port, isSucceeded, errorMessage);
        }
        else
        {
            var (isSucceeded, errorMessage) = await networkShellService
                .RemoveUrlReservationEntryByHttpPort(port)
                .ConfigureAwait(false);
            LogUrlReservationEntryResultForRemoved(isHttps, port, isSucceeded, errorMessage);
        }
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "OK.")]
    private void LogUrlReservationEntryResultForAdded(
        bool isHttps,
        ushort port,
        bool useWildcard,
        string hostName,
        bool isSucceeded,
        string? errorMessage)
    {
        if (isSucceeded)
        {
            if (useWildcard)
            {
                AddLogItem(
                    LogLevel.Information,
                    isHttps
                        ? $"Url reservation entry is added: https://+:{port}"
                        : $"Url reservation entry is added: http://+:{port}");
            }
            else
            {
                AddLogItem(
                    LogLevel.Information,
                    isHttps
                        ? $"Url reservation entry is added: https://{hostName}:{port}"
                        : $"Url reservation entry is added: http://{hostName}:{port}");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = errorMessage.IndentEachLineWith("    ");
            }

            if (useWildcard)
            {
                AddLogItem(
                    LogLevel.Warning,
                    isHttps
                        ? $"Url reservation entry is not added: https://+:{port}{Environment.NewLine}{errorMessage}"
                        : $"Url reservation entry is not added: http://+:{port}{Environment.NewLine}{errorMessage}");
            }
            else
            {
                AddLogItem(
                    LogLevel.Warning,
                    isHttps
                        ? $"Url reservation entry is not added: https://{hostName}:{port}{Environment.NewLine}{errorMessage}"
                        : $"Url reservation entry is not added: http://{hostName}:{port}{Environment.NewLine}{errorMessage}");
            }
        }
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "OK.")]
    private void LogUrlReservationEntryResultForRemoved(
        bool isHttps,
        ushort port,
        bool isSucceeded,
        string? errorMessage)
    {
        if (isSucceeded)
        {
            AddLogItem(
                LogLevel.Information,
                isHttps
                ? $"Url reservation entry is removed: protocol=https, port={port}"
                : $"Url reservation entry is removed: protocol=http, port={port}");
        }
        else
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = errorMessage.IndentEachLineWith("    ");
            }

            AddLogItem(
                LogLevel.Warning,
                isHttps
                ? $"Url reservation entry is not removed: protocol=https, port={port}{Environment.NewLine}{errorMessage}"
                : $"Url reservation entry is not removed: protocol=http, port={port}{Environment.NewLine}{errorMessage}");
        }
    }
}