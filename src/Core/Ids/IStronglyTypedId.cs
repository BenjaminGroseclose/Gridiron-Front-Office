namespace GridironFrontOffice.Core.Ids;

/// <summary>
/// Every entity ID is an integer wrapped in its own type (D-015), e.g. <c>readonly record struct PlayerId(int Value)</c>,
/// so a TeamId can never be passed where a PlayerId is expected. One EF Core convention maps them all to INTEGER;
/// declaring the type is the only step a new ID needs.
/// </summary>
public interface IStronglyTypedId
{
    int Value { get; }
}
