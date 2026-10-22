namespace GridironFrontOffice.Core.Players;

// The three enums the player data files key on (M1 WP3). Values are written to the files as their C# names, so
// renaming one is a data change too.

/// <summary>The combine numbers stored for every player (F-03 §4.3). Units: inches, pounds, seconds, reps of 225 lb.</summary>
public enum Measurable
{
    Height, Weight, Forty, TenSplit, ThreeCone, Shuttle, Bench, Vertical, BroadJump, ArmLength, HandSize,
}

/// <summary>Where a generated player sits, worst to best (F-03 §4.10): Fringe is the unsigned pool.</summary>
public enum PlayerTier
{
    Fringe, Backup, Starter,
}

/// <summary>When a player peaks relative to the position's curve (F-03 §4.6; read by F-20 in M5).</summary>
public enum DevelopmentCurve
{
    Early, Normal, Late,
}
