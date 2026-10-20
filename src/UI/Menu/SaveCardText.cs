using System.Globalization;
using GridironFrontOffice.Application.Saves;

namespace GridironFrontOffice.UI.Menu;

/// <summary>The words on a save card and under Continue (PERSISTENCE.md §9). Until M1 there's no team, season or record.</summary>
public static class SaveCardText
{
    /// <summary>The league's name, or the file name when the file can't be read.</summary>
    public static string Title(SaveCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Status == SaveCardStatus.Unreadable || card.LeagueName.Length == 0 ? card.FileName : card.LeagueName;
    }

    /// <summary>"Last played Oct 19, 2026 · Created Oct 12, 2026 · 2 h 15 min played", or nothing for an unreadable file.</summary>
    public static string? Details(SaveCard card, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Status == SaveCardStatus.Unreadable
            ? null
            : $"Last played {Date(card.LastPlayedAtUtc, culture)} · Created {Date(card.CreatedAtUtc, culture)} · {PlayTime(card.PlayTimeSeconds)} played";
    }

    /// <summary>A note for anything that isn't simply ready to open, or null.</summary>
    public static string? Note(SaveCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Status switch
        {
            SaveCardStatus.NeedsUpgrade => "Saved by an older version. Opening it upgrades the file, after keeping a snapshot.",
            SaveCardStatus.FromNewerVersion =>
                $"Saved by a newer version{(card.AppVersionLastOpened.Length > 0 ? $" ({card.AppVersionLastOpened})" : "")}. Update Gridiron Front Office to open it.",
            SaveCardStatus.Unreadable => card.Problem ?? "This file can't be read.",
            _ => null,
        };
    }

    /// <summary>The day in local time: "Oct 19, 2026".</summary>
    public static string Date(DateTime utc, CultureInfo culture) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("MMM d, yyyy", culture);

    /// <summary>"under a minute", "45 min", "2 h 15 min".</summary>
    public static string PlayTime(long seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return time.TotalMinutes < 1 ? "under a minute"
            : time.TotalHours < 1 ? $"{time.Minutes} min"
            : $"{(int)time.TotalHours} h {time.Minutes} min";
    }
}
