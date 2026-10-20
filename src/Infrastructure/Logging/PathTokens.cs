namespace GridironFrontOffice.Infrastructure.Logging;

/// <summary>
/// Replaces user-specific folders with environment tokens (<c>%LOCALAPPDATA%</c>, …), so the Windows user name
/// doesn't end up in logs people share (LOGGING.md §7, §11). The log formatter applies it to every line.
/// </summary>
public static class PathTokens
{
    // Most specific first: AppData, LocalAppData and Temp all live under the user profile.
    private static readonly (string Folder, string Token)[] Replacements =
    [
        .. new[]
        {
            (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "%LOCALAPPDATA%"),
            (Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "%APPDATA%"),
            (Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), "%TEMP%"),
            (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "%USERPROFILE%"),
        }
        .Where(r => r.Item1.Length > 3)   // never tokenize a bare drive root
        .OrderByDescending(r => r.Item1.Length),
    ];

    public static string Tokenize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        foreach (var (folder, token) in Replacements)
        {
            if (text.Contains(folder, StringComparison.OrdinalIgnoreCase))
            {
                text = text.Replace(folder, token, StringComparison.OrdinalIgnoreCase);
            }
        }

        return text;
    }
}
