using GridironFrontOffice.UI.Components.Theming;

namespace GridironFrontOffice.UI.Components.Tests.Theming;

/// <summary>VISUAL_DESIGN.md §3.3: the derived accent colors meet their contrast floors for any team color, in both modes.</summary>
public sealed class AccentColorsTests
{
    // F-02 primaries that stress the rules: dark, loud, pale and near-black, plus Fluent's neutral blue.
    public static TheoryData<string> TeamColors =>
        ["#0F6CBD", "#1B2A4A", "#1F3F7A", "#D9541E", "#E1AD21", "#A6192E", "#1F9E94", "#1E1E1E", "#F4F1EA", "#FFFFFF", "#000000"];

    [Theory]
    [MemberData(nameof(TeamColors))]
    public void Marks_and_text_meet_their_contrast_on_Surface_1_in_both_modes(string primary)
    {
        foreach (var dark in new[] { true, false })
        {
            var surface = StadiumPalette.For(dark).Surface1;
            var (fill, onAccent, accent, accentText) = AccentColors.For(primary, surface, dark);

            fill.ShouldBe(AccentColors.Normalize(primary), "The fill is the exact team color.");
            AccentColors.Contrast(accent, surface).ShouldBeGreaterThanOrEqualTo(3.0, $"accent {accent} ({(dark ? "dark" : "light")})");
            AccentColors.Contrast(accentText, surface).ShouldBeGreaterThanOrEqualTo(4.5, $"accent text {accentText} ({(dark ? "dark" : "light")})");
            AccentColors.Contrast(onAccent, fill).ShouldBeGreaterThanOrEqualTo(4.5, $"on-accent {onAccent} on {fill}");
        }
    }

    [Theory]
    [InlineData("#1B2A4A", "#FFFFFF")]   // dark navy: white text
    [InlineData("#E1AD21", "#0B0D12")]   // pale gold: near-black text
    [InlineData("#D9541E", "#0B0D12")]   // orange: near-black contrasts more than white
    public void On_accent_is_white_or_near_black_whichever_contrasts_more(string fill, string expected) =>
        AccentColors.OnAccent(fill).ShouldBe(expected);

    [Fact]
    public void A_color_that_already_passes_is_left_alone()
    {
        AccentColors.Corrected("#E1AD21", StadiumPalette.Dark.Surface1, dark: true, ratio: 3.0).ShouldBe("#E1AD21");
    }

    [Fact]
    public void A_dark_team_color_is_lightened_in_dark_mode_and_a_pale_one_darkened_in_light_mode()
    {
        var navyInDark = AccentColors.Corrected("#1B2A4A", StadiumPalette.Dark.Surface1, dark: true, ratio: 4.5);
        var goldInLight = AccentColors.Corrected("#E1AD21", StadiumPalette.Light.Surface1, dark: false, ratio: 4.5);

        Luminance(navyInDark).ShouldBeGreaterThan(Luminance("#1B2A4A"));
        Luminance(goldInLight).ShouldBeLessThan(Luminance("#E1AD21"));
    }

    [Theory]
    [InlineData("#abc", "#AABBCC")]
    [InlineData(" #0f6cbd ", "#0F6CBD")]
    public void Colors_are_normalized_to_upper_case_six_digits(string input, string expected) =>
        AccentColors.Normalize(input).ShouldBe(expected);

    [Theory]
    [InlineData("0F6CBD")]
    [InlineData("#0F6CB")]
    [InlineData("#GGGGGG")]
    [InlineData("blue")]
    public void A_bad_team_color_is_a_data_error(string input) =>
        Should.Throw<FormatException>(() => AccentColors.Normalize(input));

    private static double Luminance(string color) => AccentColors.Contrast(color, "#000000");
}
