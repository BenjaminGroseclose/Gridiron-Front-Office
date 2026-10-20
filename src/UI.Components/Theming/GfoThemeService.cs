using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GridironFrontOffice.UI.Components.Theming;

/// <summary>
/// The only code that calls Fluent's theme service (UI_DESIGN_SYSTEM.md §3.2–3.3, D-067). It builds a custom Theme from
/// the exact accent color and an explicit Dark or Light mode, sets the Stadium values and GFO's computed accent colors,
/// and resolves Match Windows itself: Fluent never sees <c>ThemeMode.System</c>, because on a Windows switch it would
/// swap in its default theme. It also owns the page side: the <c>data-theme</c> and <c>data-density</c> attributes
/// that gfo-tokens.css reads, the end of the boot hide, and the watch on the Windows light/dark setting.
/// </summary>
public sealed class GfoThemeService(IThemeService fluent, IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./_content/GridironFrontOffice.UI.Components/gfo-theme.js";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IJSObjectReference? _module;
    private DotNetObjectReference<GfoThemeService>? _self;
    private bool _windowsDark = true;

    /// <summary>The theme as last applied, or null before the first <see cref="ApplyAsync"/>.</summary>
    public GfoThemeState? Current { get; private set; }

    /// <summary>Raised after every apply, including the re-apply when Windows switches light/dark.</summary>
    public event EventHandler<GfoThemeState>? Applied;

    /// <summary>
    /// Applies the appearance. Called at startup (neutral accent), on league open (the team's primary) and whenever a
    /// setting changes. Mode and color always travel together (v5 issue #5351).
    /// </summary>
    public async Task ApplyAsync(GfoAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        GfoThemeState state;
        await _gate.WaitAsync();
        try
        {
            var module = await StartAsync();
            var dark = appearance.Mode switch
            {
                GfoThemeMode.Light => false,
                GfoThemeMode.MatchWindows => _windowsDark,
                _ => true,
            };

            var palette = StadiumPalette.For(dark);
            var (fill, onAccent, accent, accentText) = AccentColors.For(appearance.AccentColor ?? GfoAppearance.NeutralAccent, palette.Surface1, dark);
            state = new GfoThemeState(appearance, dark, fill, onAccent, accent, accentText);

            var theme = await fluent.CreateCustomThemeAsync(new ThemeSettings
            {
                Color = fill,
                Mode = dark ? ThemeMode.Dark : ThemeMode.Light,
                IsExact = true,
            });
            StadiumTheme.Apply(theme, palette, state);
            await fluent.SetThemeAsync(theme);

            var density = appearance.Density == GfoDensity.Compact ? "compact" : "comfortable";
            await module.InvokeVoidAsync("applied", dark ? "dark" : "light", density);
            Current = state;
        }
        finally
        {
            _gate.Release();
        }

        Applied?.Invoke(this, state);
    }

    /// <summary>
    /// The page's <c>prefers-color-scheme</c> changed. Re-applies on every switch: with the new mode under Match
    /// Windows, and unchanged otherwise, as a safety net in case a Fluent upgrade drops the listener guard (§3.2).
    /// </summary>
    [JSInvokable("OnWindowsThemeChanged")]
    public Task OnWindowsThemeChangedAsync(bool dark)
    {
        _windowsDark = dark;
        return Current is { } current ? ApplyAsync(current.Appearance) : Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("stop");
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The page is already gone (DevHost circuit closed, or the window closing).
            }
        }

        _self?.Dispose();
        _gate.Dispose();
    }

    /// <summary>Once per page: load the module, start the Windows watch, and clear Fluent's cached theme settings (§3.3).</summary>
    private async Task<IJSObjectReference> StartAsync()
    {
        if (_module is not null)
        {
            return _module;
        }

        var module = await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
        _self = DotNetObjectReference.Create(this);
        _windowsDark = await module.InvokeAsync<bool>("start", _self);
        await fluent.ClearStoredThemeSettingsAsync();
        _module = module;
        return module;
    }
}
