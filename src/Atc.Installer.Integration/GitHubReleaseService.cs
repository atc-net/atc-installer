// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Atc.Installer.Integration;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
public class GitHubReleaseService : IGitHubReleaseService
{
    private const string UserAgent = "Atc-Installer";
    private static readonly Uri ApiUri = new("https://api.github.com/repos/atc-net/atc-installer/releases/latest");

    public async Task<Version?> GetLatestVersion()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            var json = await client
                .GetStringAsync(ApiUri)
                .ConfigureAwait(false);

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var versionString = root.GetProperty("tag_name").ToString();

            if (versionString.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                versionString = versionString[1..];
            }

            return new Version(versionString);
        }
        catch
        {
            return null;
        }
    }

    public async Task<Uri?> GetLatestMsiLink()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            var json = await client
                .GetStringAsync(ApiUri)
                .ConfigureAwait(false);

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var assets = root.GetProperty("assets").EnumerateArray();
            foreach (var asset in assets)
            {
                if (asset.GetProperty("name").ToString().EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    return new Uri(asset.GetProperty("browser_download_url").ToString());
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}