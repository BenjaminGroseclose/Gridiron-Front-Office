using GridironFrontOffice.Application.Settings;
using Microsoft.Win32;

namespace GridironFrontOffice.Desktop.Windowing;

/// <summary>
/// Decides the mode for the first paint, before the page exists (UI_DESIGN_SYSTEM.md §3.3). Match Windows reads the
/// Windows app mode from the registry; the page's theme service takes over once it has applied.
/// </summary>
internal static class WindowsTheme
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool StartsDark(ThemeChoice choice) => choice switch
    {
        ThemeChoice.Light => false,
        ThemeChoice.MatchWindows => AppsUseDarkMode(),
        _ => true,
    };

    /// <summary>Windows' "Choose your app mode". A missing or unreadable value counts as light, Windows' own default.</summary>
    private static bool AppsUseDarkMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
    }
}
