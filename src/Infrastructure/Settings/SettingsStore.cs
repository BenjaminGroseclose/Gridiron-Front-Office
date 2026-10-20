using System.Text.Json;
using System.Text.Json.Serialization;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Settings;

namespace GridironFrontOffice.Infrastructure.Settings;

/// <summary>
/// <c>settings.json</c> (ARCHITECTURE.md §7.4). Loaded synchronously before DI, because the log level comes from it.
/// A missing file is created with the defaults, so it's there to find and edit by hand. An unreadable file is copied aside
/// and defaults are used. The composition root logs <see cref="Created"/> and <see cref="LoadWarning"/> once the logger
/// exists. Every write is atomic: temp file, then replace.
/// </summary>
public sealed class SettingsStore : ISettingsStore, IDisposable
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _path;
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    private SettingsStore(string path, AppSettings current, string? loadWarning, bool created = false)
    {
        _path = path;
        Current = current;
        LoadWarning = loadWarning;
        Created = created;
    }

    public AppSettings Current { get; private set; }

    /// <summary>Why the file couldn't be used or created, if it couldn't. Null otherwise.</summary>
    public string? LoadWarning { get; }

    /// <summary>True when this launch found no file and wrote one with the defaults (normally the first launch).</summary>
    public bool Created { get; }

    public event EventHandler<AppSettings>? Changed;

    public static SettingsStore Load(IAppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var path = paths.SettingsFile;
        if (!File.Exists(path))
        {
            var defaults = new AppSettings();
            try
            {
                WriteNow(path, defaults);
                return new SettingsStore(path, defaults, loadWarning: null, created: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return new SettingsStore(path, defaults, $"settings.json couldn't be created ({ex.GetType().Name}: {ex.Message}). Using defaults.");
            }
        }

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Json)
                ?? throw new JsonException("The file is empty or holds null.");
            return new SettingsStore(path, Normalize(settings), loadWarning: null);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            var setAside = SetAside(path);
            return new SettingsStore(path, new AppSettings(),
                $"settings.json couldn't be read ({ex.GetType().Name}: {ex.Message}). Using defaults; the old file was kept as {Path.GetFileName(setAside)}.");
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await _saveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temp = _path + ".tmp";
            var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None);
            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(stream, settings, Json, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temp, _path, overwrite: true);
            Current = settings;
        }
        finally
        {
            _saveGate.Release();
        }

        Changed?.Invoke(this, settings);
    }

    public void Dispose() => _saveGate.Dispose();

    /// <summary>The synchronous write for <see cref="Load"/>, which runs before DI and any async context.</summary>
    private static void WriteNow(string path, AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, settings, Json);
            stream.Flush(flushToDisk: true);
        }

        File.Move(temp, path, overwrite: true);
    }

    /// <summary>Nulls in a hand-edited file (e.g. <c>"developer": null</c>) fall back to the defaults.</summary>
    private static AppSettings Normalize(AppSettings settings) => settings with
    {
        Appearance = settings.Appearance ?? new AppearanceSettings(),
        Developer = settings.Developer ?? new DeveloperSettings(),
    };

    private static string SetAside(string path)
    {
        var target = Path.Combine(Path.GetDirectoryName(path)!, "settings.unreadable.json");
        try
        {
            File.Copy(path, target, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Keeping a copy is best effort. The defaults still apply, and the original file is left untouched.
        }

        return target;
    }
}
