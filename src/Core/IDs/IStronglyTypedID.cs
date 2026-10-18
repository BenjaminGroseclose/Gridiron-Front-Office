namespace GridironFrontOffice.Core.IDs;

/// <summary>
/// Every entity ID is an integer wrapped in its own type (D-015), e.g. <c>readonly record struct PlayerID(int Value)</c>,
/// so a TeamID can never be passed where a PlayerID is expected. One EF Core convention maps them all to INTEGER;
/// declaring the type is the only step a new ID needs.
/// </summary>
public interface IStronglyTypedID
{
    int Value { get; }
}
