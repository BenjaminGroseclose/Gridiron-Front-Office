using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Contracts;
using GridironFrontOffice.Core.Players;
using GridironFrontOffice.Core.Random;

namespace GridironFrontOffice.Application.Contracts;

public sealed class ContractGenerator(ContractData data, int picksPerRound)
{
	public long Minimum(int creditedSeasons)
	{
		return data.MinimumSalaries.Last(m => m.CreditedSeasons <= Math.Max(0, creditedSeasons)).Salary;
	}

	public GeneratedContract Rookie(DraftInfo draft)
	{
		var row = data.RookieScale.Rounds[draft.Round - 1];
		var k = draft.Pick - (draft.Round - 1) * picksPerRound;
		var total = Dollars(row.FirstPick + (row.LastPick - row.FirstPick) * (k - 1.0) / (picksPerRound - 1));
		var bonus = Dollars(total * row.BonusShare);
		var years = data.RookieScale.Years;
		var minimums = Enumerable.Range(0, years).Select(Minimum).ToArray();
		var split = Split(total - bonus - minimums.Sum(), data.RookieScale.BaseWeights);
		var bases = minimums.Select((m, i) => m + split[i]).ToArray();

		return Build(ContractType.Rookie, draft.Season, bases, bonus, row.GuaranteeShare);
	}

	public GeneratedContract Undrafted(int experience, int season)
	{
		var bases = new long[data.Undrafted.Years];
		var bonus = data.Undrafted.Bonus;

		for (int i = 0; i < bases.Length; i++)
		{
			bases[i] = Minimum(i);
		}

		return Build(ContractType.Minimum, season - experience, bases, bonus, 0);
	}

	/// <summary>
	/// A veteran's terms (F-12 §4.7). <paramref name="rank"/> is his league-wide percentile at the position, 0 (the worst)
	/// to 1 (the best). The draws come in a fixed order (the wiggle, the years, the elapsed seasons, the bonus share, the
	/// guarantee share), and changing it changes every league.
	/// </summary>
	public VeteranTerms DrawVeteranTerms(GfoRandom random, Position position, double rank, PlayerTier contractTier, int age, int experience)
	{
		ArgumentNullException.ThrowIfNull(random);
		// A NaN or negative rank makes the APY NaN, which casts to a garbage long instead of failing. The pattern also
		// rejects NaN, which every comparison lets through.
		if (rank is not (>= 0 and <= 1))
		{
			throw new ArgumentOutOfRangeException(nameof(rank), rank, "The rank is a percentile from 0 to 1.");
		}

		var veterans = data.Veterans;
		var wiggle = Math.Clamp(random.Normal(0, veterans.Wiggle.Sd), -veterans.Wiggle.Max, veterans.Wiggle.Max);

		// The first length band his age falls in
		var band = veterans.Lengths[contractTier].First(b => age <= b.ThroughAge);
		var years = random.Next(band.Min, band.Max + 1);

		// Never before his entry, and always with this season or a later one left
		var elapsed = random.Next(0, Math.Min(years - 1, experience) + 1);

		var bonusBand = veterans.BonusShare[contractTier];
		var bonusShare = bonusBand.Min + random.NextDouble() * (bonusBand.Max - bonusBand.Min);
		var guaranteeBand = veterans.GuaranteeShare[contractTier];
		var guaranteeShare = guaranteeBand.Min + random.NextDouble() * (guaranteeBand.Max - guaranteeBand.Min);

		// The curve runs from his minimum to the position's max; the wiggle can take it past either end
		var minimum = Minimum(experience);
		var apy = (minimum + (veterans.MaxApy[position] - minimum) * Math.Pow(rank, veterans.Curve)) * (1 + wiggle);

		return new VeteranTerms(Dollars(apy), years, elapsed, bonusShare, guaranteeShare);
	}

	public GeneratedContract Veteran(VeteranTerms terms, int experience, int season)
	{
		var minimum = Minimum(experience);

		// Within 5% of his minimum: the one-season minimum deal instead (§4.6)
		if (terms.Apy <= 1.05 * minimum)
		{
			return Build(ContractType.Minimum, season, [minimum], 0, 0);
		}

		var total = terms.Apy * terms.Years;
		var bonus = Dollars(total * terms.BonusShare);
		var weights = new List<double>();
		var floors = new long[terms.Years];

		for (int i = 0; i < terms.Years; i++)
		{
			weights.Add(1 + i * data.Veterans.BaseEscalation);
			// His minimum in the deal's season i: the seasons he had when he signed, plus i
			floors[i] = Minimum(experience - terms.Elapsed + i);
		}

		var bases = Split(total - bonus, weights);

		// Every base up to its season's minimum, the raises taken back out of the last season but never below its own
		// minimum, so the total can end up above Apy × Years (F-12 §10)
		var raised = 0L;

		for (int i = 0; i < bases.Length; i++)
		{
			if (bases[i] < floors[i])
			{
				raised += floors[i] - bases[i];
				bases[i] = floors[i];
			}
		}

		bases[^1] = Math.Max(floors[^1], bases[^1] - raised);

		return Build(ContractType.Veteran, season - terms.Elapsed, bases, bonus, terms.GuaranteeShare);
	}

	// Each season but the last gets floor(amount × wᵢ / Σw) and the last gets the remainder, so the parts add up exactly
	private static long[] Split(long amount, IReadOnlyList<double> weights)
	{
		var splits = new long[weights.Count];
		var weightSum = weights.Sum();

		for (int i = 0; i < weights.Count - 1; i++)
		{
			splits[i] = (long)Math.Floor(amount * weights[i] / weightSum);
		}

		splits[^1] = amount - splits.Sum();

		return splits;
	}

	// The "every deal" row of §4.4: the total, the guarantees and each season's proration and cap hit
	private static GeneratedContract Build(ContractType type, int signedSeason, long[] bases, long bonus, double guaranteeShare)
	{
		var total = bonus + bases.Sum();
		var years = bases.Length;
		var left = Math.Max(0, Dollars(total * guaranteeShare) - bonus);
		var seasons = new List<ContractSeason>();

		for (int i = 0; i < years; i++)
		{
			var season = signedSeason + i;
			var guaranteed = Math.Min(bases[i], left);
			left -= guaranteed;
			var proratedBonus = CapMath.ProratedBonus(bonus, years, i);
			var capHit = CapMath.CapHit(bases[i], bonus, years, i);

			var contractSeason = new ContractSeason(season, bases[i], proratedBonus, guaranteed, capHit);
			seasons.Add(contractSeason);
		}

		var guaranteedTotal = bonus + seasons.Sum(x => x.GuaranteedAmount);

		return new GeneratedContract(
			type,
			signedSeason,
			total,
			bonus,
			guaranteedTotal,
			seasons
		);
	}

	// A formula's double becomes money here: whole dollars, rounded once, half away from zero
	private static long Dollars(double amount) => (long)Math.Round(amount, MidpointRounding.AwayFromZero);
}
