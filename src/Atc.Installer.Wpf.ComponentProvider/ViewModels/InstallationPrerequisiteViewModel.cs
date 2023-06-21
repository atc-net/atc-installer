namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class InstallationPrerequisiteViewModel : ViewModelBase
{
    public InstallationPrerequisiteViewModel(
        string key,
        LogCategoryType categoryType,
        string message)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        CategoryType = categoryType;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public string Key { get; private init; }

    public LogCategoryType CategoryType { get; private init; }

    public string Message { get; private init; }

    public override string ToString()
        => $"{nameof(Key)}: {Key}, {nameof(CategoryType)}: {CategoryType}, {nameof(Message)}: {Message}";
}