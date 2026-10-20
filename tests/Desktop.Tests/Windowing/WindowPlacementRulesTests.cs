using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.Desktop.Windowing;

namespace GridironFrontOffice.Desktop.Tests.Windowing;

public sealed class WindowPlacementRulesTests
{
    // A 1920 × 1080 primary with a 40 px taskbar, and a 2560 × 1440 monitor to its right at 150%.
    private static readonly MonitorArea Primary = new(new PixelRect(0, 0, 1920, 1040), 96, IsPrimary: true);
    private static readonly MonitorArea Right = new(new PixelRect(1920, 0, 2560, 1400), 144, IsPrimary: false);

    [Fact]
    public void First_launch_centers_the_default_size_on_the_primary_monitor()
    {
        var placement = WindowPlacementRules.Fit(saved: null, [Right, Primary]);

        placement.ShouldBe(new StartupPlacement(240, 70, 1440, 900, Maximized: false, FromSaved: false));
    }

    [Fact]
    public void First_launch_on_a_screen_smaller_than_the_default_starts_maximized()
    {
        var laptop = new MonitorArea(new PixelRect(0, 0, 1366, 728), 96, IsPrimary: true);

        var placement = WindowPlacementRules.Fit(saved: null, [laptop]);

        placement.Maximized.ShouldBeTrue();
        (placement.Left, placement.Top).ShouldBe((0, 0));
    }

    [Fact]
    public void With_no_monitor_information_WPF_centers_the_default_size()
    {
        var placement = WindowPlacementRules.Fit(saved: null, []);

        placement.ShouldBe(new StartupPlacement(null, null, 1440, 900, Maximized: false));
    }

    [Fact]
    public void A_saved_placement_that_still_fits_comes_back_unchanged()
    {
        var saved = new WindowPlacement(100, 50, 1500, 950, Dpi: 96, Maximized: false);

        var placement = WindowPlacementRules.Fit(saved, [Primary, Right]);

        placement.ShouldBe(new StartupPlacement(100, 50, 1500, 950, Maximized: false, FromSaved: true));
    }

    [Fact]
    public void The_maximized_state_comes_back_with_the_normal_bounds_under_it()
    {
        var saved = new WindowPlacement(100, 50, 1500, 950, Dpi: 96, Maximized: true);

        WindowPlacementRules.Fit(saved, [Primary]).Maximized.ShouldBeTrue();
    }

    [Fact]
    public void A_window_on_a_monitor_that_was_unplugged_falls_back_to_the_default()
    {
        var onTheRightMonitor = new WindowPlacement(2200, 100, 2160, 1350, Dpi: 144, Maximized: true);

        var placement = WindowPlacementRules.Fit(onTheRightMonitor, [Primary]);

        placement.ShouldBe(new StartupPlacement(240, 70, 1440, 900, Maximized: false, FromSaved: false));
    }

    [Fact]
    public void A_window_hanging_off_an_edge_moves_fully_onto_its_monitor()
    {
        var offTheBottomRight = new WindowPlacement(1000, 600, 1440, 900, Dpi: 96, Maximized: false);

        var placement = WindowPlacementRules.Fit(offTheBottomRight, [Primary]);

        (placement.Left, placement.Top).ShouldBe((480, 140));
        (placement.Width, placement.Height).ShouldBe((1440, 900));
    }

    [Fact]
    public void A_window_larger_than_its_screen_shrinks_to_the_work_area()
    {
        var smallerNow = new MonitorArea(new PixelRect(0, 0, 1600, 860), 96, IsPrimary: true);
        var saved = new WindowPlacement(0, 0, 1920, 1040, Dpi: 96, Maximized: false);

        var placement = WindowPlacementRules.Fit(saved, [smallerNow]);

        placement.ShouldBe(new StartupPlacement(0, 0, 1600, 860, Maximized: false, FromSaved: true));
    }

    [Fact]
    public void The_minimum_size_wins_over_a_screen_that_is_too_small()
    {
        var tiny = new MonitorArea(new PixelRect(0, 0, 1280, 680), 96, IsPrimary: true);
        var saved = new WindowPlacement(0, 0, 1280, 720, Dpi: 96, Maximized: false);

        var placement = WindowPlacementRules.Fit(saved, [tiny]);

        (placement.Width, placement.Height).ShouldBe((1280, 720));
        (placement.Left, placement.Top).ShouldBe((0, 0));
    }

    [Fact]
    public void The_size_in_DIPs_survives_a_change_of_monitor_scale()
    {
        // Saved at 100% on what is now a 150% monitor: 1440 × 900 DIPs is 2160 × 1350 physical pixels there.
        var saved = new WindowPlacement(2000, 20, 1440, 900, Dpi: 96, Maximized: false);

        var placement = WindowPlacementRules.Fit(saved, [Primary, Right]);

        (placement.Width, placement.Height).ShouldBe((1440, 900));
        (placement.Left, placement.Top).ShouldBe((2000, 20));
    }

    [Fact]
    public void A_window_across_two_monitors_goes_to_the_one_it_overlaps_most()
    {
        var mostlyRight = new WindowPlacement(1700, 100, 1500, 950, Dpi: 96, Maximized: false);

        var placement = WindowPlacementRules.Fit(mostlyRight, [Primary, Right]);

        placement.Left.ShouldBe(1920);   // fully on the right monitor
    }

    [Theory]
    [InlineData(0, 900, 96)]
    [InlineData(1440, 0, 96)]
    [InlineData(1440, 900, 0)]
    public void A_damaged_placement_falls_back_to_the_default(int width, int height, int dpi)
    {
        var saved = new WindowPlacement(100, 100, width, height, dpi, Maximized: false);

        WindowPlacementRules.Fit(saved, [Primary]).FromSaved.ShouldBeFalse();
    }
}
