namespace GridironFrontOffice.Domain.Helpers;

public static class ContractHelpers
{

	// Anchors for 2026: (First Pick Value, Last Pick Value, Total Picks in Round)
	private static readonly Dictionary<int, RoundBounds> RoundMap = new()
	{
		{ 1, new RoundBounds(1, 32, 54565500m, 16168614m) },
		{ 2, new RoundBounds(33, 64, 12937488m, 8550250m) },
		{ 3, new RoundBounds(65, 100, 7150000m, 6215000m) },
		{ 4, new RoundBounds(101, 135, 5850000m, 5100000m) },
		{ 5, new RoundBounds(136, 175, 5050000m, 4850000m) },
		{ 6, new RoundBounds(176, 220, 4800000m, 4550000m) },
		{ 7, new RoundBounds(221, 257, 4450000m, 4320000m) }
	};

	public static decimal GetRookieContractValue(int pick)
	{
		// Find the round this pick belongs to
		var roundData = RoundMap.Values.FirstOrDefault(r => pick >= r.StartPick && pick <= r.EndPick);

		// If pick is out of bounds (UDFA), return the standard 3-year min estimate
		if (roundData == null)
		{
			return 3950000m;
		}

		// 1. Calculate the 'progress' through the round (0.0 to 1.0)
		double progress = (double)(pick - roundData.StartPick) / (roundData.EndPick - roundData.StartPick);

		// 2. Linear Interpolation: StartValue - (TotalRoundDrop * progress)
		decimal totalDrop = roundData.StartValue - roundData.EndValue;
		decimal interpolatedValue = roundData.StartValue - (totalDrop * (decimal)progress);

		return Math.Round(interpolatedValue, 0);
	}


	private class RoundBounds
	{
		public int StartPick, EndPick;
		public decimal StartValue, EndValue;
		public RoundBounds(int s, int e, decimal sv, decimal ev)
		{
			StartPick = s;
			EndPick = e;
			StartValue = sv;
			EndValue = ev;
		}
	}
}