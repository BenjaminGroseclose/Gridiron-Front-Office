using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.UI.Components.Tests.TestDoubles;

/// <summary>
/// Fluent's theme service as GfoThemeService uses it: records the settings it was asked to build from and the Theme it
/// was asked to apply. Anything else GfoThemeService must never call throws.
/// </summary>
internal sealed class FakeThemeService : IThemeService
{
    public List<ThemeSettings> Created { get; } = [];

    public List<Theme> Applied { get; } = [];

    public int CacheClears { get; private set; }

    public Task<Theme> CreateCustomThemeAsync(ThemeSettings settings)
    {
        Created.Add(settings);
        return Task.FromResult(new Theme());
    }

    public Task SetThemeAsync(Theme theme)
    {
        Applied.Add(theme);
        return Task.CompletedTask;
    }

    public Task ClearStoredThemeSettingsAsync()
    {
        CacheClears++;
        return Task.CompletedTask;
    }

    // GfoThemeService never hands Fluent a mode, a color or settings on their own (D-067): these must stay unused.
    public Task SetThemeAsync(ThemeColorVariant type) => throw Unused();

    public Task SetThemeAsync(ThemeColorVariant type, ThemeMode mode) => throw Unused();

    public Task SetThemeAsync(string color, bool isExact) => throw Unused();

    public Task SetThemeAsync(ThemeMode mode) => throw Unused();

    public Task SetThemeAsync(ThemeSettings settings) => throw Unused();

    public Task<bool> IsSystemDarkAsync() => throw Unused();

    public Task<bool> IsDarkModeAsync() => throw Unused();

    public Task<IReadOnlyDictionary<string, string>?> GetColorRampAsync() => throw Unused();

    public Task<IReadOnlyDictionary<string, string>?> GetColorRampFromSettingsAsync(ThemeSettings settings) => throw Unused();

    public Task<string> GetBrandColorAsync() => throw Unused();

    public Task<ThemeSettings?> GetThemeSettingsAsync() => throw Unused();

    public Task<bool> IsExactBrandColorAsync() => throw Unused();

    public Task SwitchDirectionAsync() => throw Unused();

    public Task<bool> SwitchThemeAsync() => throw Unused();

    public Task SetThemeToElementAsync(ElementReference element, ThemeSettings settings) => throw Unused();

    public Task SetThemeToElementAsync(ElementReference element, Theme theme) => throw Unused();

    private static NotSupportedException Unused() => new("GfoThemeService doesn't use this.");
}
