using GridironFrontOffice.Core.Results;

namespace GridironFrontOffice.Application.Saves;

/// <summary>The rules that guard save management. SaveManager applies them, so they hold whatever the UI shows.</summary>
public static class SaveRules
{
	/// <summary>The longest league name, which also sizes the New League field.</summary>
	public const int MaxLeagueNameLength = 60;

	/// <summary>
	/// Going back to a snapshot is refused in Ironman leagues (D-026) and while a live event runs, because otherwise
	/// you could restore before every pick and keep the best timeline (PERSISTENCE.md §2.2).
	/// </summary>
	public static Result<Unit> CanRestore(bool isIronman, bool liveEventRunning)
	{
		if (isIronman)
		{
			return Result.Fail<Unit>(SaveErrors.IronmanNoRestore);
		}

		return liveEventRunning ? Result.Fail<Unit>(SaveErrors.LiveEventRunning("Restore")) : Result.Ok();
	}

	/// <summary>
	/// A league's name, trimmed, or why it's refused. SaveManager applies it, and New League checks it before it
	/// generates, so both say the same thing.
	/// </summary>
	public static Result<string> CheckLeagueName(string? name)
	{
		var trimmed = name?.Trim() ?? "";
		if (trimmed.Length == 0)
		{
			return Result.Fail<string>(SaveErrors.InvalidName("A league needs a name."));
		}

		return trimmed.Length > MaxLeagueNameLength
			? Result.Fail<string>(SaveErrors.InvalidName($"League names are at most {MaxLeagueNameLength} characters."))
			: Result.Ok(trimmed);
	}
}
