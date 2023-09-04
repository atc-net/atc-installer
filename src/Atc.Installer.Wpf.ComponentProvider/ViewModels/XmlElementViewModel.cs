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

    public XmlElementViewModel(
        ComponentProviderViewModel refComponentProviderViewModel,
        XmlElementSettingsOptions xmlElementSettingsOptions)
    {
        Populate(refComponentProviderViewModel, xmlElementSettingsOptions);
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

    public ObservableCollectionEx<KeyValueTemplateItemViewModel> Attributes { get; set; } = new();

    private void Populate(
        XmlElementSettingsOptions xmlElementSettingsOptions)
    {
        ArgumentNullException.ThrowIfNull(xmlElementSettingsOptions);

        Path = xmlElementSettingsOptions.Path;
        Element = xmlElementSettingsOptions.Element;

        Attributes.Clear();

        Attributes.SuppressOnChangedNotification = true;

        foreach (var (attributeKey, attributeValue) in xmlElementSettingsOptions.Attributes)
        {
            Attributes.Add(
                new KeyValueTemplateItemViewModel(
                    attributeKey,
                    attributeValue,
                    template: null,
                    templateLocations: null));
        }

        Attributes.SuppressOnChangedNotification = false;
    }

    private void Populate(
        ComponentProviderViewModel refComponentProviderViewModel,
        XmlElementSettingsOptions xmlElementSettingsOptions)
    {
        ArgumentNullException.ThrowIfNull(refComponentProviderViewModel);
        ArgumentNullException.ThrowIfNull(xmlElementSettingsOptions);

        Path = xmlElementSettingsOptions.Path;
        Element = xmlElementSettingsOptions.Element;

        Attributes.Clear();

        Attributes.SuppressOnChangedNotification = true;

        foreach (var (attributeKey, attributeValue) in xmlElementSettingsOptions.Attributes)
        {
            if (attributeValue.ContainsTemplateKeyBrackets())
            {
                var (resolvedValue, templateLocations) = refComponentProviderViewModel.ResolveValueAndTemplateLocations(attributeValue);

                if (templateLocations.Count > 0)
                {
                    Attributes.Add(
                        new KeyValueTemplateItemViewModel(
                            attributeKey,
                            resolvedValue,
                            template: attributeValue,
                            templateLocations));
                }
            }
            else
            {
                Attributes.Add(
                    new KeyValueTemplateItemViewModel(
                        attributeKey,
                        attributeValue,
                        template: null,
                        templateLocations: null));
            }
        }

        Attributes.SuppressOnChangedNotification = false;
    }

    public override string ToString()
        => $"{nameof(Path)}: {Path}, {nameof(Element)}: {Element}, {nameof(Attributes)}.Count: {Attributes?.Count}";
}