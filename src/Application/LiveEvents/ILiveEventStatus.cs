namespace GridironFrontOffice.Application.LiveEvents;

/// <summary>
/// Whether a live event (the draft, FA Frenzy, Deadline Day) is running in the open save. Restore, duplicate and
/// export are refused while one is (PERSISTENCE.md §2.2–2.3). The live event runner implements it in M2.
/// </summary>
public interface ILiveEventStatus
{
    bool IsRunning { get; }
}

/// <summary>Until M2 there are no live events, so none is ever running.</summary>
public sealed class NoLiveEvents : ILiveEventStatus
{
    public bool IsRunning => false;
}
