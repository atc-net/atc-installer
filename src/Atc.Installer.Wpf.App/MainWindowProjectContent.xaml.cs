namespace Atc.Installer.Wpf.App;

/// <summary>
/// Interaction logic for MainWindowProjectContent.
/// </summary>
public partial class MainWindowProjectContent
{
    public MainWindowProjectContent()
    {
        InitializeComponent();
    }

    private void OnFilterTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        var filterText = textBox.Text;
        foreach (var item in LbComponents.Items)
        {
            if (item is not ComponentProviderViewModel vm)
            {
                continue;
            }

            vm.SetFilterTextForMenu(filterText);
        }
    }
}