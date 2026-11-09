using System.Text.RegularExpressions;
using GridironFrontOffice.UI.Components.Theming;

namespace GridironFrontOffice.UI.Components.Tests.Theming;

/// <summary>
/// The danger button (UI_DESIGN_SYSTEM.md §5.1): a primary FluentButton with Class="gfo-danger", painted by
/// gfo-tokens.css. Its colors are checked where they're written, so a value changed by hand can't slip under the floor.
/// </summary>
public sealed partial class DangerButtonTests
{
    private static readonly string Css = File.ReadAllText(Path.Combine(RepoRoot().FullName, "src", "UI.Components", "wwwroot", "gfo-tokens.css"));

    [Fact]
    public void The_text_holds_4_5_to_1_on_the_fill_and_on_every_state()
    {
        var text = Token("--gfo-on-danger");

        foreach (var fill in new[] { "--gfo-danger", "--gfo-danger-hover", "--gfo-danger-pressed" })
        {
            AccentColors.Contrast(text, Token(fill)).ShouldBeGreaterThanOrEqualTo(4.5, $"{fill} {Token(fill)} under {text}");
        }
    }

    [Fact]
    public void The_states_follow_the_one_rule_for_button_states()
    {
        var fill = Token("--gfo-danger");
        var (hover, pressed, _) = AccentColors.StateFills(fill);

        // D-095 #1: the text is whichever of white and near-black reads better, and each state moves away from it
        Token("--gfo-on-danger").ShouldBe(AccentColors.OnAccent(fill));
        (Token("--gfo-danger-hover"), Token("--gfo-danger-pressed")).ShouldBe((hover, pressed));
    }

    [Fact]
    public void The_danger_red_is_the_urgent_red() => Token("--gfo-danger").ShouldBe(Token("--gfo-urgent"));

    [Fact]
    public void The_class_sets_the_four_tokens_Fluent_paints_a_primary_button_with()
    {
        var rule = DangerRule().Match(Css);

        rule.Success.ShouldBeTrue("gfo-tokens.css has no fluent-button.gfo-danger rule.");
        rule.Groups["body"].Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ShouldBe(
        [
            "--colorBrandBackground: var(--gfo-danger)",
            "--colorBrandBackgroundHover: var(--gfo-danger-hover)",
            "--colorBrandBackgroundPressed: var(--gfo-danger-pressed)",
            "--colorNeutralForegroundOnBrand: var(--gfo-on-danger)",
        ], ignoreOrder: true);
    }

    // A token's one value in the file (the danger tokens are the same in both modes, so they're declared once)
    private static string Token(string name)
    {
        var values = Regex.Matches(Css, $@"{Regex.Escape(name)}:\s*(#[0-9a-fA-F]{{6}})\s*;").Select(m => m.Groups[1].Value).ToList();
        values.Count.ShouldBe(1, $"{name} should be declared once, as #RRGGBB.");
        return AccentColors.Normalize(values[0]);
    }

    [GeneratedRegex(@"fluent-button\.gfo-danger\s*\{(?<body>[^}]*)\}")]
    private static partial Regex DangerRule();

    private static DirectoryInfo RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GridironFrontOffice.slnx")))
            {
                return dir;
            }
        }

        throw new InvalidOperationException($"GridironFrontOffice.slnx not found above {AppContext.BaseDirectory}.");
    }
}
