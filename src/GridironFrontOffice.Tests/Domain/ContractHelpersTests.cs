using GridironFrontOffice.Domain.Enums;
using GridironFrontOffice.Domain.Helpers;
using Xunit;

namespace GridironFrontOffice.Tests.Domain;

public class ContractHelpersTests
{
	// -------------------------------------------------------------------------
	// GetRookieContractValue
	// -------------------------------------------------------------------------

	[Fact]
	public void GetRookieContractValue_FirstOverallPick_ReturnsMaxRound1Value()
	{
		// The #1 pick should be worth approximately $54.5M (the round 1 start anchor)
		var value = ContractHelpers.GetRookieContractValue(1);

		Assert.Equal(54565500m, value);
	}

	[Fact]
	public void GetRookieContractValue_LastRound1Pick_ReturnsMinRound1Value()
	{
		// Pick #32 is the floor of round 1
		var value = ContractHelpers.GetRookieContractValue(32);

		Assert.Equal(16168614m, value);
	}

	[Fact]
	public void GetRookieContractValue_FirstRound2Pick_IsLessThanLastRound1Pick()
	{
		// There should be a notable drop when crossing from round 1 to round 2
		var lastRound1 = ContractHelpers.GetRookieContractValue(32);
		var firstRound2 = ContractHelpers.GetRookieContractValue(33);

		Assert.True(firstRound2 < lastRound1,
			$"First round 2 pick ({firstRound2:C0}) should be less than last round 1 pick ({lastRound1:C0})");
	}

	[Fact]
	public void GetRookieContractValue_PicksDeclineWithinRound()
	{
		// Later picks within a round should be worth less than earlier picks
		var pick10 = ContractHelpers.GetRookieContractValue(10);
		var pick20 = ContractHelpers.GetRookieContractValue(20);

		Assert.True(pick20 < pick10,
			$"Pick 20 ({pick20:C0}) should be worth less than pick 10 ({pick10:C0})");
	}

	[Fact]
	public void GetRookieContractValue_UndraftedPlayer_ReturnsUdfaFloor()
	{
		// A pick outside all rounds (e.g. 999) is an undrafted free agent
		var value = ContractHelpers.GetRookieContractValue(999);

		Assert.Equal(ContractHelpers.UndraftedFreeAgentValue, value);
	}

	[Fact]
	public void GetRookieContractValue_LateRoundPick_IsInExpectedRange()
	{
		// A 7th-round pick should be in a modest but above-UDFA range
		var pick240 = ContractHelpers.GetRookieContractValue(240);

		Assert.InRange(pick240, 4320000m, 4450000m);
	}

	// -------------------------------------------------------------------------
	// GetMinimumSalaryForPosition
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(0, 885000)]
	[InlineData(1, 1005000)]
	[InlineData(2, 1080000)]
	[InlineData(3, 1155000)]
	[InlineData(6, 1155000)]
	[InlineData(7, 1330000)]
	[InlineData(15, 1330000)]
	public void GetMinimumSalaryForPosition_ReturnsCorrectBracket(int experienceYears, decimal expected)
	{
		var salary = ContractHelpers.GetMinimumSalaryForPosition(experienceYears);

		Assert.Equal(expected, salary);
	}

	[Fact]
	public void GetMinimumSalaryForPosition_IncreasesWithExperience()
	{
		var rookie = ContractHelpers.GetMinimumSalaryForPosition(0);
		var year1 = ContractHelpers.GetMinimumSalaryForPosition(1);
		var year2 = ContractHelpers.GetMinimumSalaryForPosition(2);
		var veteran = ContractHelpers.GetMinimumSalaryForPosition(7);

		Assert.True(rookie < year1 && year1 < year2 && year2 < veteran);
	}

	// -------------------------------------------------------------------------
	// GetBaselineContractValue — realistic salary scenarios
	// -------------------------------------------------------------------------

	[Fact]
	public void GetBaselineContractValue_EliteQBInPrime_IsTopOfMarket()
	{
		// An elite QB (OVR 95, 8 years experience) should command a top-of-market salary.
		// Exponential curve: e^(0.07 × 25) ≈ 5.75× → ~$35.9M, capped at 7× = $43.75M
		var salary = ContractHelpers.GetBaselineContractValue(
			overallRating: 95,
			potentialRating: 90,
			experienceYears: 8,
			position: PlayerPosition.QB);

		Assert.InRange(salary, 30_000_000m, 43_750_000m);
	}

	[Fact]
	public void GetBaselineContractValue_AverageStarterInPrime_IsNearLeagueAverage()
	{
		// An average starting center (OVR 75, 5 years) should land around league average.
		var salary = ContractHelpers.GetBaselineContractValue(
			overallRating: 75,
			potentialRating: 70,
			experienceYears: 5,
			position: PlayerPosition.C);

		Assert.InRange(salary, 2_500_000m, 7_500_000m);
	}

	[Fact]
	public void GetBaselineContractValue_DeclinedRB_IsLowerThanPrime()
	{
		// A running back past his prime (OVR 70, 7 years) should earn less than he did at peak.
		var peak = ContractHelpers.GetBaselineContractValue(75, 75, 3, PlayerPosition.RB);
		var decline = ContractHelpers.GetBaselineContractValue(70, 70, 7, PlayerPosition.RB);

		Assert.True(decline < peak,
			$"Declining RB ({decline:C0}) should earn less than the same RB at peak ({peak:C0})");
	}

	[Fact]
	public void GetBaselineContractValue_LowRatedPlayer_ClampsToMinimumSalary()
	{
		// A replacement-level player (OVR 45) should not be paid below the league minimum.
		var experienceYears = 2;
		var salary = ContractHelpers.GetBaselineContractValue(
			overallRating: 45,
			potentialRating: 40,
			experienceYears: experienceYears,
			position: PlayerPosition.LB);

		var minimum = ContractHelpers.GetMinimumSalaryForPosition(experienceYears);
		Assert.True(salary >= minimum,
			$"Salary ({salary:C0}) must not fall below the minimum ({minimum:C0})");
	}

	[Fact]
	public void GetBaselineContractValue_QBIsHigherThanEquivalentRB()
	{
		// Given identical ratings and experience, a QB should always command more than a RB.
		var qbSalary = ContractHelpers.GetBaselineContractValue(80, 75, 5, PlayerPosition.QB);
		var rbSalary = ContractHelpers.GetBaselineContractValue(80, 75, 5, PlayerPosition.RB);

		Assert.True(qbSalary > rbSalary,
			$"QB ({qbSalary:C0}) should out-earn an equal-rated RB ({rbSalary:C0})");
	}
}
