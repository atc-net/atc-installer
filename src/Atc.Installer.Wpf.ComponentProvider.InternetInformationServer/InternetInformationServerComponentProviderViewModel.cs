namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer;

public class InternetInformationServerComponentProviderViewModel : ComponentProviderViewModel
{
    public InternetInformationServerComponentProviderViewModel(
        ApplicationOption applicationOption)
        : base(applicationOption)
    {
        if (InstalledMainFile is not null &&
            InstalledMainFile.StartsWith(@".\", StringComparison.Ordinal))
        {
            InstalledMainFile = InstalledMainFile.Replace(@".\", @"C:\inetpub\wwwroot\", StringComparison.Ordinal);
        }
    }
}