namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer.Factories;

public static class LabelControlsFactory
{
    public static IList<ILabelControlBase> CreateForNewX509Certificate()
    {
        var labelControls = new List<ILabelControlBase>();

        var labelTextBoxFriendly = new LabelTextBox
        {
            LabelText = "Friendly",
            IsMandatory = true,
        };

        labelControls.Add(labelTextBoxFriendly);

        var labelTextBoxSubject = new LabelTextBox
        {
            LabelText = "Subject",
            IsMandatory = true,
        };

        labelControls.Add(labelTextBoxSubject);

        var labelTextBoxDns = new LabelTextBox
        {
            LabelText = "DNS",
            IsMandatory = true,
        };

        labelControls.Add(labelTextBoxDns);

        var labelTextBoxPassword = new LabelTextBox
        {
            LabelText = "Password",
            IsMandatory = true,
        };

        labelControls.Add(labelTextBoxPassword);

        var labelIntegerBoxDays = new LabelIntegerBox
        {
            LabelText = "Years",
            IsMandatory = true,
            Value = 10,
        };

        labelControls.Add(labelIntegerBoxDays);

        return labelControls;
    }

    public static IList<ILabelControlBase> CreateForEditX509Certificate(
        IList<X509Certificate2> iisCertificates,
        X509Certificate2? componentCertificate)
    {
        ArgumentNullException.ThrowIfNull(iisCertificates);

        var labelControls = new List<ILabelControlBase>();

        var labelComboBox = new LabelComboBox
        {
            LabelText = "Certificate",
            IsMandatory = true,
            Items = new Dictionary<string, string>(StringComparer.Ordinal),
        };

        foreach (var certificate in iisCertificates)
        {
            labelComboBox.Items.Add(
                certificate.Thumbprint,
                certificate.GetNameIdentifier());
        }

        if (componentCertificate is not null)
        {
            labelComboBox.Items.Add(
                "#Remove#",
                "- Remove certificate -");

            labelComboBox.SelectedKey = componentCertificate.Thumbprint;
        }

        labelControls.Add(labelComboBox);

        return labelControls;
    }
}