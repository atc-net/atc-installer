namespace Atc.Installer.Wpf.App.DataTemplateSelectors;

public class ComponentProviderTemplateSelector : DataTemplateSelector
{
    public DataTemplate DefaultTemplate { get; set; } = new();

    public DataTemplate InternetInformationServerTemplate { get; set; } = new();

    public DataTemplate PostgreSqlServerTemplate { get; set; } = new();

    public DataTemplate WindowsApplicationTemplate { get; set; } = new();

    public override DataTemplate SelectTemplate(
        object item,
        DependencyObject container)
        => item switch
        {
            null => DefaultTemplate,
            InternetInformationServerComponentProviderViewModel => InternetInformationServerTemplate,
            PostgreSqlServerComponentProviderViewModel => PostgreSqlServerTemplate,
            WindowsApplicationComponentProviderViewModel => WindowsApplicationTemplate,
            _ => base.SelectTemplate(item, container)!,
        };
}