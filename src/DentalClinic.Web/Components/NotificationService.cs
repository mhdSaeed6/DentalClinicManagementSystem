namespace DentalClinic.Web.Components;

/// <summary>
/// Lightweight in-memory notification hub used to render transient toasts.
/// </summary>
public sealed class NotificationService
{
    private readonly List<ToastMessage> _messages = [];

    public event Action? Changed;

    public IReadOnlyList<ToastMessage> Messages => _messages;

    public void Success(string title, string? message = null) => Add(ToastKind.Success, title, message);

    public void Info(string title, string? message = null) => Add(ToastKind.Info, title, message);

    public void Warning(string title, string? message = null) => Add(ToastKind.Warning, title, message);

    public void Error(string title, string? message = null) => Add(ToastKind.Error, title, message);

    public void Dismiss(Guid id)
    {
        _messages.RemoveAll(message => message.Id == id);
        Changed?.Invoke();
    }

    private void Add(ToastKind kind, string title, string? message)
    {
        _messages.Add(new ToastMessage(Guid.NewGuid(), kind, title, message, DateTime.Now));

        // Keep only the most recent handful of notifications on screen.
        if (_messages.Count > 5)
        {
            _messages.RemoveAt(0);
        }

        Changed?.Invoke();
    }
}

public enum ToastKind
{
    Success,
    Info,
    Warning,
    Error,
}

public sealed record ToastMessage(Guid Id, ToastKind Kind, string Title, string? Message, DateTime CreatedAt);
