namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class RecentOpenFileViewModel : ViewModelBase
{
    public RecentOpenFileViewModel()
    {
    }

    public RecentOpenFileViewModel(
        DateTime timeStamp,
        string file)
    {
        ArgumentNullException.ThrowIfNull(file);

        TimeStamp = timeStamp;
        File = file;
    }

    private DateTime timeStamp;
    private string file = string.Empty;

    public DateTime TimeStamp
    {
        get => timeStamp;
        set
        {
            timeStamp = value;
            RaisePropertyChanged();
        }
    }

    public string File
    {
        get => file;
        set
        {
            file = value;
            RaisePropertyChanged();
        }
    }

    public override string ToString()
        => $"{nameof(TimeStamp)}: {TimeStamp}, {nameof(File)}: {File}";
}