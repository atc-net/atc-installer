// ReSharper disable ParameterTypeCanBeEnumerable.Local
namespace Atc.Installer.Wpf.App.Helpers;

[SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
public static class ExcelHelper
{
    public static void CreateAndSave(
        FileInfo exportToFile,
        IEnumerable<ReportingData> reportingDataForComponentProviders)
    {
        ArgumentNullException.ThrowIfNull(exportToFile);

        var dataForComponentProviders = reportingDataForComponentProviders
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .ToArray();

        var distinctFileShortNames = new List<string>();
        foreach (var dataForComponentProvider in dataForComponentProviders.Where(x => x.InstalledMainFilePath is not null))
        {
            var mainFileInfo = new FileInfo(dataForComponentProvider.InstalledMainFilePath!);
            var mainFileDirectoryInfo = $"{mainFileInfo.Directory!.FullName}\\";

            foreach (var reportingFile in dataForComponentProvider.Files)
            {
                var shortName = reportingFile.FullName.Replace(mainFileDirectoryInfo, string.Empty, StringComparison.Ordinal);
                if (!distinctFileShortNames.Contains(shortName, StringComparer.OrdinalIgnoreCase))
                {
                    distinctFileShortNames.Add(shortName);
                }
            }
        }

        using var workbook = new XLWorkbook();
        CreateAndAddWorksheetOverview(workbook, dataForComponentProviders);
        CreateAndAddWorksheetFiles(workbook, dataForComponentProviders, distinctFileShortNames);
        workbook.SaveAs(exportToFile.FullName);
    }

    private static void CreateAndAddWorksheetOverview(
        IXLWorkbook workbook,
        IReadOnlyCollection<ReportingData> dataForComponentProviders)
    {
        var wsOverview = workbook.Worksheets.Add("Overview");

        var colNr = 1;
        wsOverview.Cell(1, colNr).Value = "System";
        colNr++;
        wsOverview.Cell(1, colNr).Value = "Version";
        colNr++;
        wsOverview.Cell(1, colNr).Value = "MainFile";

        var rowNr = 1;
        foreach (var dataForComponentProvider in dataForComponentProviders)
        {
            rowNr++;
            wsOverview.Cell(rowNr, 1).Value = dataForComponentProvider.Name;
            wsOverview.Cell(rowNr, 2).Value = dataForComponentProvider.Version;
            wsOverview.Cell(rowNr, 3).Value = dataForComponentProvider.InstalledMainFilePath;
        }

        var totalRowNr = rowNr;
        var totalColNr = colNr;

        // Header row
        wsOverview.SheetView.Freeze(1, totalColNr);

        wsOverview.Range(1, 1, 1, totalColNr)
            .Style
            .Font.SetBold()
            .Fill.SetBackgroundColor(XLColor.LightBlue)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        wsOverview.Columns().AdjustToContents();
        wsOverview.Range(1, 1, 1, totalColNr).SetAutoFilter();

        // Content - 1 column
        wsOverview.Range(2, 1, totalRowNr, 1)
            .Style
            .Fill.SetBackgroundColor(XLColor.LightBlue);
    }

    private static void CreateAndAddWorksheetFiles(
        IXLWorkbook workbook,
        IReadOnlyCollection<ReportingData> dataForComponentProviders,
        IReadOnlyCollection<string> distinctFileShortNames)
    {
        var wsFiles = workbook.Worksheets.Add("Files");

        var colNr = 1;
        wsFiles.Cell(1, colNr).Value = "File";
        colNr++;

        foreach (var dataForComponentProvider in dataForComponentProviders)
        {
            wsFiles.Cell(1, colNr).Value = dataForComponentProvider.Name;
            colNr++;
        }

        var rowNr = 2;
        foreach (var fileShortName in distinctFileShortNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            colNr = 1;
            wsFiles.Cell(rowNr, colNr).Value = fileShortName;
            colNr++;

            foreach (var dataForComponentProvider in dataForComponentProviders)
            {
                var reportingFile = dataForComponentProvider.Files.FirstOrDefault(x =>
                    x.FullName.EndsWith(fileShortName, StringComparison.OrdinalIgnoreCase));
                if (reportingFile is null)
                {
                    wsFiles.Cell(rowNr, colNr).Value = "-";
                }
                else
                {
                    if (reportingFile.Version is null)
                    {
                        wsFiles.Cell(rowNr, colNr).Value = "Unknown";
                    }
                    else
                    {
                        wsFiles.Cell(rowNr, colNr).Value = reportingFile.IsDebugBuild
                            ? $"{reportingFile.Version} - Debug"
                            : reportingFile.Version;
                    }
                }

                colNr++;
            }

            rowNr++;
        }

        // Header row
        wsFiles.SheetView.Freeze(1, dataForComponentProviders.Count + 1);

        wsFiles.Range(1, 1, 1, dataForComponentProviders.Count + 1)
            .Style
            .Font.SetBold()
            .Fill.SetBackgroundColor(XLColor.LightBlue)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        wsFiles.Columns().AdjustToContents();
        wsFiles.Range(1, 1, 1, dataForComponentProviders.Count + 1).SetAutoFilter();

        // Content - 1 column
        wsFiles.Range(2, 1, distinctFileShortNames.Count + 1, 1)
            .Style
            .Fill.SetBackgroundColor(XLColor.LightBlue);

        // Content - rest
        wsFiles.Range(2, 2, distinctFileShortNames.Count + 1, dataForComponentProviders.Count + 1)
            .Style
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }
}