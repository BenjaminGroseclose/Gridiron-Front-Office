using System.ComponentModel.DataAnnotations.Schema;
using SQLite;

namespace GridironFrontOffice.Domain;

public class Team : BaseEntity
{
	[PrimaryKey, AutoIncrement]
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
	[ForeignKey("Conference")]
	public int ConferenceID { get; set; }

	public Conference Conference { get; set; }

	/// <summary>
	/// The ID of the division this team belongs to.
	/// </summary>
	[ForeignKey("Division")]
	public int DivisionID { get; set; }

	public Division Division { get; set; }

	/// <summary>
	/// The ID of the stadium this team plays in.
	/// </summary>
	[ForeignKey("Stadium")]
	public int StadiumID { get; set; }

	public Stadium Stadium { get; set; }

	public override int ID => TeamID;
}