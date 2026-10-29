using GridironFrontOffice.Application.Contracts;
using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Core.Random;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.Application.Leagues;

public sealed class LeagueGenerator(ISeedData data, ILogger<LeagueGenerator> logger, TimeProvider time)
{
	private static readonly Position[] Positions = Enum.GetValues<Position>();

	private readonly PlayerGenerator _players = new(data);
	private readonly PersonNames _names = new(data.FirstNames, data.LastNames, data.FamousPeople);
	private readonly Dictionary<Position, PositionProfile> _profiles = data.Positions.Positions.ToDictionary(p => p.Position);
	private readonly ContractGenerator _contracts = new(data.Contracts, data.TeamSet.Teams.Count);

	// Each position's allowed jersey numbers, ascending, the positions with the fewest first (LS, QB, the line …), then
	// in enum order. Single throws here if a position has no rule, or two.
	private readonly (Position Position, int[] Numbers)[] _jerseyOrder = Positions
		.Select(p => (Position: p, Numbers: data.JerseyRules.Rules.Single(r => r.Position == p).Numbers
			.SelectMany(range => Enumerable.Range(range.From, range.To - range.From + 1)).Order().ToArray()))
		.OrderBy(r => r.Numbers.Length)
		.ThenBy(r => r.Position)
		.ToArray();

	public GeneratedLeague Generate(ulong seed, int season)
	{
		var started = time.GetTimestamp();
		var teams = data.TeamSet.Teams;
		int teamCount = teams.Count;

		var counts = RosterMix(seed, season, teamCount);
		var strengths = TeamStrengths(seed, season, teamCount);

		var active = data.Generation.Roster.Active;
		var rosters = new List<List<GeneratedPlayer>>(teams.Count);

		// Generate Rosters for teams
		for (int team = 0; team < teamCount; team++)
		{
			var roster = new List<GeneratedPlayer>(active);

			// Loop through positions
			for (int p = 0; p < Positions.Length; p++)
			{
				var starterCount = _profiles[Positions[p]].Roster.Starters;

				// Create players for this team as position with context of RosterMix

				for (int n = 0; n < counts[team][p]; n++)
				{
					var tier = n < starterCount ? PlayerTier.Starter : PlayerTier.Backup;
					var slot = team * active + roster.Count;
					var generatedPlayer = _players.Generate(seed, season, slot, Positions[p], tier, strengths[team]);
					roster.Add(generatedPlayer);
				}
			}

			rosters.Add(roster);
		}

		var freeAgentCount = this.FreeAgentCounts();
		var freeAgents = new List<GeneratedPlayer>(freeAgentCount.Sum());


		for (int p = 0; p < Positions.Length; p++)
		{
			var countToGenerate = freeAgentCount[p];

			for (int i = 0; i < countToGenerate; i++)
			{
				// 0 team strength because they don't have a team
				var slot = teamCount * active + freeAgents.Count;
				var generatedPlayer = _players.Generate(seed, season, slot, Positions[p], PlayerTier.Fringe, 0);
				freeAgents.Add(generatedPlayer);
			}
		}

		this.DraftClasses(seed, season, rosters, freeAgents);

		this.AssignContracts(seed, season, rosters);

		this.FitTheCap(season, rosters);

		this.AssignJerseys(seed, season, rosters);

		this.AssignNames(seed, season, rosters, freeAgents);

		var playerCount = rosters.Sum(r => r.Count) + freeAgents.Count;
		var elapsedMs = (long)time.GetElapsedTime(started).TotalMilliseconds;
		PlayerGenerationLog.LeagueGenerated(logger, season, seed, playerCount, elapsedMs);

		var generatedTeams = teams.Select((team, t) => new GeneratedTeam(team, strengths[t], rosters[t])).ToList();

		return new GeneratedLeague(seed, season, generatedTeams, freeAgents);
	}

	private int[][] RosterMix(ulong seed, int season, int teamCount)
	{
		var targets = Positions.Select(p => _profiles[p].Roster).ToArray();
		var counts = new int[teamCount][];

		// Setup default for all teams
		for (var t = 0; t < teamCount; t++)
		{
			counts[t] = targets.Select(r => (int)r.Average).ToArray();
		}

		var half = teamCount / 2;
		for (var k = 0; k < half; k++)
		{
			var team = counts[k];
			var partnerTeam = counts[k + half];
			var random = new GfoRandom(Seeds.Derive(seed, SeedPurpose.RosterMix, season, k));

			for (var swap = 0; swap < data.Generation.Roster.MixSwaps; swap++)
			{
				// Get possible ups where we are below the max and the partner is above the min
				var possibleUps = Enumerable.Range(0, Positions.Length)
					.Where(p => team[p] < targets[p].Max && partnerTeam[p] > targets[p].Min).ToList();

				// If no possibilities then continue to next
				if (possibleUps.Count == 0)
				{
					continue;
				}

				// Randomly select a position to increase;
				var up = possibleUps[random.Next(0, possibleUps.Count)];

				// Do the same thing for down except can't be the same position as the up
				var possibleDowns = Enumerable.Range(0, Positions.Length)
					.Where(p => p != up && team[p] > targets[p].Min && partnerTeam[p] < targets[p].Max).ToList();

				// If no possibilities then continue to next
				if (possibleDowns.Count == 0)
				{
					continue;
				}

				// Randomly select a position to decrease;
				var down = possibleDowns[random.Next(0, possibleDowns.Count)];

				// Execute the up and down
				team[up]++;
				team[down]--;
				partnerTeam[up]--;
				partnerTeam[down]++;
			}
		}

		return counts;
	}

	/// <summary>
	/// Each team's place on the ladder (D-082), in the team set's order: evenly spaced, shuffled by the seed.
	/// "How strong is the team relative to the rest of the league"
	/// </summary>
	private double[] TeamStrengths(ulong seed, int season, int teamCount)
	{
		var range = data.Generation.TeamStrength.Range;
		var strengths = new double[teamCount];

		// Set base level
		for (var i = 0; i < teamCount; i++)
		{
			strengths[i] = range * (2 * (i + 0.5) / teamCount - 1);
		}

		var random = new GfoRandom(Seeds.Derive(seed, SeedPurpose.TeamStrength, season));

		for (var i = teamCount - 1; i > 0; i--)
		{
			var j = random.Next(0, i + 1);
			(strengths[i], strengths[j]) = (strengths[j], strengths[i]);
		}

		return strengths;
	}

	/// <summary>
	/// Get the number of free agents in each position.
	/// </summary>
	private int[] FreeAgentCounts()
	{
		var pool = data.Generation.Roster.FreeAgents;
		var active = data.Generation.Roster.Active;
		var shares = Positions.Select(p => pool * (int)_profiles[p].Roster.Average).ToArray();   // the quota is share / active
		var counts = shares.Select(s => s / active).ToArray();

		// Largest remainder; OrderByDescending is stable, so ties stay in enum order
		foreach (var p in Enumerable.Range(0, Positions.Length).OrderByDescending(p => shares[p] % active).Take(pool - counts.Sum()))
		{
			counts[p]++;
		}

		return counts;
	}

	private void DraftClasses(ulong seed, int season, List<List<GeneratedPlayer>> rosters, List<GeneratedPlayer> freeAgents)
	{
		var teams = data.TeamSet.Teams;
		var active = data.Generation.Roster.Active;
		var rosterSlots = teams.Count * active;
		var draft = data.Generation.Draft;
		var rookieYears = data.Contracts.RookieScale.Years;
		var picks = data.Contracts.RookieScale.Rounds.Count * teams.Count;

		var everyone = rosters.SelectMany(x => x).Concat(freeAgents).ToList();

		foreach (var draftClass in everyone.GroupBy(p => season - p.Experience).OrderBy(c => c.Key))
		{
			int entrySeason = draftClass.Key;
			var random = new GfoRandom(Seeds.Derive(seed, SeedPurpose.Draft, season, entrySeason));

			var scored = new List<(GeneratedPlayer Player, double Score)>();

			foreach (var player in draftClass)
			{
				var score = OverallCalculator.Overall(_profiles[player.Position], player.Ratings) + random.Normal(0, draft.RankNoiseSd);

				scored.Add((player, score));
			}

			var ranked = scored.OrderByDescending(x => x.Score).ThenBy(x => x.Player.Slot).Select(x => x.Player).ToList();
			var drafted = Math.Min(picks, (int)Math.Round(draft.DraftedShare * ranked.Count, MidpointRounding.AwayFromZero));
			var classSize = ranked.Count;

			// Not everyone is drafted
			for (int n = 0; n < drafted; n++)
			{
				var player = ranked[n];
				var pick = n * picks / drafted + 1;
				var round = (pick - 1) / teams.Count + 1;
				var onRoster = player.Slot < rosterSlots;

				// On his rookie deal, his own team drafted him; anyone else, a random team, drawn in pick order
				var team = onRoster && player.Experience < rookieYears
					? teams[player.Slot / active]
					: teams[random.Next(0, teams.Count)];

				var withDraft = player with { Draft = new DraftInfo(entrySeason, round, pick, team) };

				if (onRoster)
				{
					rosters[player.Slot / active][player.Slot % active] = withDraft;
				}
				else
				{
					freeAgents[player.Slot - rosterSlots] = withDraft;
				}
			}
		}
	}

	/// <summary>
	/// Step 6 (§4.4): every rostered player's contract. Pay keys on his league-wide rank at his position (F-12 §4.7), so
	/// every rank is set, from all the rosters, before any contract is drawn. Free agents get none.
	/// </summary>
	private void AssignContracts(ulong seed, int season, List<List<GeneratedPlayer>> rosters)
	{
		var contracts = data.Contracts;
		var ranks = new Dictionary<int, (double Rank, PlayerTier Tier)>();

		// Per position, worst to best by OVR, then slot: index i of N ranks i / (N − 1), and the best teams × starters
		// are the contract tier's Starters, whatever their own team's depth chart says
		foreach (var position in rosters.SelectMany(r => r).GroupBy(p => p.Position))
		{
			var ordered = position
				.OrderBy(p => OverallCalculator.Overall(_profiles[p.Position], p.Ratings))
				.ThenBy(p => p.Slot)
				.ToList();
			var firstStarter = ordered.Count - rosters.Count * _profiles[position.Key].Roster.Starters;

			for (var i = 0; i < ordered.Count; i++)
			{
				ranks[ordered[i].Slot] = (i / (double)(ordered.Count - 1), i >= firstStarter ? PlayerTier.Starter : PlayerTier.Backup);
			}
		}

		foreach (var roster in rosters)
		{
			for (var n = 0; n < roster.Count; n++)
			{
				var player = roster[n];
				GeneratedContract contract;

				if (player.Draft is not null && player.Experience < contracts.RookieScale.Years)
				{
					contract = _contracts.Rookie(player.Draft, player.Experience, season);
				}
				else if (player.Draft is null && player.Experience < contracts.Undrafted.Years)
				{
					contract = _contracts.Undrafted(player.Experience, season);
				}
				else
				{
					// Everyone else, a veteran deal that may come back as a minimum one
					var (rank, tier) = ranks[player.Slot];
					var random = new GfoRandom(Seeds.Derive(seed, SeedPurpose.Contract, season, player.Slot));
					var terms = _contracts.DrawVeteranTerms(random, player.Position, rank, tier, player.Age, player.Experience);
					contract = _contracts.Veteran(terms, player.Experience, season);
				}

				roster[n] = player with { Contract = contract };
			}
		}
	}

	/// <summary>
	/// Step 7 (§4.4, D-081 #1): the trades that bring every team inside the cap band, then event 1301 with the teams'
	/// shares of the cap, and a 1302 Warning for each team still outside the band.
	/// </summary>
	private void FitTheCap(int season, List<List<GeneratedPlayer>> rosters)
	{
		var contracts = data.Contracts;
		var trades = CapTrades.Balance(rosters, season, contracts);
		var totals = rosters.Select(r => r.Sum(p => CapTrades.CapHit(p, season))).ToList();

		var minPercent = Percent(totals.Min());
		var maxPercent = Percent(totals.Max());
		var meanPercent = Percent(totals.Average());
		PlayerGenerationLog.CapFitted(logger, trades, minPercent, maxPercent, meanPercent);

		for (var t = 0; t < totals.Count; t++)
		{
			// The band exactly as CapTrades checks it
			if (totals[t] < contracts.Utilization.Min * contracts.Cap || totals[t] > contracts.Utilization.Max * contracts.Cap)
			{
				var team = data.TeamSet.Teams[t].Abbr;
				var percent = Percent(totals[t]);
				PlayerGenerationLog.TeamOutsideCapBand(logger, team, percent);
			}
		}

		// A total as the events show it: a percentage of the cap, to one decimal
		double Percent(double total) => Math.Round(total * 100 / contracts.Cap, 1, MidpointRounding.AwayFromZero);
	}

	private void AssignNames(ulong seed, int season, List<List<GeneratedPlayer>> rosters, List<GeneratedPlayer> freeAgents)
	{
		foreach (var roster in rosters)
		{
			var taken = new HashSet<string>(PersonNames.FullNames);

			for (var n = 0; n < roster.Count; n++)
			{
				roster[n] = this.Named(seed, season, roster[n], taken);
			}
		}

		for (var n = 0; n < freeAgents.Count; n++)
		{
			freeAgents[n] = this.Named(seed, season, freeAgents[n], new HashSet<string>(PersonNames.FullNames));
		}
	}

	/// <summary>The player with a name that isn't in <paramref name="taken"/>, which then holds it.</summary>
	private GeneratedPlayer Named(ulong seed, int season, GeneratedPlayer player, HashSet<string> taken)
	{
		var random = new GfoRandom(Seeds.Derive(seed, SeedPurpose.Names, season, player.Slot));
		var (first, last) = _names.Pick(random, taken);
		taken.Add($"{first} {last}");

		return player with { FirstName = first, LastName = last };
	}

	/// <summary>
	/// Step 8: each team's numbers. Positions go fewest allowed numbers first, players in roster order, and each player
	/// takes one of his rule's untaken numbers. Free agents get none.
	/// </summary>
	private void AssignJerseys(ulong seed, int season, List<List<GeneratedPlayer>> rosters)
	{
		for (var t = 0; t < rosters.Count; t++)
		{
			var roster = rosters[t];
			var random = new GfoRandom(Seeds.Derive(seed, SeedPurpose.Jerseys, season, t));
			var taken = new HashSet<int>();

			foreach (var (position, numbers) in _jerseyOrder)
			{
				for (var n = 0; n < roster.Count; n++)
				{
					if (roster[n].Position != position)
					{
						continue;
					}

					var free = numbers.Where(x => !taken.Contains(x)).ToList();

					if (free.Count == 0)
					{
						throw new InvalidOperationException($"No jersey number left for a {position} on team {t}: the jersey rules allow too few.");
					}

					var number = free[random.Next(0, free.Count)];
					taken.Add(number);

					// In place: after the cap trades a slot no longer says where a player is
					roster[n] = roster[n] with { JerseyNumber = number };
				}
			}
		}
	}
}