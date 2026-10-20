using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.Infrastructure.Paths;
using GridironFrontOffice.Infrastructure.Settings;

namespace GridironFrontOffice.Infrastructure.Tests.Settings;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly TempFolder _temp = new();
    private readonly AppPaths _paths;

    public SettingsStoreTests() => _paths = AppPaths.ForRoot(_temp.Path);

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void A_missing_file_is_created_with_the_defaults()
    {
        using var store = SettingsStore.Load(_paths);

        store.Current.ShouldBe(new AppSettings());
        store.Current.Appearance.Theme.ShouldBe(ThemeChoice.Dark);       // D-031
        store.Current.Developer.LogLevel.ShouldBe(AppLogLevel.Information);
        store.Created.ShouldBeTrue();
        store.LoadWarning.ShouldBeNull();
        File.ReadAllText(_paths.SettingsFile).ShouldContain("\"theme\": \"dark\"");
        File.Exists(_paths.SettingsFile + ".tmp").ShouldBeFalse();

        using var second = SettingsStore.Load(_paths);
        second.Created.ShouldBeFalse("Only the first launch creates it.");
        second.Current.ShouldBe(new AppSettings());
    }

    [Fact]
    public void A_file_that_cannot_be_created_still_starts_on_the_defaults()
    {
        var blocker = Path.Combine(_temp.Path, "not-a-folder");
        File.WriteAllText(blocker, "a file where the data folder should be");

        using var store = SettingsStore.Load(AppPaths.ForRoot(blocker));

        store.Current.ShouldBe(new AppSettings());
        store.Created.ShouldBeFalse();
        store.LoadWarning.ShouldNotBeNull();
        store.LoadWarning.ShouldContain("couldn't be created");
    }

    [Fact]
    public async Task Saved_settings_load_back_and_become_current()
    {
        var changed = new List<AppSettings>();
        var settings = new AppSettings
        {
            Appearance = new AppearanceSettings { Theme = ThemeChoice.MatchWindows, Accent = AccentMode.Neutral, Density = Density.Compact },
            Developer = new DeveloperSettings { LogLevel = AppLogLevel.Debug, LogSql = true },
        };

        using (var store = SettingsStore.Load(_paths))
        {
            store.Changed += (_, s) => changed.Add(s);
            await store.SaveAsync(settings, TestContext.Current.CancellationToken);
            store.Current.ShouldBe(settings);
        }

        using var reloaded = SettingsStore.Load(_paths);
        reloaded.Current.ShouldBe(settings);
        changed.ShouldBe([settings]);
    }

    [Fact]
    public async Task The_file_is_readable_json_with_named_values_and_no_temp_file_is_left()
    {
        using var store = SettingsStore.Load(_paths);

        await store.SaveAsync(new AppSettings { Appearance = new AppearanceSettings { Theme = ThemeChoice.MatchWindows } },
            TestContext.Current.CancellationToken);

        var json = await File.ReadAllTextAsync(_paths.SettingsFile, TestContext.Current.CancellationToken);
        json.ShouldContain("\"theme\": \"matchWindows\"");
        json.ShouldContain("\"logLevel\": \"information\"");
        File.Exists(_paths.SettingsFile + ".tmp").ShouldBeFalse();
    }

    [Fact]
    public void An_unreadable_file_falls_back_to_defaults_and_is_kept_aside()
    {
        File.WriteAllText(_paths.SettingsFile, "{ \"appearance\": { \"theme\": ");

        using var store = SettingsStore.Load(_paths);

        store.Current.ShouldBe(new AppSettings());
        store.LoadWarning.ShouldNotBeNull();
        store.LoadWarning.ShouldContain("settings.unreadable.json");
        File.ReadAllText(Path.Combine(_temp.Path, "settings.unreadable.json")).ShouldStartWith("{ \"appearance\"");
    }

    [Fact]
    public void Hand_edited_nulls_and_unknown_settings_are_tolerated()
    {
        File.WriteAllText(_paths.SettingsFile, """
            {
              // a comment
              "appearance": null,
              "developer": { "logLevel": "debug" },
              "aSettingFromTheFuture": 42,
            }
            """);

        using var store = SettingsStore.Load(_paths);

        store.LoadWarning.ShouldBeNull();
        store.Current.Appearance.ShouldBe(new AppearanceSettings());
        store.Current.Developer.LogLevel.ShouldBe(AppLogLevel.Debug);
    }
}
