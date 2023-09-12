namespace Atc.Installer.Wpf.ComponentProvider.Controls;

[SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "OK.")]
[SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
public class FirewallRulesViewModel : ViewModelBase
{
    private ComponentProviderViewModel? refComponentProvider;
    private bool enableEditingMode;

    public FirewallRulesViewModel()
        => Messenger.Default.Register<UpdateApplicationOptionsMessage>(this, HandleUpdateApplicationOptionsMessage);

    public IRelayCommand NewCommand
        => new RelayCommand(
            NewCommandHandler);

    public IRelayCommand<FirewallRuleViewModel> EditCommand
        => new RelayCommand<FirewallRuleViewModel>(
            EditCommandHandler);

    public IRelayCommand<FirewallRuleViewModel> DeleteCommand
        => new RelayCommand<FirewallRuleViewModel>(
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

    public ObservableCollectionEx<FirewallRuleViewModel> Items { get; init; } = new();

    public void Populate(
        ComponentProviderViewModel refComponentProviderViewModel,
        IList<FirewallRuleOption> firewallRules)
    {
        ArgumentNullException.ThrowIfNull(refComponentProviderViewModel);
        ArgumentNullException.ThrowIfNull(firewallRules);

        refComponentProvider = refComponentProviderViewModel;

        Items.Clear();

        Items.SuppressOnChangedNotification = true;

        foreach (var firewallRule in firewallRules)
        {
            Items.Add(new FirewallRuleViewModel(firewallRule));
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
        var labelControls = CreateLabelControls(updateItem: null);
        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        var dialogBox = new InputFormDialogBox(
            Application.Current.MainWindow!,
            "New firewall rule",
            labelControlsForm);

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        var data = dialogBox.Data.GetKeyValues();

        var dataKey = data["Name"].ToString()!;
        var dataValue = (int)data["Port"];
        var dataDirectionStr = data["Direction"].ToString()!;
        var dataProtocolStr = data["Protocol"].ToString()!;

        if (string.IsNullOrEmpty(dataDirectionStr) ||
            dataDirectionStr.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal) ||
            string.IsNullOrEmpty(dataProtocolStr) ||
            dataProtocolStr.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal) ||
            Items.Any(x => x.Name.Equals(dataKey, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        Items.Add(
            new FirewallRuleViewModel
            {
                Name = dataKey,
                Port = dataValue,
                Direction = Enum<FirewallDirectionType>.Parse(dataDirectionStr),
                Protocol = Enum<FirewallProtocolType>.Parse(dataProtocolStr),
            });

        IsDirty = true;
    }

    private void EditCommandHandler(
        FirewallRuleViewModel item)
    {
        var updateItem = Items.First(x => x.Name.Equals(item.Name, StringComparison.Ordinal));

        var labelControls = CreateLabelControls(updateItem);
        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        var dialogBox = new InputFormDialogBox(
            Application.Current.MainWindow!,
            "Edit firewall rule",
            labelControlsForm);

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        HandleEditDialogResult(dialogBox, updateItem);
    }

    private void DeleteCommandHandler(
        FirewallRuleViewModel item)
    {
        var dialogBox = new QuestionDialogBox(
            Application.Current.MainWindow!,
            "Delete firewall rule",
            $"Are you sure you want to delete:\n\n{item.Name}")
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

    private List<ILabelControlBase> CreateLabelControls(
        FirewallRuleViewModel? updateItem)
    {
        var labelControls = new List<ILabelControlBase>();

        var labelTextBoxName = new LabelTextBox
        {
            LabelText = "Name",
            IsMandatory = true,
            MinLength = 1,
        };

        if (updateItem is not null)
        {
            labelTextBoxName.IsMandatory = false;
            labelTextBoxName.IsEnabled = false;
            labelTextBoxName.Text = updateItem.Name;
        }
        else if (refComponentProvider is not null)
        {
            labelTextBoxName.Text = refComponentProvider.Name;
        }

        labelControls.Add(labelTextBoxName);

        var labelIntegerPort = new LabelIntegerBox
        {
            LabelText = "Port",
            IsMandatory = true,
            Minimum = 0,
        };

        if (updateItem is not null)
        {
            labelIntegerPort.Value = updateItem.Port;
        }
        else if (refComponentProvider is not null)
        {
            if (refComponentProvider.ApplicationSettings.TryGetUshort("HttpsPort", out var httpsPort))
            {
                labelIntegerPort.Value = httpsPort;
            }
            else if (refComponentProvider.ApplicationSettings.TryGetUshort("HttpPort", out var httpPort))
            {
                labelIntegerPort.Value = httpPort;
            }
        }

        labelControls.Add(labelIntegerPort);

        var labelComboBoxDirection = new LabelComboBox
        {
            LabelText = "Direction",
            IsMandatory = true,
            Items = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                {
                    DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString(),
                    string.Empty
                },
            },
        };

        foreach (var item in Enum<FirewallDirectionType>.ToDictionaryWithStringKey())
        {
            labelComboBoxDirection.Items.Add(item.Key, item.Value);
        }

        if (updateItem is not null)
        {
            labelComboBoxDirection.SelectedKey = updateItem.Direction.ToString();
        }
        else if (refComponentProvider is not null)
        {
            labelComboBoxDirection.SelectedKey = nameof(FirewallDirectionType.Inbound);
        }

        labelControls.Add(labelComboBoxDirection);

        var labelComboBoxProtocol = new LabelComboBox
        {
            LabelText = "Protocol",
            IsMandatory = true,
            Items = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                {
                    DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString(),
                    string.Empty
                },
            },
        };

        foreach (var item in Enum<FirewallProtocolType>.ToDictionaryWithStringKey())
        {
            labelComboBoxProtocol.Items.Add(item.Key, item.Value);
        }

        if (updateItem is not null)
        {
            labelComboBoxProtocol.SelectedKey = updateItem.Protocol.ToString();
        }
        else if (refComponentProvider is not null)
        {
            labelComboBoxProtocol.SelectedKey = nameof(FirewallProtocolType.Tcp);
        }

        labelControls.Add(labelComboBoxProtocol);

        return labelControls;
    }

    private void HandleEditDialogResult(
        InputFormDialogBox dialogBox,
        FirewallRuleViewModel updateItem)
    {
        var data = dialogBox.Data.GetKeyValues();

        var dataDirectionStr = data["Direction"].ToString()!;
        var dataProtocolStr = data["Protocol"].ToString()!;

        if (string.IsNullOrEmpty(dataDirectionStr) ||
            dataDirectionStr.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal) ||
            string.IsNullOrEmpty(dataProtocolStr) ||
            dataProtocolStr.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal))
        {
            return;
        }

        updateItem.Port = (int)data["Port"];
        updateItem.Direction = Enum<FirewallDirectionType>.Parse(dataDirectionStr);
        updateItem.Protocol = Enum<FirewallProtocolType>.Parse(dataProtocolStr);

        IsDirty = true;
    }
}