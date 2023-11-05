// ReSharper disable StringLiteralTypo
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedVariable
namespace Atc.Installer.Wpf.ComponentProvider.Controls;

[SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "OK.")]
[SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
[SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "OK.")]
[SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = "OK.")]
public class FolderPermissionsViewModel : ViewModelBase
{
    private ComponentProviderViewModel? refComponentProvider;
    private bool enableEditingMode;

    public FolderPermissionsViewModel()
        => Messenger.Default.Register<UpdateApplicationOptionsMessage>(this, HandleUpdateApplicationOptionsMessage);

    public IRelayCommand NewCommand
        => new RelayCommand(
            NewCommandHandler);

    public IRelayCommand<FolderPermissionViewModel> DeleteCommand
        => new RelayCommand<FolderPermissionViewModel>(
            DeleteCommandHandler);

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
            RaisePropertyChanged();
        }
    }

    public ObservableCollectionEx<FolderPermissionViewModel> Items { get; init; } = new();

    public void Populate(
        ComponentProviderViewModel refComponentProviderViewModel,
        IList<FolderPermissionOption> folderPermissions)
    {
        ArgumentNullException.ThrowIfNull(refComponentProviderViewModel);
        ArgumentNullException.ThrowIfNull(folderPermissions);

        refComponentProvider = refComponentProviderViewModel;

        Items.Clear();

        Items.SuppressOnChangedNotification = true;

        foreach (var folderPermission in folderPermissions)
        {
            Items.Add(new FolderPermissionViewModel(folderPermission));
        }

        Items.SuppressOnChangedNotification = false;
    }

    public void ClearAllIsDirty()
    {
        IsDirty = false;
        foreach (var item in Items)
        {
            item.IsDirty = false;
        }
    }

    private void HandleUpdateApplicationOptionsMessage(
        UpdateApplicationOptionsMessage obj)
        => EnableEditingMode = obj.EnableEditingMode;

    private void NewCommandHandler()
    {
        ArgumentNullException.ThrowIfNull(refComponentProvider);

        var dialogBox = InputFormDialogBoxFactory.CreateForNewFolderPermissions(refComponentProvider);

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        var data = dialogBox.Data.GetKeyValues();

        var dataUser = data["User"].ToString()!;
        var dataAccessRightsStr = data["AccessRights"].ToString()!;

        var dataFolder = data["Folder"].ToString()!;
        var dataTemplate = string.Empty;
        if (data.TryGetValue("Templates", out var value))
        {
            dataTemplate = value?.ToString()!;
        }

        if (string.IsNullOrEmpty(dataFolder) &&
            (string.IsNullOrEmpty(dataTemplate) ||
             dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal)))
        {
            return;
        }

        if (string.IsNullOrEmpty(dataTemplate) ||
            dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal))
        {
            if (!dataFolder.StartsWith(".\\", StringComparison.Ordinal) &&
               !dataFolder.Contains(":\\", StringComparison.Ordinal))
            {
                dataFolder = $".\\{dataFolder}";
            }

            Items.Add(
                new FolderPermissionViewModel
                {
                    User = dataUser,
                    FileSystemRights = Enum<FileSystemRights>.Parse(dataAccessRightsStr),
                    Folder = dataFolder,
                    Directory = new DirectoryInfo(dataFolder),
                });
        }
        else
        {
            (dataTemplate, var templateLocation, var dataDefaultValue) = TemplateExtract(dataTemplate);

            Items.Add(
                new FolderPermissionViewModel
                {
                    User = dataUser,
                    FileSystemRights = Enum<FileSystemRights>.Parse(dataAccessRightsStr),
                    Folder = $"[[{dataTemplate}]]",
                    Directory = new DirectoryInfo(dataDefaultValue),
                });
        }

        IsDirty = true;
    }

    private void DeleteCommandHandler(
        FolderPermissionViewModel item)
    {
        var dialogBox = new QuestionDialogBox(
            Application.Current.MainWindow!,
            "Delete folder permission",
            $"Are you sure you want to delete:\n\n{item.User} -> {item.FileSystemRights} -> {item.Folder}")
        {
            Width = 500,
        };

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        Items.Remove(item);

        IsDirty = true;
    }

    private (string DataTemplate, string TemplateLocation, string DataDefaultValue) TemplateExtract(
        string dataTemplate)
    {
        var templateLocation = string.Empty;
        var dataDefaultValue = string.Empty;

        if (refComponentProvider is null)
        {
            return (dataTemplate, templateLocation, dataDefaultValue);
        }

        var sa = dataTemplate.Split('|');
        dataTemplate = sa[^1];

        if (sa[0].Equals(Constants.Default, StringComparison.Ordinal))
        {
            templateLocation = Constants.DefaultTemplateLocation;
            dataDefaultValue = refComponentProvider.DefaultApplicationSettings.Items
                .First(x => x.Key.Equals(dataTemplate, StringComparison.Ordinal))
                .GetValueAsString();
        }
        else if (sa[0].Equals(Constants.Current, StringComparison.Ordinal))
        {
            templateLocation = Constants.CurrentTemplateLocation;
            dataDefaultValue = refComponentProvider.ApplicationSettings.Items
                .First(x => x.Key.Equals(dataTemplate, StringComparison.Ordinal))
                .GetValueAsString();
        }

        return (dataTemplate, templateLocation, dataDefaultValue);
    }
}