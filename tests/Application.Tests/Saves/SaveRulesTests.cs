using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.Results;

namespace GridironFrontOffice.Application.Tests.Saves;

public sealed class SaveRulesTests
{
    [Theory]
    [InlineData(false, false, null)]
    [InlineData(true, false, "IronmanNoRestore")]
    [InlineData(false, true, "LiveEventRunning")]
    [InlineData(true, true, "IronmanNoRestore")]
    public void Restore_is_refused_in_Ironman_and_during_a_live_event(bool ironman, bool liveEvent, string? refusal) =>
        SaveRules.CanRestore(ironman, liveEvent).Error?.Code.ShouldBe(refusal);

    [Fact]
    public void Continue_is_the_most_recently_played_save_that_opens()
    {
        var now = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
        List<SaveCard> cards =
        [
            Card("newer app", SaveCardStatus.FromNewerVersion, now.AddMinutes(30)),
            Card("upgrade", SaveCardStatus.NeedsUpgrade, now.AddMinutes(20)),
            Card("ready", SaveCardStatus.Ready, now.AddMinutes(10)),
            Card("unreadable", SaveCardStatus.Unreadable, default),
        ];

        cards.ContinueTarget()!.LeagueName.ShouldBe("upgrade");
        cards.Where(c => c.Status is SaveCardStatus.FromNewerVersion or SaveCardStatus.Unreadable).ContinueTarget().ShouldBeNull();
    }

    // M1 WP8 (guide §4): the name rule New League checks before it generates, with M0's two messages word for word
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t  ")]
    public void A_blank_name_is_refused(string? name) =>
        SaveRules.CheckLeagueName(name).Error.ShouldBe(new DomainError("InvalidLeagueName", "A league needs a name."));

    [Fact]
    public void A_name_is_trimmed_and_may_be_sixty_characters_but_not_sixty_one()
    {
        var sixty = new string('x', SaveRules.MaxLeagueNameLength);

        SaveRules.MaxLeagueNameLength.ShouldBe(60);
        SaveRules.CheckLeagueName("  Saturday League ").Value.ShouldBe("Saturday League");
        SaveRules.CheckLeagueName($"  {sixty}  ").Value.ShouldBe(sixty, "The length is counted after the trim.");
        SaveRules.CheckLeagueName(sixty + "y").Error
            .ShouldBe(new DomainError("InvalidLeagueName", "League names are at most 60 characters."));
    }

    private static SaveCard Card(string name, SaveCardStatus status, DateTime lastPlayed) =>
        new($@"C:\Saves\{name}.gfo", status) { LeagueName = name, LastPlayedAtUtc = lastPlayed };
}
