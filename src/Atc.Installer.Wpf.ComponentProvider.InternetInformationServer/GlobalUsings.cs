global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.IO;
global using System.Security.Cryptography.X509Certificates;
global using System.Text.Json;
global using System.Windows;
global using System.Windows.Data;
global using System.Xml;

global using Atc.Helpers;
global using Atc.Installer.Integration;
global using Atc.Installer.Integration.Helpers;
global using Atc.Installer.Integration.InstallationConfigurations;
global using Atc.Installer.Integration.InternetInformationServer;
global using Atc.Installer.Wpf.ComponentProvider.InternetInformationServer.Factories;
global using Atc.Installer.Wpf.ComponentProvider.InternetInformationServer.Helpers;
global using Atc.Installer.Wpf.ComponentProvider.Messages;
global using Atc.Installer.Wpf.ComponentProvider.ViewModels;
global using Atc.Serialization;
global using Atc.Wpf.Collections;
global using Atc.Wpf.Command;
global using Atc.Wpf.Controls.Dialogs;
global using Atc.Wpf.Controls.LabelControls;
global using Atc.Wpf.Controls.LabelControls.Abstractions;
global using Atc.Wpf.Controls.Notifications;
global using Atc.Wpf.Messaging;

global using Microsoft.Extensions.Logging;