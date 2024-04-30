namespace Atc.Installer.Wpf.App.ViewModels;

public class AzureOptionsViewModel : ViewModelBase
{
    private string storageConnectionString = string.Empty;
    private string blobContainerName = string.Empty;

    public AzureOptionsViewModel()
    {
    }

    public AzureOptionsViewModel(
        AzureOptions azureOptions)
    {
        ArgumentNullException.ThrowIfNull(azureOptions);

        StorageConnectionString = azureOptions.StorageConnectionString;
        BlobContainerName = azureOptions.BlobContainerName;
    }

    public string StorageConnectionString
    {
        get => storageConnectionString;
        set
        {
            storageConnectionString = value;
            RaisePropertyChanged();
        }
    }

    public string BlobContainerName
    {
        get => blobContainerName;
        set
        {
            blobContainerName = value;
            RaisePropertyChanged();
        }
    }

    public string? AccountName
        => StorageConnectionString
            .Split(';')
            .FirstOrDefault(x => x.StartsWith("AccountName=", StringComparison.OrdinalIgnoreCase))
            ?.Split('=', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

    public override string ToString()
        => $"{nameof(StorageConnectionString)}: {StorageConnectionString}, {nameof(BlobContainerName)}: {BlobContainerName}";
}