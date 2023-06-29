namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class RecentOpenFileViewModel : ViewModelBase
{
    private readonly DirectoryInfo installerProgramDataProjectsDirectory;

    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
    public RecentOpenFileViewModel()
    {
        installerProgramDataProjectsDirectory = new DirectoryInfo(@"C:\");
    }

    public RecentOpenFileViewModel(
        DirectoryInfo installerProgramDataProjectsDirectory,
        DateTime timeStamp,
        string file)
    {
        ArgumentNullException.ThrowIfNull(installerProgramDataProjectsDirectory);
        ArgumentNullException.ThrowIfNull(file);

        this.installerProgramDataProjectsDirectory = installerProgramDataProjectsDirectory;
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

    public string FileDisplay
    {
        get
        {
            if (!file.StartsWith(installerProgramDataProjectsDirectory.FullName, StringComparison.Ordinal))
            {
                return file;
            }

            var fileInfo = new FileInfo(file);
            return $"Project: {fileInfo.Directory!.Name} - {fileInfo.Name}";
        }
    }

    public override string ToString()
        => $"{nameof(TimeStamp)}: {TimeStamp}, {nameof(File)}: {File}, {nameof(FileDisplay)}: {FileDisplay}";
}