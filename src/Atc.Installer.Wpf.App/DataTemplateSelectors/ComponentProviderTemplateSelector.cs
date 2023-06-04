namespace Atc.Installer.Wpf.App.DataTemplateSelectors;

public class ComponentProviderTemplateSelector : DataTemplateSelector
{
    public DataTemplate DefaultTemplate { get; set; } = new();

    public DataTemplate WindowsApplicationTemplate { get; set; } = new();

    public DataTemplate InternetInformationServerTemplate { get; set; } = new();

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
        => item switch
        {
            null => DefaultTemplate,
            WindowsApplicationComponentProviderViewModel => WindowsApplicationTemplate,
            InternetInformationServerComponentProviderViewModel => InternetInformationServerTemplate,
            _ => base.SelectTemplate(item, container)!,
        };
}