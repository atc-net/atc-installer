namespace Atc.Installer.Wpf.ComponentProvider.WindowsApplication;

public class WindowsApplicationComponentProviderViewModel : ComponentProviderViewModel
{
    public WindowsApplicationComponentProviderViewModel(
        ApplicationOption applicationOption)
        : base(applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

        if (applicationOption.ComponentType == ComponentType.WindowsService)
        {
            IsWindowsService = true;
        }
    }

    public bool IsWindowsService { get; }

    public override void CheckPrerequisites()
    {
        base.CheckPrerequisites();

        ////if (!IsWindowsService)
        ////{
        ////    CheckPrerequisitesApplication();
        ////}
        ////else
        ////{
        ////    CheckPrerequisitesWindowsService();
        ////}
    }

    ////private void CheckPrerequisitesApplication()
    ////{
    ////    // TODO:
    ////}

    ////private void CheckPrerequisitesWindowsService()
    ////{
    ////    // TODO:
    ////}
}