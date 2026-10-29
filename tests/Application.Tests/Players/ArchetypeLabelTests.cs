using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Application.Tests.Players;

/// <summary>
/// The archetype label (M1 WP4 guide §4.1): a player built exactly at an archetype's expected profile gets that label,
/// however good or poor he is, because the label removes his overall level first.
/// </summary>
public sealed class ArchetypeLabelTests
{
	private static readonly IReadOnlyList<PositionProfile> Positions = ShippedSeedData.Instance.Positions.Positions;
	private static readonly SkillSettings Skills = ShippedSeedData.Instance.Generation.Skills;

	[Theory]
	[InlineData(-10.0)]
	[InlineData(0.0)]
	[InlineData(15.0)]
	public void A_player_built_at_an_archetypes_profile_gets_its_label(double level)
	{
		var offenders =
			from profile in Positions
			from archetype in profile.Archetypes
			let label = OverallCalculator.Archetype(profile, Skills, Expected(archetype, level))
			where label != archetype.Name
			select $"{profile.Position} {archetype.Name} at level {level}: labeled {label}";

		offenders.ShouldBeEmpty();
	}

	/// <summary>
	/// The archetype's own means, else the common ones, moved by <paramref name="level"/>; off-position skills at their
	/// mean with no level. Not clamped: this is the reference profile, not a generated player.
	/// </summary>
	private static Dictionary<PlayerAttribute, double> Expected(Archetype archetype, double level) =>
		Enum.GetValues<PlayerAttribute>().ToDictionary(a => a, a =>
			archetype.Skills.TryGetValue(a, out var mean) ? mean + level
			: Skills.Common.TryGetValue(a, out var common) ? common + level
			: Skills.OffPosition.Mean);
}
