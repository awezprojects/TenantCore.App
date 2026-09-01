namespace TenantCore.Web.Client.Services;

public enum ToastLevel { Success, Error, Warning }

public record ToastMessage(string Text, ToastLevel Level, string? ActionText = null, string? ActionUrl = null);

public interface IToastService
{
    event Action<ToastMessage>? OnToast;
    void ShowSuccess(string message);
    void ShowError(string message, string? actionText = null, string? actionUrl = null);
    void ShowWarning(string message, string? actionText = null, string? actionUrl = null);
}

public class ToastService : IToastService
{
    public event Action<ToastMessage>? OnToast;

    public void ShowSuccess(string message) => OnToast?.Invoke(new ToastMessage(message, ToastLevel.Success));
    public void ShowError(string message, string? actionText = null, string? actionUrl = null) =>
        OnToast?.Invoke(new ToastMessage(message, ToastLevel.Error, actionText, actionUrl));
    public void ShowWarning(string message, string? actionText = null, string? actionUrl = null) =>
        OnToast?.Invoke(new ToastMessage(message, ToastLevel.Warning, actionText, actionUrl));
}
