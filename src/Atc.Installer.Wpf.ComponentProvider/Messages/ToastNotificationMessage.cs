namespace Atc.Installer.Wpf.ComponentProvider.Messages;

public class ToastNotificationMessage
{
    public ToastNotificationMessage(
        ToastNotificationType toastNotificationType,
        string title,
        string message)
    {
        ToastNotificationType = toastNotificationType;
        Title = title;
        Message = message;
    }

    public ToastNotificationType ToastNotificationType { get; }

    public string Title { get; }

    public string Message { get; }

    public override string ToString()
        => $"{nameof(ToastNotificationType)}: {ToastNotificationType}, {nameof(Title)}: {Title}, {nameof(Message)}: {Message}";
}