using GridironFrontOffice.Core.Results;

namespace GridironFrontOffice.Application.Saves;

/// <summary>The rules that guard save management. SaveManager applies them, so they hold whatever the UI shows.</summary>
public static class SaveRules
{
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
}
