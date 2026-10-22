using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Application.Players;

/// <summary>
/// OVR, its display, the potential grade and the archetype label (F-03 §4.5–4.6). One yardstick per position: nothing
/// here takes the player's archetype, and the data keeps the archetypes fair (D-084).
/// </summary>
public static class OverallCalculator
{
    /// <summary>OVR on the internal 0–100 scale: Σ wᵢ · attributeᵢ over the position's weights, in the file's order (F-03 §4.5).</summary>
    public static double Overall(PositionProfile profile, IReadOnlyDictionary<PlayerAttribute, double> ratings)
    {
        var overall = 0.0;

        foreach (var (attribute, weight) in profile.OvrWeights)
        {
            overall += ratings[attribute] * weight;
        }

        return overall;
    }

    /// <summary>The 0–99 integer screens show (D-034): rounded half away from zero, within 0–99.</summary>
    public static int Display(double rating)
    {
        return Math.Clamp((int)Math.Round(rating, MidpointRounding.AwayFromZero), 0, 99);
    }

    /// <summary>The grade of a peak OVR: the first band, best first, whose MinPeakOvr the displayed peak reaches.</summary>
    public static PotentialGrade Grade(double peakOverall, IReadOnlyList<GradeBand> bands)
    {
        var shown = Display(peakOverall);
        return bands.FirstOrDefault(b => shown >= b.MinPeakOvr)?.Grade
            ?? throw new ArgumentOutOfRangeException(nameof(peakOverall), "No grade band for this peak OVR");
    }

    /// <summary>The archetype the ratings fit best (F-03 §4.5): recomputed, never stored, so it stays true after development.</summary>
    public static string Archetype(PositionProfile profile, SkillSettings skills, IReadOnlyDictionary<PlayerAttribute, double> ratings)
    {
        var union = profile.Archetypes.SelectMany(a => a.Skills.Keys).Distinct().ToList();
        var bestMisfit = double.MaxValue;
        var bestArchetype = string.Empty;

        foreach (var archetype in profile.Archetypes)
        {
            var level = archetype.Skills.Average(s => ratings[s.Key] - s.Value);
            var misfit = 0.0;

            foreach (var skill in union)
            {
                var expected = archetype.Skills.TryGetValue(skill, out var mean) ? mean + level
                    : skills.Common.TryGetValue(skill, out var common) ? common + level
                    : skills.OffPosition.Mean;   // off-position skills get no level
                var miss = ratings[skill] - expected;
                misfit += miss * miss;
            }

            if (misfit < bestMisfit)
            {
                bestMisfit = misfit;
                bestArchetype = archetype.Name;
            }
        }

        return bestArchetype;
    }
}
