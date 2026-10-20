using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Core.Tests.Players;

public sealed class PlayerEnumTests
{
    [Fact]
    public void There_are_42_attributes_and_16_positions()
    {
        // D-050 (every attribute has a sim consumer) and F-03 §4.1-4.2 are written against these counts.
        Enum.GetValues<PlayerAttribute>().Length.ShouldBe(42);
        Enum.GetValues<Position>().Length.ShouldBe(16);
    }

    [Fact]
    public void A_position_name_is_its_football_abbreviation()
    {
        Enum.GetValues<Position>().Select(p => p.ToString()).ShouldBe(
            ["QB", "RB", "FB", "WR", "TE", "OT", "OG", "C", "EDGE", "DT", "LB", "CB", "S", "K", "P", "LS"]);
    }

    [Fact]
    public void Grades_run_worst_to_best_so_comparisons_read_naturally()
    {
        Enum.GetValues<PotentialGrade>().ShouldBe(
        [
            PotentialGrade.F, PotentialGrade.D, PotentialGrade.CMinus, PotentialGrade.C, PotentialGrade.CPlus,
            PotentialGrade.BMinus, PotentialGrade.B, PotentialGrade.BPlus, PotentialGrade.AMinus, PotentialGrade.A,
            PotentialGrade.APlus,
        ]);
        (PotentialGrade.BPlus >= PotentialGrade.B).ShouldBeTrue();
        (PotentialGrade.CPlus >= PotentialGrade.B).ShouldBeFalse();
    }
}
