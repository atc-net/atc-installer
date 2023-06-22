// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class XmlElementViewModel : ViewModelBase
{
    private string path = string.Empty;
    private string element = string.Empty;

    public XmlElementViewModel()
    {
    }

    public XmlElementViewModel(
        XmlElementSettingsOptions xmlElementSettingsOptions)
    {
        Populate(xmlElementSettingsOptions);
    }

    public string Path
    {
        get => path;
        set
        {
            path = value;
            RaisePropertyChanged();
        }
    }

    public string Element
    {
        get => element;
        set
        {
            element = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollectionEx<KeyValueItemViewModel> Attributes { get; init; } = new();

    private void Populate(
        XmlElementSettingsOptions xmlElementSettingsOptions)
    {
        ArgumentNullException.ThrowIfNull(xmlElementSettingsOptions);

        Path = xmlElementSettingsOptions.Path;
        Element = xmlElementSettingsOptions.Element;

        Attributes.Clear();

        Attributes.SuppressOnChangedNotification = true;

        foreach (var attribute in xmlElementSettingsOptions.Attributes)
        {
            Attributes.Add(
                new KeyValueItemViewModel(
                    new KeyValuePair<string, object>(attribute.Key, attribute.Value)));
        }

        Attributes.SuppressOnChangedNotification = false;
    }

    public override string ToString()
        => $"{nameof(Path)}: {Path}, {nameof(Element)}: {Element}, {nameof(Attributes)}.Count: {Attributes?.Count}";
}