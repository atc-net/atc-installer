namespace Atc.Installer.Wpf.ComponentProvider.ValueConverters;

[ValueConversion(typeof(ComponentInstallationState), typeof(SolidColorBrush))]
public class ComponentInstallationStateToBrushValueConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is null)
        {
            return Brushes.DeepPink;
        }

        if (value is not ComponentInstallationState state)
        {
            throw new UnexpectedTypeException();
        }

        return state switch
        {
            ComponentInstallationState.Unknown => Brushes.Gray,
            ComponentInstallationState.Checking => Brushes.DarkViolet,
            ComponentInstallationState.NoInstallationsFiles => Brushes.Chocolate,
            ComponentInstallationState.NotInstalled => Brushes.DodgerBlue,
            ComponentInstallationState.Installing => Brushes.Crimson,
            ComponentInstallationState.InstalledWithOldVersion => Brushes.Gold,
            ComponentInstallationState.InstalledWithNewestVersion => Brushes.Green,
            _ => throw new SwitchCaseDefaultException(state),
        };
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}