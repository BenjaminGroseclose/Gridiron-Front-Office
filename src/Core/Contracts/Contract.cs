using GridironFrontOffice.Core.IDs;

namespace GridironFrontOffice.Core.Contracts;

// Core/Contracts/Contract.cs (F-12 §5)
public sealed class Contract
{
	public ContractID ContractID { get; set; }
	public PlayerID PlayerID { get; set; }
	public TeamID TeamID { get; set; }
	public ContractType Type { get; set; }
	public int SignedSeason { get; set; }
	public int Years { get; set; }
	public long TotalValue { get; set; }
	public long SigningBonus { get; set; }
	public long GuaranteedTotal { get; set; }
	public bool IsActive { get; set; }                   // every M1 contract is
}

/// <summary>One season of a contract, past seasons included (D-074). In M1, CapHit equals CapMath.CapHit (F-12 §5).</summary>
public sealed class ContractYear
{
	public ContractID ContractID { get; set; }
	public int Season { get; set; }
	public long BaseSalary { get; set; }
	public long ProratedBonus { get; set; }
	public long GuaranteedAmount { get; set; }
	public long CapHit { get; set; }
}