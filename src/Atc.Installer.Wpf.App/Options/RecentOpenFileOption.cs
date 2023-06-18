namespace Atc.Installer.Wpf.App.Options;

public class RecentOpenFileOption
{
    public DateTime TimeStamp { get; set; }

    public string File { get; set; } = string.Empty;

    public override string ToString()
        => $"{nameof(TimeStamp)}: {TimeStamp}, {nameof(File)}: {File}";
}