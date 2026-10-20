using GridironFrontOffice.Application.Saves;

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

    private static SaveCard Card(string name, SaveCardStatus status, DateTime lastPlayed) =>
        new($@"C:\Saves\{name}.gfo", status) { LeagueName = name, LastPlayedAtUtc = lastPlayed };
}
