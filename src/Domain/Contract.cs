using GridironFrontOffice.Domain.Enums;

namespace GridironFrontOffice.Domain;

/// <summary>
/// A players contract with a team. This allows us to track the player's current team 
/// affiliation, contract type, status, and financial details. A player can have multiple 
/// contracts over their career if they switch teams, so this entity helps us manage the 
/// player's career history and current status with the team. The contract also includes a 
/// yearly breakdown of the player's salary and bonuses, which is important for managing 
/// the team's salary cap and understanding the financial implications of the contract over time.
/// </summary>
public class Contract : BaseEntity
{
	public int ContractID { get; set; }

	/// <summary>
	/// The ID of the player associated with this contract. This is a foreign key that links to the Player entity, allowing us to identify which player is under this contract.
	/// </summary>
	public int PlayerID { get; set; }

	public Player Player { get; set; }

	public bool IsCurrent => ContractStatus == ContractStatus.Active;

	/// <summary>
	/// The contract type. <see cref="ContractType"/>
	/// </summary>
	public ContractType ContractType { get; set; }

	/// <summary>
	/// The status of this contract. <see cref="ContractStatus"/> 
	/// </summary>
	public ContractStatus ContractStatus { get; set; }

	public int StartYear { get; set; }
	public int EndYear { get; set; }

	/// <summary>
	/// The date contract was signed. This allows us to track when the player officially 
	/// joined the team under this contract, which can be important for managing contract 
	/// durations, salary cap implications, and player eligibility. The SignedDate is a 
	/// key piece of information for understanding the timeline of a player's career and 
	/// their relationship with the team.
	/// </summary>
	public DateTime SignedDate { get; set; }

	/// <summary>
	/// Yearly breakdown of the contract. This allows us to track the salary and other 
	/// details for each year of the contract, which is important for managing the team's 
	/// salary cap and understanding the financial implications of the contract over time. 
	/// Each ContractYear entry corresponds to a specific year within the contract's duration, 
	/// providing a detailed view of the player's compensation and contract terms on a 
	/// year-by-year basis.
	/// </summary>
	public IEnumerable<ContractYear> YearlyBreakdown { get; set; }

	public int TermLength => EndYear - StartYear + 1;

	public decimal Salary => YearlyBreakdown.Sum(cy => cy.BaseSalary + cy.SigningBonus);

	public decimal GuaranteedMoney => YearlyBreakdown.Sum(cy => cy.GuaranteedMoney);

	public Team? CurrentTeam => YearlyBreakdown.FirstOrDefault(cy => cy.IsCurrent)?.Team;

	/// <summary>
	/// Gets the ContractYear for a specific year of the contract. This allows us to retrieve 
	/// the details of the contract for a particular year, such as the base salary, bonus earnings, 
	/// and guaranteed money for that year. This is useful for analyzing the 
	/// financial implications of the contract on a year-by-year basis and for 
	/// managing the team's salary cap effectively.
	/// </summary>
	/// <param name="year">The year</param>
	/// <returns>The contract year or null if not found</returns>
	public ContractYear? GetYear(int year)
	{
		var contractYear = YearlyBreakdown.FirstOrDefault(cy => cy.SeasonID == year);
		return contractYear;
	}

	public override int ID => ContractID;
}