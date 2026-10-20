namespace GridironFrontOffice.Application.Settings;

/// <summary>
/// The app settings, loaded once before DI (so the log level can come from them) and saved atomically.
/// Screens read <see cref="Current"/> and react to <see cref="Changed"/>.
/// </summary>
public interface ISettingsStore
{
    AppSettings Current { get; }

    /// <summary>Raised after a successful save, with the new settings.</summary>
    event EventHandler<AppSettings>? Changed;

    /// <summary>Writes the settings (temp file, then replace) and makes them current.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
