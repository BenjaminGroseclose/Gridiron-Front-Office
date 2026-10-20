namespace GridironFrontOffice.Core.IDs;

// Every entity ID (D-015), one line each. A new entity adds its ID here; Infrastructure's convention (TypedIDs) finds
// every IStronglyTypedID in Core and stores it as a plain INTEGER column, so nothing else needs registering.
// Logs and messages show them as "PlayerID { Value = 812 }"; use id.Value where only the number should appear.
public readonly record struct TeamID(int Value) : IStronglyTypedID;

public readonly record struct PlayerID(int Value) : IStronglyTypedID;

public readonly record struct ConferenceID(int Value) : IStronglyTypedID;

public readonly record struct DivisionID(int Value) : IStronglyTypedID;

public readonly record struct StadiumID(int Value) : IStronglyTypedID;

public readonly record struct CollegeID(int Value) : IStronglyTypedID;

public readonly record struct ContractID(int Value) : IStronglyTypedID;
