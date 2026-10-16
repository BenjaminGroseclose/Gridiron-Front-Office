namespace GridironFrontOffice.Spikes.S0a.EfProof.Domain;

/// <summary>D-015: integer keys wrapped in strongly typed IDs, mapped by one EF convention (Data/IdConverter.cs).</summary>
internal interface IStronglyTypedId
{
    int Value { get; }
}

internal readonly record struct TeamId(int Value) : IStronglyTypedId;

internal readonly record struct PlayerId(int Value) : IStronglyTypedId;

internal readonly record struct ContractId(int Value) : IStronglyTypedId;

internal readonly record struct GameId(int Value) : IStronglyTypedId;
