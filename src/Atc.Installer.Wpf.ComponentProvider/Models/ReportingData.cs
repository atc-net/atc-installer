namespace Atc.Installer.Wpf.ComponentProvider.Models;

public class ReportingData
{
    public ReportingData(
        string name)
    {
        Name = name;
        Files = new List<ReportingFile>();
    }

    public string Name { get; }

    public string? InstalledMainFilePath { get; set; }

    public string? Version { get; set; }

    public IList<ReportingFile> Files { get; set; }
}