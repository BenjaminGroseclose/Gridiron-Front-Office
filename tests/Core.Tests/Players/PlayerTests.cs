using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Core.Tests.Players;

/// <summary>
/// M1 WP6 §4.2: <see cref="Player.IsUndrafted"/>. Its [MemberNotNullWhen(false, …)] lets a caller read the four draft
/// columns' values after !IsUndrafted, and the compiler doesn't check the body against it, so this test does.
/// </summary>
public sealed class PlayerTests
{
	/// <summary>Every combination of the four draft columns, set or null: bit 0 the season, 1 the round, 2 the pick, 3 the team.</summary>
	public static TheoryData<int> Combinations { get; } = new(Enumerable.Range(0, 16));

	[Theory]
	[MemberData(nameof(Combinations))]
	public void A_player_is_drafted_only_when_all_four_draft_columns_are_set(int set)
	{
		var player = new Player
		{
			DraftSeason = (set & 1) != 0 ? 2022 : null,
			DraftRound = (set & 2) != 0 ? 3 : null,
			DraftPick = (set & 4) != 0 ? 71 : null,
			DraftTeamID = (set & 8) != 0 ? new TeamID(12) : null,
		};

		player.IsUndrafted.ShouldBe(set != 15, $"Set: {Describe(set)}.");
	}

	private static readonly string[] Columns = ["season", "round", "pick", "team"];

	private static string Describe(int set) =>
		string.Join(", ", Columns.Where((_, bit) => (set & (1 << bit)) != 0).DefaultIfEmpty("none"));
}
