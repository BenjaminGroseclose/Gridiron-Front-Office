namespace GridironFrontOffice.Core.Random;

/// <summary>
/// A normal distribution, clamped to <see cref="Min"/>–<see cref="Max"/> when they're given. The data files use it
/// (ages, measurables, hidden ratings, personality); <see cref="GfoRandom.Draw"/> draws from it.
/// </summary>
public sealed record Distribution(double Mean, double Sd, double? Min = null, double? Max = null);
