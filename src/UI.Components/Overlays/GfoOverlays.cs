using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.UI.Components.Overlays;

/// <summary>Decisions and quick looks (UI_DESIGN_SYSTEM.md §2: peek, then commit; modals only for decisions).</summary>
public interface IGfoDialogs
{
    /// <summary>Asks a question with two buttons. True when the player chose <paramref name="confirm"/>; Escape is no.</summary>
    Task<bool> ConfirmAsync(string title, string message, string confirm = "OK", string cancel = "Cancel");

    /// <summary>
    /// Opens a peek sheet: a drawer on the right with <typeparamref name="TComponent"/> inside. Its markup goes inside
    /// <c>&lt;FluentDialogBody&gt;</c>, which draws the header, the close button and the scrolling body. Completes
    /// when the sheet closes.
    /// </summary>
    Task PeekAsync<TComponent>(string title, IReadOnlyDictionary<string, object?>? parameters = null, string width = "480px")
        where TComponent : ComponentBase;
}

/// <summary>Short notices, bottom right (UI_DESIGN_SYSTEM.md §5.1).</summary>
public interface IGfoToasts
{
    Task SuccessAsync(string title, string? message = null);

    Task InfoAsync(string title, string? message = null);

    Task WarningAsync(string title, string? message = null);

    Task ErrorAsync(string title, string? message = null);
}

/// <summary>IGfoDialogs over Fluent's dialog service (the v5 names verified in Spike S0b).</summary>
internal sealed class GfoDialogs(IDialogService dialogs) : IGfoDialogs
{
    public async Task<bool> ConfirmAsync(string title, string message, string confirm = "OK", string cancel = "Cancel")
    {
        // Fluent's order is (message, title, primaryButton, secondaryButton).
        var result = await dialogs.ShowConfirmationAsync(message, title, confirm, cancel);
        return !result.Cancelled;
    }

    public async Task PeekAsync<TComponent>(string title, IReadOnlyDictionary<string, object?>? parameters = null, string width = "480px")
        where TComponent : ComponentBase
    {
        await dialogs.ShowDrawerAsync<TComponent>(options =>
        {
            options.Header.Title = title;
            if (options.Header.CloseAction is { } close)
            {
                close.Visible = true;
            }

            options.Alignment = DialogAlignment.End;
            options.Width = width;
            if (parameters is not null)
            {
                options.Parameters = parameters.ToDictionary(p => p.Key, p => p.Value);
            }
        });
    }
}

/// <summary>IGfoToasts over Fluent's notification service (v5 has no IToastService).</summary>
internal sealed class GfoToasts(INotificationService notifications) : IGfoToasts
{
    public Task SuccessAsync(string title, string? message = null) => ShowAsync(ToastIntent.Success, title, message);

    public Task InfoAsync(string title, string? message = null) => ShowAsync(ToastIntent.Info, title, message);

    public Task WarningAsync(string title, string? message = null) => ShowAsync(ToastIntent.Warning, title, message);

    public Task ErrorAsync(string title, string? message = null) => ShowAsync(ToastIntent.Error, title, message);

    private async Task ShowAsync(ToastIntent intent, string title, string? message)
    {
        await notifications.ShowToastAsync(options =>
        {
            options.Intent = intent;
            options.Title = title;
            options.Message = message;
        });
    }
}
