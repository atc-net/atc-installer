namespace Atc.Installer.Wpf.ComponentProvider.ValueConverters;

[ValueConversion(typeof(ComponentInstallationState), typeof(SolidColorBrush))]
public class ComponentRunningStateToBrushValueConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is null)
        {
            return Brushes.DeepPink;
        }

        if (value is not ComponentRunningState state)
        {
            throw new UnexpectedTypeException();
        }

        return state switch
        {
            ComponentRunningState.Unknown => Brushes.Gray,
            ComponentRunningState.Checking => Brushes.DarkViolet,
            ComponentRunningState.NotAvailable => Brushes.DimGray,
            ComponentRunningState.Stopped => Brushes.Crimson,
            ComponentRunningState.PartiallyRunning => Brushes.IndianRed,
            ComponentRunningState.Running => Brushes.Green,
            _ => throw new SwitchCaseDefaultException(state),
        };
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => ComponentRunningState.Unknown;
}