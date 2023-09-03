global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Diagnostics.CodeAnalysis;
global using System.IO;
global using System.Reflection;
global using System.Security.Cryptography;
global using System.Text.Json;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Input;

global using Atc.Helpers;
global using Atc.Installer.Integration;
global using Atc.Installer.Integration.Azure;
global using Atc.Installer.Integration.ElasticSearch;
global using Atc.Installer.Integration.InstallationConfigurations;
global using Atc.Installer.Integration.InternetInformationServer;
global using Atc.Installer.Integration.PostgreSql;
global using Atc.Installer.Integration.WindowsApplication;
global using Atc.Installer.Wpf.App.Dialogs;
global using Atc.Installer.Wpf.App.Options;
global using Atc.Installer.Wpf.App.ViewModels;
global using Atc.Installer.Wpf.ComponentProvider;
global using Atc.Installer.Wpf.ComponentProvider.ElasticSearch;
global using Atc.Installer.Wpf.ComponentProvider.InternetInformationServer;
global using Atc.Installer.Wpf.ComponentProvider.Messages;
global using Atc.Installer.Wpf.ComponentProvider.PostgreSql;
global using Atc.Installer.Wpf.ComponentProvider.ViewModels;
global using Atc.Installer.Wpf.ComponentProvider.WindowsApplication;
global using Atc.Serialization;
global using Atc.Wpf.Collections;
global using Atc.Wpf.Command;
global using Atc.Wpf.Controls.Notifications;
global using Atc.Wpf.Controls.Notifications.Messages;
global using Atc.Wpf.Messaging;
global using Atc.Wpf.Mvvm;
global using Atc.Wpf.Theming.Controls.Windows;
global using Atc.Wpf.Theming.Themes.Dialogs;
global using ControlzEx.Theming;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Microsoft.Extensions.Options;
global using Microsoft.Win32;

global using Serilog;
global using Serilog.Events;
global using Serilog.Exceptions;