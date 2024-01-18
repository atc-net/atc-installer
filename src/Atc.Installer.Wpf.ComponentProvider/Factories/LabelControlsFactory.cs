namespace Atc.Installer.Wpf.ComponentProvider.Factories;

[SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
public static class LabelControlsFactory
{
    public static IList<ILabelControlBase> CreateForApplicationSettings(
        ApplicationSettingsViewModel? defaultSettings,
        KeyValueTemplateItemViewModel? updateItem)
    {
        var labelControls = new List<ILabelControlBase>();

        var labelTextBoxKey = new LabelTextBox
        {
            LabelText = "Key",
            IsMandatory = true,
            MinLength = 1,
        };

        if (updateItem is not null)
        {
            labelTextBoxKey.IsMandatory = false;
            labelTextBoxKey.IsEnabled = false;
            labelTextBoxKey.Text = updateItem.Key;
        }

        labelControls.Add(labelTextBoxKey);

        if (defaultSettings is null)
        {
            var labelTextBoxValue = new LabelTextBox
            {
                LabelText = "Value",
                IsMandatory = false,
            };

            if (updateItem is not null)
            {
                labelTextBoxValue.Text = string.IsNullOrEmpty(updateItem.Template)
                    ? updateItem.Value.ToString()!
                    : updateItem.Template;
            }

            labelControls.Add(labelTextBoxValue);
        }
        else
        {
            var labelTextBoxValue = new LabelTextBox
            {
                LabelText = "Value",
                IsMandatory = false,
            };

            var labelComboBox = new LabelComboBox
            {
                LabelText = "Templates",
                IsMandatory = false,
                Items = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    {
                        DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString(),
                        string.Empty
                    },
                },
            };

            foreach (var keyValueTemplateItem in defaultSettings.Items)
            {
                labelComboBox.Items.Add(
                    keyValueTemplateItem.Key,
                    $"[[{keyValueTemplateItem.Key}]]   -   {keyValueTemplateItem.GetValueAsString()}");
            }

            if (updateItem is not null)
            {
                if (string.IsNullOrEmpty(updateItem.Template))
                {
                    labelTextBoxValue.Text = updateItem.Value.ToString()!;
                }
                else
                {
                    labelComboBox.SelectedKey = updateItem.Template.GetTemplateKeys(TemplatePatternType.DoubleHardBrackets)[0];
                }
            }

            labelControls.Add(labelTextBoxValue);
            labelControls.Add(labelComboBox);

            labelTextBoxValue.TextChanged += (_, args) =>
            {
                if (args.NewValue is not null &&
                    args.OldValue != args.NewValue)
                {
                    labelComboBox.SelectedKey = labelComboBox.Items.First().Key;
                }
            };

            labelComboBox.SelectorChanged += (_, args) =>
            {
                if (args.NewValue is not null &&
                    args.NewValue != DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString())
                {
                    labelTextBoxValue.Text = string.Empty;
                }
            };
        }

        return labelControls;
    }

    [SuppressMessage("Security", "MA0009:Add regex evaluation timeout", Justification = "OK - For now.")]
    [SuppressMessage("Performance", "MA0110:Use the Regex source generator", Justification = "OK - For now.")]
    [SuppressMessage("Performance", "SYSLIB1045:Convert to 'GeneratedRegexAttribute'.", Justification = "OK - For now.")]
    public static IList<ILabelControlBase> CreateForConfigurationSettingsFiles(
        ComponentProviderViewModel? refComponentProvider,
        KeyValueTemplateItemViewModel? updateItem)
    {
        var labelControls = new List<ILabelControlBase>();

        var labelTextBoxKey = new LabelTextBox
        {
            LabelText = "Key",
            IsMandatory = true,
            MinLength = 1,
        };

        var valueLabelRegexPattern = string.Empty;
        var sbValueLabelText = new StringBuilder();
        sbValueLabelText.Append("Value");

        if (updateItem is not null)
        {
            labelTextBoxKey.IsMandatory = false;
            labelTextBoxKey.IsEnabled = false;
            labelTextBoxKey.Text = updateItem.Key;

            if (ConfigurationKeyToUnitTypeValueConverter.TryParse(updateItem.Key, out var unitType))
            {
                sbValueLabelText.Append(" in ");
                sbValueLabelText.Append(unitType.ToLower(GlobalizationConstants.EnglishCultureInfo));
                valueLabelRegexPattern = ConfigurationKeyToUnitTypeValueConverter.GetRegexPatternFromUnitType(unitType);
            }
        }

        labelControls.Add(labelTextBoxKey);

        var labelTextBoxValue = new LabelTextBox
        {
            Tag = "Value",
            LabelText = sbValueLabelText.ToString(),
            IsMandatory = false,
        };

        if (!string.IsNullOrEmpty(valueLabelRegexPattern))
        {
            labelTextBoxValue.RegexPattern = valueLabelRegexPattern;
        }

        if (updateItem is not null &&
            string.IsNullOrEmpty(updateItem.Template))
        {
            var value = updateItem.Value.ToString()!;
            if (string.IsNullOrEmpty(valueLabelRegexPattern) &&
                Regex.IsMatch(value, RegexPatternConstants.Boolean.Strict, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
            {
                labelTextBoxValue.ValidationText = "True/False";
                labelTextBoxValue.RegexPattern = RegexPatternConstants.Boolean.Optional;
            }

            labelTextBoxValue.Text = value;
        }

        labelControls.Add(labelTextBoxValue);

        if (refComponentProvider is null)
        {
            return labelControls;
        }

        var labelComboBox = new LabelComboBox
        {
            LabelText = "Templates",
            IsMandatory = false,
            Items = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                {
                    DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString(),
                    string.Empty
                },
            },
        };

        foreach (var keyValueTemplateItem in refComponentProvider.DefaultApplicationSettings.Items)
        {
            labelComboBox.Items.Add(
                $"Default|{keyValueTemplateItem.Key}",
                $"Default   -   [[{keyValueTemplateItem.Key}]]   -   {keyValueTemplateItem.GetValueAsString()}");
        }

        foreach (var keyValueTemplateItem in refComponentProvider.ApplicationSettings.Items)
        {
            labelComboBox.Items.Add(
                $"Current|{keyValueTemplateItem.Key}",
                $"Current   -   [[{keyValueTemplateItem.Key}]]   -   {keyValueTemplateItem.GetValueAsString()}");
        }

        if (updateItem is not null &&
            !string.IsNullOrEmpty(updateItem.Template))
        {
            var templateKey = updateItem.Template.GetTemplateKeys(TemplatePatternType.DoubleHardBrackets)[0];
            labelComboBox.SelectedKey =
                labelComboBox.Items.Keys.First(x => x.EndsWith($"|{templateKey}", StringComparison.Ordinal));
        }

        labelControls.Add(labelComboBox);

        labelTextBoxValue.TextChanged += (_, args) =>
        {
            if (args.NewValue is not null &&
                args.OldValue != args.NewValue)
            {
                labelComboBox.SelectedKey = labelComboBox.Items.First().Key;
            }
        };

        labelComboBox.SelectorChanged += (_, args) =>
        {
            if (args.NewValue is not null &&
                args.NewValue != DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString())
            {
                labelTextBoxValue.Text = string.Empty;
            }
        };

        return labelControls;
    }

    public static IList<ILabelControlBase> CreateForFirewallRules(
        ComponentProviderViewModel? refComponentProvider,
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

    public static IList<ILabelControlBase> CreateForFolderPermissions(
        ComponentProviderViewModel refComponentProvider)
    {
        ArgumentNullException.ThrowIfNull(refComponentProvider);

        var labelControls = new List<ILabelControlBase>();

        var labelComboBoxUser = new LabelComboBox
        {
            LabelText = "User",
            IsMandatory = true,
            Items = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                {
                    DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString(),
                    string.Empty
                },
                {
                    Constants.WindowsAccounts.IssUser,
                    Constants.WindowsAccounts.IssUser
                },
            },
        };

        labelControls.Add(labelComboBoxUser);

        var labelComboAccessRights = new LabelComboBox
        {
            LabelText = "Access Rights",
            IsMandatory = true,
            Items = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                {
                    DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString(),
                    string.Empty
                },
                {
                    nameof(FileSystemRights.Read),
                    "Read"
                },
                {
                    nameof(FileSystemRights.Write),
                    "Write"
                },
                {
                    nameof(FileSystemRights.ReadAndExecute),
                    "Read and Execute"
                },
                {
                    nameof(FileSystemRights.Modify),
                    "Modify"
                },
            },
        };

        labelControls.Add(labelComboAccessRights);

        var labelTextBoxFolder = new LabelTextBox
        {
            LabelText = "Folder",
            IsMandatory = false,
        };

        labelControls.Add(labelTextBoxFolder);

        var labelComboBoxFolderTemplate = new LabelComboBox
        {
            LabelText = "Templates",
            IsMandatory = false,
            Items = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                {
                    DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString(),
                    string.Empty
                },
            },
        };

        foreach (var keyValueTemplateItem in refComponentProvider.DefaultApplicationSettings.Items)
        {
            var value = keyValueTemplateItem.GetValueAsString();
            if (!value.StartsWith(".\\", StringComparison.Ordinal) &&
                !value.Contains(":\\", StringComparison.Ordinal))
            {
                continue;
            }

            labelComboBoxFolderTemplate.Items.Add(
                $"Default|{keyValueTemplateItem.Key}",
                $"Default   -   [[{keyValueTemplateItem.Key}]]   -   {value}");
        }

        foreach (var keyValueTemplateItem in refComponentProvider.ApplicationSettings.Items)
        {
            var value = keyValueTemplateItem.GetValueAsString();
            if (!value.StartsWith(".\\", StringComparison.Ordinal) &&
                !value.Contains(":\\", StringComparison.Ordinal))
            {
                continue;
            }

            labelComboBoxFolderTemplate.Items.Add(
                $"Current|{keyValueTemplateItem.Key}",
                $"Current   -   [[{keyValueTemplateItem.Key}]]   -   {value}");
        }

        if (labelComboBoxFolderTemplate.Items.Count > 1)
        {
            labelControls.Add(labelComboBoxFolderTemplate);
        }

        return labelControls;
    }
}