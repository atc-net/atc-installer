namespace Atc.Installer.Wpf.App.ViewModels;

public class ApplicationOptionsViewModel : ViewModelBase
{
    private string theme = string.Empty;
    private string title = string.Empty;
    private bool enableEditingMode;
    private bool showOnlyBaseSettings;

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
        showOnlyBaseSettings = applicationOptions.ShowOnlyBaseSettings;
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
            if (enableEditingMode == value)
            {
                return;
            }

            enableEditingMode = value;
            IsDirty = true;
            RaisePropertyChanged();
        }
    }

    public bool ShowOnlyBaseSettings
    {
        get => showOnlyBaseSettings;
        set
        {
            if (showOnlyBaseSettings == value)
            {
                return;
            }

            showOnlyBaseSettings = value;
            IsDirty = true;
            RaisePropertyChanged();
        }
    }

    public override string ToString()
        => $"{nameof(Title)}: {Title}, {nameof(Theme)}: {Theme}, {nameof(EnableEditingMode)}: {EnableEditingMode}, {nameof(ShowOnlyBaseSettings)}: {ShowOnlyBaseSettings}, {nameof(IsDirty)}: {IsDirty}";
}