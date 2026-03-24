namespace GridironFrontOffice.Domain;

public class ContractYear : BaseEntity
{
	public int ContractYearID { get; set; }

	public int ContractID { get; set; }
	public Contract Contract { get; set; }

	/// <summary>
	/// The ID of the team associated with this contract year. This is a foreign key that 
	/// links to the Team entity, allowing us to identify which team the player is contracted
	///  to for this specific year.
	/// </summary>
	public int TeamID { get; set; }
	public Team Team { get; set; }

	public int Year { get; set; }

	/// <summary>
	/// The base salary for this contract year. This is the amount the player will 
	/// earn for this year of the contract, not including any bonuses or incentives. 
	/// This allows us to track the player's earnings on a year-by-year basis, which 
	/// is important for salary cap management and financial planning for the team.
	/// </summary>
	public decimal BaseSalary { get; set; }

	/// <summary>
	/// The bonus earnings for this contract year. This includes any signing bonuses, 
	/// roster bonuses, and other performance-based incentives. This allows us to track 
	/// the player's total compensation for the year, which is important for salary cap 
	/// management and financial planning for the team.
	/// </summary>
	public decimal BonusEarnings { get; set; }

	/// <summary>
	/// The guaranteed money for this contract year. This is the portion of the player's earnings
	/// that is guaranteed regardless of performance or roster status. This allows us to track
	/// </summary>
	public decimal GuaranteedMoney { get; set; }

	public bool IsCurrent { get; set; }

	public override int ID => ContractYearID;
}