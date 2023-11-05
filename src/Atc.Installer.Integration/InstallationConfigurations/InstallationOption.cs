namespace Atc.Installer.Integration.InstallationConfigurations;

[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "OK.")]
public class InstallationOption
{
    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public AzureOptions Azure { get; set; } = new();

    public IDictionary<string, object> DefaultApplicationSettings { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    public IList<ApplicationOption> Applications { get; set; } = new List<ApplicationOption>();

    public void ClearDataForTemplateSettings()
    {
        Azure.StorageConnectionString = string.Empty;
        Azure.BlobContainerName = string.Empty;

        foreach (var key in DefaultApplicationSettings.Keys.ToList())
        {
            var value = DefaultApplicationSettings[key];
            DefaultApplicationSettings[key] = value switch
            {
                JsonElement jsonValue => jsonValue.ValueKind switch
                {
                    JsonValueKind.String => string.Empty,
                    JsonValueKind.Number => 0,
                    _ => DefaultApplicationSettings[key],
                },
                string => string.Empty,
                int => 0,
                bool => false,
                _ => DefaultApplicationSettings[key],
            };
        }
    }
}