namespace Atc.Installer.Wpf.App.ViewModels;

public class ApplicationOptionsViewModel : ViewModelBase
{
    private string theme = string.Empty;
    private string title = string.Empty;
    private bool enableEditingMode;

    public ApplicationOptionsViewModel()
    {
    }

    public ApplicationOptionsViewModel(
        ApplicationOptions applicationOptions)
    {
        ArgumentNullException.ThrowIfNull(applicationOptions);

        title = applicationOptions.Title;
        theme = applicationOptions.Theme;
        enableEditingMode = applicationOptions.EnableEditingMode;
    }

    public string Title
    {
        get => title;
        set
        {
            title = value;
            IsDirty = true;
            RaisePropertyChanged();
        }
    }

    public string Theme
    {
        get => theme;
        set
        {
            theme = value;
            IsDirty = true;
            RaisePropertyChanged();
        }
    }

    public bool EnableEditingMode
    {
        get => enableEditingMode;
        set
        {
            enableEditingMode = value;
            IsDirty = true;
            RaisePropertyChanged();
        }
    }

    public override string ToString()
        => $"{nameof(Title)}: {Title}, {nameof(Theme)}: {Theme}, {nameof(EnableEditingMode)}: {EnableEditingMode}, {nameof(IsDirty)}: {IsDirty}";
}