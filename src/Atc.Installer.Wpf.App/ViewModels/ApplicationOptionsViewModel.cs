namespace Atc.Installer.Wpf.App.ViewModels;

public class ApplicationOptionsViewModel : ViewModelBase
{
    private string theme = string.Empty;
    private string title = string.Empty;

    public ApplicationOptionsViewModel()
    {
    }

    public ApplicationOptionsViewModel(
        ApplicationOptions applicationOptions)
    {
        ArgumentNullException.ThrowIfNull(applicationOptions);

        Title = applicationOptions.Title;
        Theme = applicationOptions.Theme;
    }

    public string Title
    {
        get => title;
        set
        {
            title = value;
            RaisePropertyChanged();
        }
    }

    public string Theme
    {
        get => theme;
        set
        {
            theme = value;
            RaisePropertyChanged();
        }
    }

    public override string ToString()
        => $"{nameof(Title)}: {Title}, {nameof(Theme)}: {Theme}";
}