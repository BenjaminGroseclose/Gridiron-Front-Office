using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.UI.Components.Overlays;
using Microsoft.AspNetCore.Components;

namespace GridironFrontOffice.UI.Menu;

/// <summary>
/// What the main menu, the load screen and the shell do with saves (M0 WP9, PERSISTENCE.md §9–10): the one place that
/// turns ISaveManager's results into navigation and messages, so every screen says the same thing.
/// </summary>
public sealed class SaveActions(ISaveManager saves, IGfoDialogs dialogs, IGfoToasts toasts, NavigationManager navigation)
{
    /// <summary>The open league's home.</summary>
    public const string LeagueHome = "/league";

    /// <summary>The main menu.</summary>
    public const string MainMenu = "/";

    /// <summary>Opens a save and goes to its home. A failure is reported and returns false.</summary>
    public async Task<bool> OpenAsync(SaveCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        var opened = await saves.OpenAsync(card.FilePath);
        if (opened.IsFailure)
        {
            await toasts.ErrorAsync($"Couldn't open {SaveCardText.Title(card)}", opened.Error!.Message);
            return false;
        }

        if (opened.Value.Upgraded)
        {
            await toasts.InfoAsync($"{opened.Value.LeagueName} was upgraded", "It was saved by an older version. A snapshot of it was kept first.");
        }

        navigation.NavigateTo(LeagueHome);
        return true;
    }

    /// <summary>Creates a league and goes to its home, or returns why the name was refused.</summary>
    public async Task<DomainError?> CreateAsync(string leagueName)
    {
        var created = await saves.CreateAsync(leagueName);
        if (created.IsFailure)
        {
            return created.Error;
        }

        navigation.NavigateTo(LeagueHome);
        return null;
    }

    /// <summary>
    /// Asks first, naming the league, then sends the save and its snapshots to the Recycle Bin. True when it's gone.
    /// </summary>
    public async Task<bool> DeleteAsync(SaveCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        var title = SaveCardText.Title(card);
        // An unreadable file has no SaveID, so no snapshots go with it (PERSISTENCE.md §10).
        var what = card.Status == SaveCardStatus.Unreadable ? title : $"{title}, with its snapshots,";
        var confirmed = await dialogs.ConfirmAsync(
            $"Delete {title}?",
            $"{what} goes to the Recycle Bin. You can restore it from there.",
            confirm: "Delete",
            cancel: "Keep it");
        if (!confirmed)
        {
            return false;
        }

        var deleted = await saves.DeleteAsync(card.FilePath);
        if (deleted.IsSuccess)
        {
            await toasts.SuccessAsync($"{title} moved to the Recycle Bin");
            return true;
        }

        var error = deleted.Error!;
        if (error.Code == SaveErrors.DeleteCancelled(card.FileName).Code)
        {
            await toasts.InfoAsync(error.Message);
        }
        else
        {
            await toasts.ErrorAsync($"Couldn't delete {title}", error.Message);
        }

        return false;
    }

    /// <summary>Closes the open league and goes back to the main menu.</summary>
    public async Task CloseAsync()
    {
        await saves.CloseAsync();
        navigation.NavigateTo(MainMenu);
    }
}
