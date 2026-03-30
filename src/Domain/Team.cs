namespace GridironFrontOffice.Domain;

public class Team : BaseEntity
{
	public int TeamID { get; set; }

	/// <summary>
	/// The name of the team. eg "Lions"
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The city the team is based in. eg "Detroit"
	/// </summary>
	public string City { get; set; }

	/// <summary>
	/// The team's abbreviation. eg "DET"
	/// </summary>
	public string Abbreviation { get; set; }

	/// <summary>
	/// The primary color of the team in hex format. eg "#FF0000"
	/// </summary>
	public string PrimaryColor { get; set; }

	/// <summary>
	/// The secondary color of the team in hex format. eg "#00FF00"
	/// </summary>
	public string SecondaryColor { get; set; }

	/// <summary>
	/// The path to the team's logo image file.
	/// Should be local to the application's base directory.
	/// </summary>
	public string? LogoPath { get; set; }

	/// <summary>
	/// The ID of the conference this team belongs to.
	/// </summary>
	public int ConferenceID { get; set; }

	public Conference Conference { get; set; }

	/// <summary>
	/// The ID of the division this team belongs to.
	/// </summary>
	public int DivisionID { get; set; }

	public Division Division { get; set; }

	/// <summary>
	/// The ID of the stadium this team plays in.
	/// </summary>
	public int StadiumID { get; set; }

	public Stadium Stadium { get; set; }

	/// <summary>
	/// The contracts associated with this team. This allows us to track which players 
	/// are currently under contract with the team, as well as their contract history. 
	/// A player can have multiple contracts over their career if they switch teams, 
	/// so this collection helps us manage the team's roster and player affiliations over time.
	/// </summary>
	public IEnumerable<ContractYear> ContractYears { get; set; }
	public decimal CurrentSalaryCap => ContractYears.Where(cy => cy.IsCurrent).Sum(cy => cy.BaseSalary + cy.SigningBonus);

	public override int ID => TeamID;
}