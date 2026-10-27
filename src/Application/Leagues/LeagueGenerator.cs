using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Core.Random;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

namespace GridironFrontOffice.Application.Leagues;

public sealed class LeagueGenerator(ISeedData data, ILogger<LeagueGenerator> logger, TimeProvider time)
{
	private static readonly Position[] Positions = Enum.GetValues<Position>();

	private readonly PlayerGenerator _players = new(data);
	private readonly PersonNames _names = new(data.FirstNames, data.LastNames, data.FamousPeople);
	private readonly Dictionary<Position, PositionProfile> _profiles = data.Positions.Positions.ToDictionary(p => p.Position);

	public GeneratedLeague Generate(ulong seed, int season)
	{
		var started = time.GetTimestamp();
		var teams = data.TeamSet.Teams;

		var counts = RosterMix
	}

	private void RosterMix(ulong seed, int season, int teamCount)
	{
		// Only half the teams because we are doing k+16
		for (int k = 0; k < teamCount / 2; k++)
		{
			var random = new GfoRandom(Seeds.Derive(seed, SeedPurpose.RosterMix, season, teamCount));
		}
	}
}