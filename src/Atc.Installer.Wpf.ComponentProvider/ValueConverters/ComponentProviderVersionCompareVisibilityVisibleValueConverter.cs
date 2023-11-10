namespace Atc.Installer.Wpf.ComponentProvider.ValueConverters;

[ValueConversion(typeof(ComponentProviderViewModel), typeof(Visibility))]
public class ComponentProviderVersionCompareVisibilityVisibleValueConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not ComponentProviderViewModel vm)
        {
            return Visibility.Collapsed;
        }

        if (vm.InstallationVersion is null ||
            vm.InstalledVersion is null)
        {
            return Visibility.Collapsed;
        }

        if (vm.HostingFramework is
            HostingFrameworkType.DonNetFramework48 or
            HostingFrameworkType.DotNet7 or
            HostingFrameworkType.DotNet8)
        {
            return AnalyzeVersion(vm);
        }

        return vm.ComponentType == ComponentType.InternetInformationService
            ? AnalyzeVersion(vm)
            : Visibility.Collapsed;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) => null;

    private static Visibility AnalyzeVersion(
        ComponentProviderViewModel vm)
        => VersionHelper.IsSourceNewerThanDestination(vm.InstallationVersion, vm.InstalledVersion)
            ? Visibility.Visible
            : Visibility.Collapsed;
}