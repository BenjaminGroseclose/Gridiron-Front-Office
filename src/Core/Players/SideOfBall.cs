namespace GridironFrontOffice.Core.Players;

/// <summary>The unit a position plays on: 11 starters on offense, 11 on defense, and K, P and LS on special teams.</summary>
public enum SideOfBall
{
	Offense, Defense, SpecialTeams,
}

public static class PositionSides
{
	/// <summary>The side of the ball <paramref name="position"/> plays on.</summary>
	public static SideOfBall Side(this Position position) => position switch
	{
		Position.QB or Position.RB or Position.FB or Position.WR or Position.TE or Position.OT or Position.OG
			or Position.C => SideOfBall.Offense,
		Position.EDGE or Position.DT or Position.LB or Position.CB or Position.S => SideOfBall.Defense,
		Position.K or Position.P or Position.LS => SideOfBall.SpecialTeams,
		_ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
	};
}
