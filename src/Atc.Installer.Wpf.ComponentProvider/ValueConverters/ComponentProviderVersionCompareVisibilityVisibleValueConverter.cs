namespace Atc.Installer.Wpf.ComponentProvider.ValueConverters;

[ValueConversion(typeof(ComponentProviderViewModel), typeof(Visibility))]
public class ComponentProviderVersionCompareVisibilityVisibleValueConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
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

        return vm is { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.DotNet7 }
            ? AnalyzeForJsVersion(vm)
            : AnalyzeForForDotNet(vm);
    }

    public object? ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) => null;

    private static Visibility AnalyzeForJsVersion(
        ComponentProviderViewModel vm)
    {
        if (vm.InstallationVersion == vm.InstalledVersion)
        {
            return Visibility.Collapsed;
        }

        var sortedSet = new SortedSet<string>(StringComparer.Ordinal)
        {
            vm.InstallationVersion!,
            vm.InstalledVersion!,
        };

        return vm.InstalledVersion == sortedSet.First()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static Visibility AnalyzeForForDotNet(
        ComponentProviderViewModel vm)
    {
        var sourceVersion = new Version(vm.InstallationVersion!);
        var destinationVersion = new Version(vm.InstalledVersion!);
        return sourceVersion.IsNewerThan(destinationVersion)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}