namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer.Factories;

public static class InputFormDialogBoxFactory
{
    public static InputFormDialogBox CreateForNewX509Certificate()
    {
        var labelControls = LabelControlsFactory.CreateForNewX509Certificate();

        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        return new InputFormDialogBox(
            Application.Current.MainWindow!,
            "Create Self-Signed certificate",
            labelControlsForm);
    }

    public static InputFormDialogBox CreateForEditX509Certificate(
        IList<X509Certificate2> iisCertificates,
        X509Certificate2? componentCertificate)
    {
        var labelControls = LabelControlsFactory.CreateForEditX509Certificate(
            iisCertificates,
            componentCertificate);

        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        return new InputFormDialogBox(
            Application.Current.MainWindow!,
            "Change certificate",
            labelControlsForm);
    }
}