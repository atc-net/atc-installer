namespace Atc.Installer.Wpf.ComponentProvider;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "OK.")]
public static class Constants
{
    public const string Default = "Default";

    public const string DefaultTemplateLocation = "DefaultApplicationSetting";

    public const string Current = "Current";

    public const string CurrentTemplateLocation = "ApplicationSetting";

    public static readonly string ItemBlankIdentifier = DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString();

    public static class WindowsAccounts
    {
        public const string IssUser = "IIS_IUSRS";
    }
}