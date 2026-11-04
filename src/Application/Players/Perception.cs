using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Application.Players;

/// <summary>What perception needs of a player: who he is, whose he is, and his true ratings.</summary>
public sealed record PlayerSnapshot(
	PlayerID PlayerID,
	TeamID? TeamID,
	Position Position,
	IReadOnlyDictionary<PlayerAttribute, double> Attributes,
	double Potential,
	IReadOnlyDictionary<PersonalityTrait, double> Personality);

/// <summary>A rating as a viewer sees it. Sigma 0 means exact (M1–M5).</summary>
public readonly record struct PerceivedRating(double Estimate, double Sigma);

/// <summary>
/// A roster's strength as a viewer sees it (D-092 #3): the starters' mean OVR on offense, on defense, and over all 25
/// (K, P and LS count only in <paramref name="Overall"/>).
/// </summary>
public readonly record struct PerceivedRosterRating(PerceivedRating Offense, PerceivedRating Defense, PerceivedRating Overall);

/// <summary>A player through one viewer's eyes. <paramref name="Personality"/> is null when the viewer can't see it.</summary>
public sealed record PerceivedPlayer(
	PlayerID PlayerID,
	IReadOnlyDictionary<PlayerAttribute, PerceivedRating> Attributes,
	PerceivedRating Overall,
	PerceivedRating Potential,
	IReadOnlyDictionary<PersonalityTrait, TraitLevel>? Personality);

/// <summary>The fog-of-war boundary (F-03 §4.9, D-032). The sim never calls it.</summary>
public interface IPerception
{
	/// <summary>The user's team for screens, an AI team for its own decisions; null is an outsider, before the pick (D-092).</summary>
	PerceivedPlayer Perceive(TeamID? viewer, PlayerSnapshot player);

	/// <summary>The strength of one team's <paramref name="players"/> through the viewer's eyes, built from <see cref="Perceive"/>.</summary>
	PerceivedRosterRating PerceiveRoster(TeamID? viewer, IReadOnlyList<PlayerSnapshot> players);
}

public sealed class Perception(ISeedData data) : IPerception
{
	private readonly Dictionary<Position, PositionProfile> _profiles = data.Positions.Positions.ToDictionary(p => p.Position);
	private readonly PersonalitySettings _personality = data.Generation.Personality;

	public PerceivedPlayer Perceive(TeamID? viewer, PlayerSnapshot player)
	{
		ArgumentNullException.ThrowIfNull(player);

		var sigma = 0.0;

		var attributes = player.Attributes.ToDictionary(a => a.Key, a => new PerceivedRating(a.Value, sigma));
		var overall = new PerceivedRating(OverallCalculator.Overall(_profiles[player.Position], player.Attributes), sigma);
		var potential = new PerceivedRating(player.Potential, sigma);

		// D-092: only the viewer's own players show personality. Free agents and an outsider (null) see none
		var personality = viewer is not null && viewer == player.TeamID
			? player.Personality.ToDictionary(t => t.Key, t => Level(t.Value))
			: null;

		return new PerceivedPlayer(
			player.PlayerID,
			attributes,
			overall,
			potential,
			personality
		);
	}

	public PerceivedRosterRating PerceiveRoster(TeamID? viewer, IReadOnlyList<PlayerSnapshot> players)
	{
		ArgumentNullException.ThrowIfNull(players);

		var starters = new List<(SideOfBall Side, PerceivedRating Overall)>();

		foreach (var (position, profile) in _profiles)
		{
			// The position's best players as the viewer sees them, up to its starters count (positions.json)
			var best = players
				.Where(p => p.Position == position)
				.Select(p => Perceive(viewer, p).Overall)
				.OrderByDescending(o => o.Estimate)
				.Take(profile.Roster.Starters);

			starters.AddRange(best.Select(o => (position.Side(), o)));
		}

		return new PerceivedRosterRating(
			Mean(starters.Where(s => s.Side == SideOfBall.Offense).Select(s => s.Overall)),
			Mean(starters.Where(s => s.Side == SideOfBall.Defense).Select(s => s.Overall)),
			Mean(starters.Select(s => s.Overall)));
	}

	/// <summary>The mean estimate and the mean sigma, or (0, 0) with nothing to average.</summary>
	private static PerceivedRating Mean(IEnumerable<PerceivedRating> ratings)
	{
		var list = ratings.ToList();
		return list.Count == 0
			? new PerceivedRating(0, 0)
			: new PerceivedRating(list.Average(r => r.Estimate), list.Average(r => r.Sigma));
	}

	private TraitLevel Level(double value) =>
		value < _personality.LowBelow ? TraitLevel.Low
		: value > _personality.HighAbove ? TraitLevel.High
		: TraitLevel.Average;
}
