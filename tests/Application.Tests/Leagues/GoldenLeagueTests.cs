using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Players;
using Microsoft.Extensions.Logging.Abstractions;

namespace GridironFrontOffice.Application.Tests.Leagues;

/// <summary>
/// The golden league (M1 WP4 guide §5): seed 42's 2026 league as a summary, against its approved snapshot (D-085). It
/// catches what the rule tests can't: any change to what generation draws, or how it uses the draws. Approve a new
/// snapshot only for a change you meant, and expect one after a .NET update (`Math.Pow` and friends can move a last bit).
/// </summary>
public sealed class GoldenLeagueTests
{
	private static readonly ISeedData Data = ShippedSeedData.Instance;
	private static readonly Dictionary<Position, PositionProfile> Profiles = Data.Positions.Positions.ToDictionary(p => p.Position);

	[Fact]
	public void Seed_42_matches_its_approved_summary()
	{
		var league = new LeagueGenerator(Data, NullLogger<LeagueGenerator>.Instance, TimeProvider.System).Generate(42, GeneratedLeagues.Season);

		Snapshot.Match(Summary(league), "Leagues/GoldenLeagueTests.Seed42");
	}

	/// <summary>
	/// §5's summary: the league's player count, 90+ count and mean age; per team its abbreviation, strength, position
	/// counts, Starters' mean OVR, cap total and space; the five highest cap hits; team one's first five players in full.
	/// </summary>
	private static object Summary(GeneratedLeague league)
	{
		var everyone = league.Teams.SelectMany(t => t.Players).Concat(league.FreeAgents).ToList();
		var cap = Data.Contracts.Cap;

		return new
		{
			league.Seed,
			league.Season,
			Players = everyone.Count,
			At90Plus = everyone.Count(p => OverallCalculator.Display(Ovr(p)) >= 90),
			MeanAge = Round(everyone.Average(p => p.Age)),
			Teams = league.Teams.Select(t => new
			{
				t.Definition.Abbr,
				t.Strength,
				Positions = Enum.GetValues<Position>().ToDictionary(position => position, position => t.Players.Count(p => p.Position == position)),
				StartersOvr = Round(t.Players.Where(p => p.Tier == PlayerTier.Starter).Average(Ovr)),
				CapTotal = t.Players.Sum(CapHit),
				CapSpace = cap - t.Players.Sum(CapHit),
			}),
			TopCapHits = league.Teams
				.SelectMany(t => t.Players.Select(p => (Team: t.Definition.Abbr, Player: p)))
				.OrderByDescending(x => CapHit(x.Player))
				.ThenBy(x => x.Player.Slot)
				.Take(5)
				.Select(x => new { Name = $"{x.Player.FirstName} {x.Player.LastName}", x.Team, x.Player.Position, x.Player.Contract!.Type, CapHit = CapHit(x.Player) }),
			TeamOne = league.Teams[0].Players.Take(5).Select(InFull),
		};

		long CapHit(GeneratedPlayer player) => player.Contract!.Seasons.Single(s => s.Season == league.Season).CapHit;
	}

	/// <summary>Every field, with the team, college and hometown records shortened to what names them.</summary>
	private static object InFull(GeneratedPlayer p) => new
	{
		p.Slot,
		p.Position,
		p.Tier,
		p.Archetype,
		p.FirstName,
		p.LastName,
		p.BirthDate,
		p.Age,
		p.Experience,
		College = p.College.Name,
		Hometown = $"{p.Hometown.Name}, {p.Hometown.State}",
		Draft = p.Draft is null ? null : new { p.Draft.Season, p.Draft.Round, p.Draft.Pick, Team = p.Draft.Team.Abbr },
		p.JerseyNumber,
		p.Measurables,
		p.Ratings,
		p.Hidden,
		p.Personality,
		p.Contract,
	};

	private static double Ovr(GeneratedPlayer player) => OverallCalculator.Overall(Profiles[player.Position], player.Ratings);

	private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
