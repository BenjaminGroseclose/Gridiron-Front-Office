using GridironFrontOffice.Domain.Enums;

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
	/// Dictates if this team is user controlled or AI controlled. T
	/// his can be used to determine which teams the user can make roster moves for,
	///  as well as other team management actions. 
	/// </summary>
	public bool IsUserControlled { get; set; }

	/// <summary>
	/// The path to the team's logo image file.
	/// Should be local to the application's base directory.
	/// </summary>
	public string? LogoPath { get; set; }

	/// <summary>
	/// The ID of the conference this team belongs to.
	/// This is a foreign key that links to the Conference entity, allowing us to identify which conference the team is in for organizational and scheduling purposes.
	/// </summary>
	public Conference Conference { get; set; }

	/// <summary>
	/// The division this team belongs to. 
	/// <see cref="Division"/> 
	/// </summary>
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

	/// <summary>
	/// The status of the team. This can be used to indicate if the team is active or inactive in the league.
	/// For example, if a team is removed from the league, we can set its status to Inactive instead of deleting it from the database,
	///  which allows us to maintain historical data about the team and its players.
	/// </summary>
	public TeamStatus Status { get; set; }

	public override int ID => TeamID;

	public IEnumerable<ContractYear> GetContractsForSeason(int seasonID)
	{
		if (ContractYears == null)
		{
			throw new Exception($"Team {Name} has no contract years defined.");
		}

		return ContractYears.Where(cy => cy.SeasonID == seasonID);
	}

	public string DisplayName => $"{City} {Name}";
	public string ConferenceAndDivision
	{
		get
		{
			var conferenceName = Conference.ToString();
			var divisionName = Division.ToString();

			return $"{conferenceName} {divisionName}";
		}
	}
}